using System;
using System.Collections.Generic;
using Godot;
using CivGame.Core;

namespace CivGame.Audio;

/// <summary>
/// Centralized AudioManager AutoLoad singleton handling Era-specific BGM crossfading,
/// unit/combat SFX routing, volume controls, and smart asset path resolution with caching.
/// </summary>
public partial class AudioManager : Node
{
    public static AudioManager? Instance { get; private set; }

    // Dual music players for smooth crossfades between eras/menus
    private AudioStreamPlayer _musicPlayerA = null!;
    private AudioStreamPlayer _musicPlayerB = null!;
    private bool _usingPlayerA = true;
    private Tween? _crossfadeTween;

    private string? _currentMusicPath;
    private TechEra? _currentEra;

    // Volume & mute state
    public float MasterVolumeDb { get; set; } = 0f;
    public float MusicVolumeDb { get; set; } = -6f;
    public float SfxVolumeDb { get; set; } = 0f;
    public bool IsMuted { get; private set; }
    public bool IsMusicMuted { get; private set; }
    public bool IsSfxMuted { get; private set; }

    // Stream cache & missing-path cache (prevents disk re-checks and log spam)
    private readonly Dictionary<string, AudioStream> _streamCache = new();
    private readonly HashSet<string> _missingPaths = new();
    private readonly Dictionary<string, ulong> _lastSfxPlayedMs = new();
    private const ulong SfxCooldownMs = 40;

    private static readonly string[] AudioExtensions = { ".ogg", ".mp3", ".wav" };

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;

