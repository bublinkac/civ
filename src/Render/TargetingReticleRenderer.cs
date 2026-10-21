using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.Render;

/// <summary>
/// Renders the interactive targeting reticle and tactical range overlay
/// for Artillery, Naval Bombardment, Air Strikes, Recon, and Re-basing.
/// </summary>
public partial class TargetingReticleRenderer : Node2D
{
    private MapRenderer? _mapRenderer;
    private GameSimulation? _sim;
    private Unit? _sourceUnit;
    private string _action = "";
    private int _range;
    private int _minRange = 1;

    private readonly HashSet<(int X, int Y)> _validTargets = new();
    private readonly HashSet<(int X, int Y)> _hostileTargets = new();
    private Vector2I? _hoveredCell;
    private bool _isActive;
    private float _pulseTime;

    public bool IsActive => _isActive;
    public string CurrentAction => _action;
    public Unit? SourceUnit => _sourceUnit;

    public override void _Ready()
    {
        // Render above units, cities, and fog of war
        ZIndex = 3;
        ZAsRelative = false;
    }

    public void Initialize(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
    }

    public void Activate(GameSimulation sim, Unit unit, string action, int range, int minRange = 1)
    {
        _sim = sim;
        _sourceUnit = unit;
        _action = action;
        _range = range;
        _minRange = minRange;

        _validTargets.Clear();
        _hostileTargets.Clear();

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                if (dist < minRange || dist > range) continue;

                int tx = sim.Map.NormalizeX(unit.X + dx);
                int ty = unit.Y + dy;

                if (ty < 0 || ty >= sim.Map.Height) continue;
                if (sim.VisibilityGrid[tx, ty] == FogState.Unexplored) continue;

                if (action == "air_rebase")
                {
                    // Only friendly cities can be re-base targets
                    if (sim.Cities.Any(c => c.X == tx && c.Y == ty && c.Faction == unit.Faction))
                    {
                        _validTargets.Add((tx, ty));
                    }
                    continue;
                }

                _validTargets.Add((tx, ty));

                // Check hostile targets (units or cities)
                bool hasHostileUnit = sim.Units.Any(u => u.X == tx && u.Y == ty && u.TransportUnitId == null && sim.IsHostile(unit, u));
                bool hasHostileCity = sim.Cities.Any(c => c.X == tx && c.Y == ty && c.Faction != unit.Faction && sim.IsHostileToFaction(unit.Faction, c.Faction));

                if (hasHostileUnit || hasHostileCity)
                {
                    _hostileTargets.Add((tx, ty));
                }
            }
        }

        _isActive = true;
        _pulseTime = 0.0f;
        QueueRedraw();
    }

    public void Deactivate()
    {
        _isActive = false;
        _validTargets.Clear();
        _hostileTargets.Clear();
        _hoveredCell = null;
        _sourceUnit = null;
        _action = "";
        QueueRedraw();
    }

    public void SetHoveredTile(int x, int y)
    {
        if (_hoveredCell.HasValue && _hoveredCell.Value.X == x && _hoveredCell.Value.Y == y)
            return;

        _hoveredCell = new Vector2I(x, y);
        if (_isActive)
        {
            QueueRedraw();
        }
    }

    public bool IsValidTarget(int x, int y)
    {
        if (!_isActive || _sim == null || _sourceUnit == null) return false;
        if (!_validTargets.Contains((x, y))) return false;

        if (_action == "air_rebase")
        {
            return _sim.Cities.Any(c => c.X == x && c.Y == y && c.Faction == _sourceUnit.Faction);
        }

        if (_action == "air_recon")
        {
            return _sim.Map.IsInBounds(x, y);
        }

        // For bombard and air_strike: valid target if enemy unit, enemy city, or targetable terrain improvement/road
        bool hasHostileUnit = _sim.Units.Any(u => u.X == x && u.Y == y && u.TransportUnitId == null && _sim.IsHostile(_sourceUnit, u));
        if (hasHostileUnit) return true;

        bool hasHostileCity = _sim.Cities.Any(c => c.X == x && c.Y == y && c.Faction != _sourceUnit.Faction && _sim.IsHostileToFaction(_sourceUnit.Faction, c.Faction));
        if (hasHostileCity) return true;

        var tile = _sim.Map.GetTile(x, y);
        if (tile != null && (tile.Improvement != null || tile.HasRoad || tile.HasRailroad))
            return true;

        return false;
    }

    public override void _Process(double delta)
    {
        if (_isActive)
        {
            _pulseTime += (float)delta;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!_isActive || _mapRenderer == null || _sim == null) return;

        // 1. Draw Range Polygon Overlays on All Valid Tiles
        foreach (var (tx, ty) in _validTargets)
        {
            Vector2 center = _mapRenderer.MapToLocal(new Vector2I(tx, ty));

            Vector2[] diamond = new Vector2[]
            {
                center + new Vector2(0, -64),
                center + new Vector2(128, 0),
                center + new Vector2(0, 64),
                center + new Vector2(-128, 0)
            };

            if (_hostileTargets.Contains((tx, ty)))
            {
                // Hostile target: pulsing red diamond
                float alpha = 0.28f + 0.08f * Mathf.Sin(_pulseTime * 4.0f);
                DrawColoredPolygon(diamond, new Color(0.95f, 0.15f, 0.15f, alpha));
                DrawPolyline(diamond, new Color(1.0f, 0.25f, 0.25f, 0.85f), 2.0f);
                DrawLine(diamond[3], diamond[0], new Color(1.0f, 0.25f, 0.25f, 0.85f), 2.0f);
            }
            else if (_action == "air_rebase")
            {
                // Friendly base: bright emerald green
                DrawColoredPolygon(diamond, new Color(0.15f, 0.85f, 0.25f, 0.28f));
                DrawPolyline(diamond, new Color(0.25f, 1.0f, 0.35f, 0.80f), 1.5f);
                DrawLine(diamond[3], diamond[0], new Color(0.25f, 1.0f, 0.35f, 0.80f), 1.5f);
            }
            else
            {
                // General target range: amber warning
                DrawColoredPolygon(diamond, new Color(1.0f, 0.65f, 0.1f, 0.18f));
                DrawPolyline(diamond, new Color(1.0f, 0.75f, 0.2f, 0.65f), 1.5f);
                DrawLine(diamond[3], diamond[0], new Color(1.0f, 0.75f, 0.2f, 0.65f), 1.5f);
            }
        }

        // 2. Draw Crosshairs Targeting Reticle on Hovered Tile
        if (_hoveredCell.HasValue)
        {
            int hx = _hoveredCell.Value.X;
            int hy = _hoveredCell.Value.Y;
            Vector2 hCenter = _mapRenderer.MapToLocal(new Vector2I(hx, hy));

            bool isValid = IsValidTarget(hx, hy);
            bool isHostile = _hostileTargets.Contains((hx, hy));

            Color reticleColor = isHostile
                ? new Color(1.0f, 0.2f, 0.2f, 0.95f)
                : (isValid ? new Color(1.0f, 0.85f, 0.2f, 0.9f) : new Color(0.7f, 0.7f, 0.7f, 0.45f));

            float pulse = 1.0f + 0.08f * Mathf.Sin(_pulseTime * 8.0f);
            float radius = 28.0f * pulse;

            // Target Circle
            DrawArc(hCenter, radius, 0, Mathf.Tau, 32, reticleColor, 2.0f);
            DrawCircle(hCenter, 3.0f, reticleColor);

            // Crosshair Lines
            float lineLen = 14.0f;
            float gap = 8.0f;
            DrawLine(hCenter + new Vector2(-radius - lineLen, 0), hCenter + new Vector2(-gap, 0), reticleColor, 2.0f);
            DrawLine(hCenter + new Vector2(gap, 0), hCenter + new Vector2(radius + lineLen, 0), reticleColor, 2.0f);
            DrawLine(hCenter + new Vector2(0, -radius - lineLen), hCenter + new Vector2(0, -gap), reticleColor, 2.0f);
            DrawLine(hCenter + new Vector2(0, gap), hCenter + new Vector2(0, radius + lineLen), reticleColor, 2.0f);

            // Isometric Corner Brackets
            float bw = 24.0f;
            // Top corner
            Vector2 top = hCenter + new Vector2(0, -56);
            DrawLine(top + new Vector2(-bw, bw * 0.5f), top, reticleColor, 2.5f);
            DrawLine(top, top + new Vector2(bw, bw * 0.5f), reticleColor, 2.5f);
            // Bottom corner
            Vector2 btm = hCenter + new Vector2(0, 56);
            DrawLine(btm + new Vector2(-bw, -bw * 0.5f), btm, reticleColor, 2.5f);
            DrawLine(btm, btm + new Vector2(bw, -bw * 0.5f), reticleColor, 2.5f);
            // Left corner
            Vector2 left = hCenter + new Vector2(-110, 0);
            DrawLine(left + new Vector2(bw, -bw * 0.5f), left, reticleColor, 2.5f);
            DrawLine(left, left + new Vector2(bw, bw * 0.5f), reticleColor, 2.5f);
            // Right corner
            Vector2 right = hCenter + new Vector2(110, 0);
            DrawLine(right + new Vector2(-bw, -bw * 0.5f), right, reticleColor, 2.5f);
            DrawLine(right, right + new Vector2(-bw, bw * 0.5f), reticleColor, 2.5f);

            // Target Tooltip Badge right above the tile
            string targetDesc = GetTargetDescription(hx, hy);
            if (!string.IsNullOrEmpty(targetDesc))
            {
                var font = ThemeDB.FallbackFont;
                int fontSize = 12;
                Vector2 textSize = font.GetStringSize(targetDesc, HorizontalAlignment.Center, -1, fontSize);
                Vector2 badgePos = hCenter + new Vector2(-textSize.X / 2 - 8, -75 - textSize.Y);
                Rect2 badgeRect = new Rect2(badgePos, new Vector2(textSize.X + 16, textSize.Y + 8));

                Color badgeBg = isHostile
                    ? new Color(0.18f, 0.02f, 0.02f, 0.90f)
                    : (isValid ? new Color(0.12f, 0.12f, 0.16f, 0.90f) : new Color(0.1f, 0.1f, 0.1f, 0.75f));

                DrawRect(badgeRect, badgeBg, true);
                DrawRect(badgeRect, reticleColor, false, 1.5f);
                DrawString(font, badgePos + new Vector2(8, textSize.Y), targetDesc, HorizontalAlignment.Left, -1, fontSize, Colors.White);
            }
        }
    }

    private string GetTargetDescription(int x, int y)
    {
        if (_sim == null || !_sim.Map.IsInBounds(x, y)) return "";
        if (!_validTargets.Contains((x, y))) return "❌ Out of Range";

        if (_action == "air_rebase")
        {
            var baseCity = _sim.Cities.FirstOrDefault(c => c.X == x && c.Y == y && c.Faction == _sourceUnit?.Faction);
            if (baseCity != null) return $"🛬 Re-base to {baseCity.Name}";
            return "❌ Not a friendly city";
        }

        if (_action == "air_recon")
        {
            return $"🔭 Air Reconnaissance at ({x}, {y})";
        }

        var enemyUnit = _sim.Units.FirstOrDefault(u => u.X == x && u.Y == y && u.TransportUnitId == null && _sim.IsHostile(_sourceUnit!, u));
        if (enemyUnit != null)
        {
            return $"🎯 [{_action.ToUpper()}] {enemyUnit.Type} (HP {enemyUnit.Health}/{enemyUnit.MaxHealth}, Def {enemyUnit.DefenseStrength})";
        }

        var enemyCity = _sim.Cities.FirstOrDefault(c => c.X == x && c.Y == y && c.Faction != _sourceUnit?.Faction && _sim.IsHostileToFaction(_sourceUnit!.Faction, c.Faction));
        if (enemyCity != null)
        {
            return $"🎯 [{_action.ToUpper()}] City of {enemyCity.Name} (Pop {enemyCity.Population})";
        }

        var tile = _sim.Map.GetTile(x, y);
        if (tile != null && (tile.Improvement != null || tile.HasRoad || tile.HasRailroad))
        {
            string impName = tile.Improvement?.Name ?? (tile.HasRailroad ? "Railroad" : "Road");
            return $"💥 [{_action.ToUpper()}] Infrastructure: {impName}";
        }

        return $"⚠️ No valid target at ({x}, {y})";
    }
}
