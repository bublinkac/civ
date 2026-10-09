using System;
using Godot;

namespace CivGame.Audio;

/// <summary>
/// Simple AudioManager to handle background music and sound effects.
/// Add this as an AutoLoad (Singleton) in Godot project settings, 
/// or instantiate it at the root of your main scene.
/// </summary>
public partial class AudioManager : Node
{
    public static AudioManager? Instance { get; private set; }

    private AudioStreamPlayer _musicPlayer = null!;

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        
        _musicPlayer = new AudioStreamPlayer
        {
            Name = "MusicPlayer"
        };
        AddChild(_musicPlayer);
    }

    public void PlayMusic(AudioStream stream, float volumeDb = 0f)
    {
        if (stream == null) return;
        _musicPlayer.Stream = stream;
        _musicPlayer.VolumeDb = volumeDb;
        _musicPlayer.Play();
    }

    public void PlayMusic(string resourcePath, float volumeDb = 0f)
    {
        if (!ResourceLoader.Exists(resourcePath))
        {
            GD.Print($"[AudioManager] Music file not found, skipping: {resourcePath}");
            return;
        }

        var stream = ResourceLoader.Load<AudioStream>(resourcePath);
        if (stream != null)
        {
            PlayMusic(stream, volumeDb);
        }
    }

    public void StopMusic()
    {
        _musicPlayer.Stop();
    }

    public void PlaySfx(AudioStream stream, float volumeDb = 0f)
    {
        if (stream == null) return;
        
        var sfxPlayer = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = volumeDb
        };
        AddChild(sfxPlayer);
        
        // Clean up the player once the sound finishes playing
        sfxPlayer.Finished += () => sfxPlayer.QueueFree();
        
        sfxPlayer.Play();
    }

    public void PlaySfx(string resourcePath, float volumeDb = 0f)
    {
        if (!ResourceLoader.Exists(resourcePath))
        {
            GD.Print($"[AudioManager] SFX file not found, skipping: {resourcePath}");
            return;
        }

        var stream = ResourceLoader.Load<AudioStream>(resourcePath);
        if (stream != null)
        {
            PlaySfx(stream, volumeDb);
        }
    }
}
