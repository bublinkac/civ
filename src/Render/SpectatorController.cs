using System;
using System.Linq;
using Godot;
using CivGame.Core;
using CivGame.Core.AI;

namespace CivGame.Render;

public partial class SpectatorController : Node
{
    private MapRenderer _mapRenderer = null!;
    private GameSimulation _sim = null!;

    public bool IsActive { get; private set; } = false;
    public bool IsPaused { get; set; } = false;
    public float TurnDelaySeconds { get; set; } = 0.2f; // 200ms default speed
    public bool OmniscientVision { get; set; } = true;
    public int TurnsSimulatedInSession { get; private set; } = 0;

    public event Action<bool>? OnSpectatorStateChanged;
    public event Action? OnTurnAdvanced;

    private double _elapsedTime = 0.0;

    public void Initialize(MapRenderer mapRenderer, GameSimulation sim)
    {
        _mapRenderer = mapRenderer;
        _sim = sim;
    }

    public void ToggleSpectatorMode()
    {
        if (IsActive)
        {
            DisableSpectatorMode();
        }
        else
        {
            EnableSpectatorMode(OmniscientVision);
        }
    }

    public void EnableSpectatorMode(bool omniscientVision = true)
    {
        if (IsActive) return;

        IsActive = true;
        IsPaused = false;
        OmniscientVision = omniscientVision;

        if (_sim != null) _sim.GodMode = OmniscientVision;

        if (OmniscientVision)
        {
            ApplyGodVision();
        }

        _mapRenderer.UpdateUiAfterAction();
        OnSpectatorStateChanged?.Invoke(true);
        GD.Print("[Spectator] Spectator Mode ENABLED. AI controls all civilizations.");
    }

    public void DisableSpectatorMode()
    {
        if (!IsActive) return;

        IsActive = false;
        IsPaused = false;
        if (_sim != null) _sim.GodMode = false;

        // Restore normal player visibility
        _sim.UpdateVisibility();
        _mapRenderer.UpdateUiAfterAction();

        OnSpectatorStateChanged?.Invoke(false);
        GD.Print("[Spectator] Spectator Mode DISABLED. Control returned to human player.");
    }

    public void TogglePause()
    {
        if (!IsActive) return;
        IsPaused = !IsPaused;
        GD.Print($"[Spectator] {(IsPaused ? "PAUSED" : "RESUMED")}");
    }

    public void SetSpeed(float delaySeconds)
    {
        TurnDelaySeconds = Math.Max(0.0f, delaySeconds);
    }

    public override void _Process(double delta)
    {
        if (!IsActive || IsPaused || _sim == null || _sim.EndState != GameEndState.None) return;

        _elapsedTime += delta;
        if (_elapsedTime >= TurnDelaySeconds)
        {
            _elapsedTime = 0.0;
            StepTurn();
        }
    }

    public void StepTurn()
    {
        if (_sim == null || _sim.EndState != GameEndState.None) return;

        // 1. If human player has no research chosen, pick automatically
        EnsurePlayerResearch();

        // 2. If human player turn, execute AI logic for Faction.Player
        if (_sim.ActiveTurnFaction == Faction.Player)
        {
            AiRivalBrain.ProcessTurn(_sim, Faction.Player);
        }

        // 3. Complete turn (barbarians, AI rivals, production, science, trade)
        _sim.EndTurn();
        TurnsSimulatedInSession++;

        // 4. Re-apply omniscient vision if enabled
        if (OmniscientVision)
        {
            ApplyGodVision();
        }

        // 5. Update scene and UI
        _mapRenderer.UpdateUiAfterAction();
        OnTurnAdvanced?.Invoke();

        GD.Print($"[Spectator] Turn {_sim.TurnNumber} ({TurnTimeline.GetFormattedYear(_sim.TurnNumber)}) completed.");
    }

    public void FastForward(int count)
    {
        if (_sim == null || _sim.EndState != GameEndState.None || count <= 0) return;

        GD.Print($"[Spectator] Fast-forwarding {count} turns...");
        int targetTurn = _sim.TurnNumber + count;

        while (_sim.TurnNumber < targetTurn && _sim.EndState == GameEndState.None)
        {
            EnsurePlayerResearch();
            if (_sim.ActiveTurnFaction == Faction.Player)
            {
                AiRivalBrain.ProcessTurn(_sim, Faction.Player);
            }
            _sim.EndTurn();
            TurnsSimulatedInSession++;
        }

        if (OmniscientVision)
        {
            ApplyGodVision();
        }

        _mapRenderer.UpdateUiAfterAction();
        OnTurnAdvanced?.Invoke();
        GD.Print($"[Spectator] Fast-forward finished. Reached Turn {_sim.TurnNumber} ({TurnTimeline.GetFormattedYear(_sim.TurnNumber)}).");
    }

    private void EnsurePlayerResearch()
    {
        if (_sim.Research.CurrentResearch == null)
        {
            var available = _sim.Research.GetAvailableTechnologies();
            if (available.Count > 0)
            {
                var candidate = available.OrderBy(t => t.Era).ThenBy(t => t.ScienceCost).First();
                _sim.Research.SetResearch(candidate);
                GD.Print($"[Spectator Auto-Research] Player chose {candidate.Name}");
            }
        }
    }

    public void ApplyGodVision()
    {
        if (_sim == null) return;

        for (int x = 0; x < _sim.Map.Width; x++)
        {
            for (int y = 0; y < _sim.Map.Height; y++)
            {
                _sim.VisibilityGrid[x, y] = FogState.Visible;
                if (_sim.Player2VisibilityGrid != null)
                {
                    _sim.Player2VisibilityGrid[x, y] = FogState.Visible;
                }
            }
        }
    }
}
