using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core;

public enum TurnEventCategory
{
    Production,
    Growth,
    Disorder,
    Science,
    Wonder,
    Economy,
    Military,
    Disaster,
    Trade,
    Culture,
    Diplomacy
}

public enum TurnEventSeverity
{
    Info,
    Success,
    Warning,
    Critical
}

public enum AdvisorType
{
    Domestic,
    Trade,
    Military,
    Foreign,
    Cultural,
    Science,
    General
}

public enum TurnEventActionType
{
    None,
    ZoomToTile,
    OpenCity,
    OpenTechTree,
    OpenAdvisor
}

/// <summary>
/// Represents a structured turn event or advisor notification that occurred during turn processing.
/// Pure C# model decoupled from Godot runtime, enabling headless testing and full game telemetry.
/// </summary>
public class TurnEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int Turn { get; set; }
    public Faction Faction { get; set; } = Faction.Player;
    public TurnEventCategory Category { get; set; }
    public AdvisorType Advisor { get; set; }
    public TurnEventSeverity Severity { get; set; }
    public string Icon { get; set; } = "ℹ️";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? AdvisorQuote { get; set; }
    public int? TileX { get; set; }
    public int? TileY { get; set; }
    public string? TargetCityId { get; set; }
    public string? TargetCityName { get; set; }
    public string? TargetTechId { get; set; }
    public string? ActionLabel { get; set; }
    public TurnEventActionType ActionType { get; set; } = TurnEventActionType.None;
    public bool IsRead { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string GetAdvisorDisplayName() => Advisor switch
    {
        AdvisorType.Domestic => "🏠 Domáci & Hospodársky poradca",
        AdvisorType.Trade => "💰 Obchodný poradca",
        AdvisorType.Military => "⚔️ Vojenský poradca",
        AdvisorType.Foreign => "🌍 Zahraničný poradca",
        AdvisorType.Cultural => "🎭 Kultúrny poradca",
        AdvisorType.Science => "🔬 Vedecký poradca",
        _ => "👑 Kancelária impéria"
    };

    public string GetSeverityLabel() => Severity switch
    {
        TurnEventSeverity.Critical => "🚨 KRITICKÉ",
        TurnEventSeverity.Warning => "⚠️ VAROVANIE",
        TurnEventSeverity.Success => "✅ ÚSPECH",
        _ => "ℹ️ INFO"
    };
}

/// <summary>
/// Event repository managing the history and real-time buffering of empire notifications.
/// </summary>
public class TurnEventLog
{
    private readonly List<TurnEvent> _events = new();
    private readonly object _lock = new();

    public event Action<TurnEvent>? OnEventAdded;
    public event Action<int, Faction>? OnTurnCompleted;

    public void AddEvent(TurnEvent ev)
    {
        lock (_lock)
        {
            _events.Add(ev);
        }
        OnEventAdded?.Invoke(ev);
    }

    public void NotifyTurnCompleted(int turn, Faction faction)
    {
        OnTurnCompleted?.Invoke(turn, faction);
    }

    public List<TurnEvent> GetTurnEvents(int turn, Faction faction)
    {
        lock (_lock)
        {
            return _events
                .Where(e => e.Turn == turn && (e.Faction == faction || e.Faction == Faction.Player))
                .OrderByDescending(e => e.Severity)
                .ThenBy(e => e.Category)
                .ToList();
        }
    }

    public List<TurnEvent> GetLatestEvents(Faction faction, int maxCount = 50)
    {
        lock (_lock)
        {
            return _events
                .Where(e => e.Faction == faction || e.Faction == Faction.Player)
                .OrderByDescending(e => e.Turn)
                .ThenByDescending(e => e.Severity)
                .Take(maxCount)
                .ToList();
        }
    }

    public List<TurnEvent> GetUnreadEvents(Faction faction)
    {
        lock (_lock)
        {
            return _events
                .Where(e => !e.IsRead && (e.Faction == faction || e.Faction == Faction.Player))
                .OrderByDescending(e => e.Turn)
                .ThenByDescending(e => e.Severity)
                .ToList();
        }
    }

    public void MarkAllAsRead(Faction faction)
    {
        lock (_lock)
        {
            foreach (var ev in _events.Where(e => e.Faction == faction || e.Faction == Faction.Player))
            {
                ev.IsRead = true;
            }
        }
    }

    public void MarkAsRead(string eventId)
    {
        lock (_lock)
        {
            var ev = _events.FirstOrDefault(e => e.Id == eventId);
            if (ev != null) ev.IsRead = true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}