        _musicPlayerA = new AudioStreamPlayer { Name = "MusicPlayerA" };
        _musicPlayerB = new AudioStreamPlayer { Name = "MusicPlayerB" };
        AddChild(_musicPlayerA);
        AddChild(_musicPlayerB);
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        ApplyMusicMuteState();
    }

    public void SetMusicMuted(bool muted)
    {
        IsMusicMuted = muted;
        ApplyMusicMuteState();
    }

    public void SetSfxMuted(bool muted)
    {
        IsSfxMuted = muted;
    }

    public void ToggleMute() => SetMuted(!IsMuted);

    private void ApplyMusicMuteState()
    {
        bool muteMusic = IsMuted || IsMusicMuted;
        if (_musicPlayerA != null) _musicPlayerA.StreamPaused = muteMusic;
        if (_musicPlayerB != null) _musicPlayerB.StreamPaused = muteMusic;
    }

    public void PlayMenuMusic()
    {
        _currentEra = null;
        PlayMusic("res://assets/audio/music/main_menu.ogg");
    }

    /// <summary>
    /// Switches background music to match the active TechEra if it changed.
    /// Probes .ogg, .mp3, and .wav under res://assets/audio/music/.
    /// </summary>
    public void PlayEraMusic(TechEra era)
    {
        if (_currentEra == era && IsMusicPlaying()) return;
        _currentEra = era;

        string trackName = era switch
        {
            TechEra.Ancient    => "era_ancient",
            TechEra.MiddleAges => "era_medieval",
            TechEra.Industrial => "era_industrial",
            TechEra.Modern     => "era_modern",
            _                  => "era_ancient"
        };

        PlayMusic($"res://assets/audio/music/{trackName}.ogg");
    }

    public bool IsMusicPlaying() =>
        (_musicPlayerA != null && _musicPlayerA.Playing) ||
        (_musicPlayerB != null && _musicPlayerB.Playing);

    public void PlayMusic(string resourcePath, float fadeSeconds = 1.5f)
    {
        string? resolved = ResolveMusicPath(resourcePath);
        if (resolved == null) return;
        if (_currentMusicPath == resolved && IsMusicPlaying()) return;

        var stream = LoadCachedStream(resolved);
        if (stream == null) return;

        _currentMusicPath = resolved;
        CrossfadeToStream(stream, fadeSeconds);
    }

    public void PlayMusic(AudioStream stream, float volumeDb = 0f)
    {
        if (stream == null) return;
        CrossfadeToStream(stream, 1.0f, volumeDb);
    }

    private void CrossfadeToStream(AudioStream stream, float fadeSeconds, float? overrideDb = null)
    {
        var incoming = _usingPlayerA ? _musicPlayerB : _musicPlayerA;
        var outgoing = _usingPlayerA ? _musicPlayerA : _musicPlayerB;
        _usingPlayerA = !_usingPlayerA;

        float targetDb = (overrideDb ?? MusicVolumeDb) + MasterVolumeDb;

        incoming.Stream = stream;
        incoming.VolumeDb = fadeSeconds > 0f ? -40f : targetDb;
        incoming.StreamPaused = IsMuted || IsMusicMuted;
        incoming.Play();

        _crossfadeTween?.Kill();
        if (fadeSeconds > 0f)
        {
            _crossfadeTween = CreateTween();
            _crossfadeTween.SetParallel(true);
            _crossfadeTween.TweenProperty(incoming, "volume_db", targetDb, fadeSeconds);
            if (outgoing.Playing)
            {
                _crossfadeTween.TweenProperty(outgoing, "volume_db", -40f, fadeSeconds);
                _crossfadeTween.Chain().TweenCallback(Callable.From(() => outgoing.Stop()));
            }
        }
        else if (outgoing.Playing)
        {
            outgoing.Stop();
        }
    }

    public void StopMusic()
    {
        _crossfadeTween?.Kill();
        _musicPlayerA?.Stop();
        _musicPlayerB?.Stop();
        _currentMusicPath = null;
    }

    public void PlaySfx(AudioStream stream, float volumeDb = 0f)
    {
        if (stream == null || IsMuted || IsSfxMuted) return;

        var sfxPlayer = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = volumeDb + SfxVolumeDb + MasterVolumeDb
        };
        AddChild(sfxPlayer);
        sfxPlayer.Finished += () => sfxPlayer.QueueFree();
        sfxPlayer.Play();
    }

    public void PlaySfx(string resourcePath, float volumeDb = 0f)
    {
        if (IsMuted || IsSfxMuted || string.IsNullOrEmpty(resourcePath)) return;

        string? resolved = ResolveSfxPath(resourcePath);
        if (resolved == null) return;

        ulong now = Time.GetTicksMsec();
        if (_lastSfxPlayedMs.TryGetValue(resolved, out ulong lastMs) && (now - lastMs) < SfxCooldownMs)
        {
            return;
        }
        _lastSfxPlayedMs[resolved] = now;

        var stream = LoadCachedStream(resolved);
        if (stream != null)
        {
            PlaySfx(stream, volumeDb);
        }
    }

    /// <summary>
    /// Plays unit-appropriate movement SFX with automatic fallback to generic unit_move.wav.
    /// </summary>
    public void PlayUnitMoveSfx(UnitType unitType)
    {
        string specific = unitType switch
        {
            UnitType.Settler => "res://assets/sounds/unit_settler_move.wav",
            UnitType.Worker => "res://assets/sounds/unit_worker_move.wav",
            UnitType.Horseman or UnitType.Knight or UnitType.Cavalry => "res://assets/sounds/unit_cavalry_move.wav",
            UnitType.Tank or UnitType.ModernArmor or UnitType.MechInfantry => "res://assets/sounds/unit_armor_move.wav",
            UnitType.Galley or UnitType.Caravel or UnitType.Galleon or UnitType.Frigate or UnitType.Ironclad
                or UnitType.Transport or UnitType.Destroyer or UnitType.Cruiser or UnitType.Battleship
                or UnitType.Carrier or UnitType.Submarine or UnitType.NuclearSubmarine or UnitType.AegisCruiser
                or UnitType.Privateer => "res://assets/sounds/unit_naval_move.wav",
            UnitType.Fighter or UnitType.JetFighter or UnitType.Bomber or UnitType.StealthBomber or UnitType.Helicopter
                => "res://assets/sounds/unit_air_move.wav",
            _ => "res://assets/sounds/unit_move.wav"
        };

        if (ResolveSfxPath(specific) != null)
        {
            PlaySfx(specific);
        }
        else
        {
            PlaySfx("res://assets/sounds/unit_move.wav");
        }
    }

    /// <summary>
    /// Plays unit-appropriate combat SFX with automatic fallback to combat_hit.wav.
    /// </summary>
    public void PlayUnitCombatSfx(UnitType unitType, bool isBombard = false)
    {
        string specific;
        if (isBombard || unitType is UnitType.Catapult or UnitType.Cannon or UnitType.Artillery or UnitType.RadarArtillery)
        {
            specific = "res://assets/sounds/combat_artillery.wav";
        }
        else
        {
            specific = unitType switch
            {
                UnitType.Archer or UnitType.Longbowman => "res://assets/sounds/combat_archery.wav",
                UnitType.Horseman or UnitType.Knight or UnitType.Cavalry => "res://assets/sounds/combat_cavalry.wav",
                UnitType.Musketman or UnitType.Rifleman or UnitType.Infantry or UnitType.Marine or UnitType.Paratrooper
                    => "res://assets/sounds/combat_gunfire.wav",
                UnitType.Tank or UnitType.ModernArmor or UnitType.MechInfantry => "res://assets/sounds/combat_armor.wav",
                UnitType.Fighter or UnitType.JetFighter or UnitType.Bomber or UnitType.StealthBomber
                    => "res://assets/sounds/combat_air.wav",
                UnitType.Galley or UnitType.Frigate or UnitType.Ironclad or UnitType.Destroyer or UnitType.Cruiser
                    or UnitType.Battleship or UnitType.Submarine or UnitType.NuclearSubmarine or UnitType.AegisCruiser
                    => "res://assets/sounds/combat_naval.wav",
                _ => "res://assets/sounds/combat_melee.wav"
            };
        }

        if (ResolveSfxPath(specific) != null)
        {
            PlaySfx(specific);
        }
        else
        {
            PlaySfx("res://assets/sounds/combat_hit.wav");
        }
    }

    private AudioStream? LoadCachedStream(string resolvedPath)
    {
        if (_streamCache.TryGetValue(resolvedPath, out var cached))
            return cached;

        var stream = ResourceLoader.Load<AudioStream>(resolvedPath);
        if (stream != null)
        {
            _streamCache[resolvedPath] = stream;
        }
        return stream;
    }

    private string? ResolveMusicPath(string requestedPath)
    {
        if (_streamCache.ContainsKey(requestedPath)) return requestedPath;
        if (_missingPaths.Contains(requestedPath)) return null;

        if (ResourceLoader.Exists(requestedPath)) return requestedPath;

        string baseNoExt = System.IO.Path.ChangeExtension(requestedPath, null).Replace("\\", "/");
        foreach (var ext in AudioExtensions)
        {
            string candidate = baseNoExt + ext;
            if (ResourceLoader.Exists(candidate)) return candidate;
        }

        _missingPaths.Add(requestedPath);
        return null;
    }

    private string? ResolveSfxPath(string requestedPath)
    {
        if (_streamCache.ContainsKey(requestedPath)) return requestedPath;
        if (_missingPaths.Contains(requestedPath)) return null;

        if (ResourceLoader.Exists(requestedPath)) return requestedPath;

        // Normalize between res://assets/sounds/ and res://assets/audio/sfx/
        var candidates = new List<string>();
        string fileName = System.IO.Path.GetFileName(requestedPath);
        string nameNoExt = System.IO.Path.GetFileNameWithoutExtension(requestedPath);

        candidates.Add($"res://assets/sounds/{fileName}");
        candidates.Add($"res://assets/audio/sfx/{fileName}");

        // Common naming aliases used across older/newer UI files
        string? alias = nameNoExt switch
        {
            "click" => "ui_click",
            "ui_click" => "click",
            "close" => "ui_close",
            "ui_close" => "close",
            "hover" => "ui_hover",
            "ui_hover" => "hover",
            "end_turn" => "ui_turn_end",
            "ui_turn_end" => "end_turn",
            _ => null
        };

        if (alias != null)
        {
            candidates.Add($"res://assets/sounds/{alias}.wav");
            candidates.Add($"res://assets/audio/sfx/{alias}.wav");
        }

        foreach (var c in candidates)
        {
            string baseNoExt = System.IO.Path.ChangeExtension(c, null).Replace("\\", "/");
            foreach (var ext in AudioExtensions)
            {
                string full = baseNoExt + ext;
                if (ResourceLoader.Exists(full))
                {
                    return full;
                }
            }
        }

        _missingPaths.Add(requestedPath);
        return null;
    }
}
