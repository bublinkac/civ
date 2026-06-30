using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

/// <summary>
/// Reusable minimap component for advisor panels.
/// Renders the full world map with terrain colors and configurable overlays:
///   - Cities (with faction coloring, optional culture radius)
///   - Units (with faction coloring, optional filter by type)
///   - Territory borders (colored by faction)
/// 
/// Usage: var map = new AdvisorMapPanel(sim, options);
/// Embed in any advisor panel's layout.
/// </summary>
public partial class AdvisorMapPanel : PanelContainer
{
    private GameSimulation _sim;
    private AdvisorMapOptions _options;
    private AdvisorMapCanvas _canvas;

    public AdvisorMapPanel(GameSimulation sim, AdvisorMapOptions? options = null)
    {
        _sim = sim;
        _options = options ?? new AdvisorMapOptions();

        CustomMinimumSize = _options.MinSize;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // Dark frame around the map
        var frameStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.1f, 0.08f),
            BorderWidthTop = 3, BorderWidthBottom = 3, BorderWidthLeft = 3, BorderWidthRight = 3,
            BorderColor = new Color(0.4f, 0.35f, 0.25f),
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2
        };
        AddThemeStyleboxOverride("panel", frameStyle);

        _canvas = new AdvisorMapCanvas(sim, _options);
        _canvas.SizeFlagsVertical = SizeFlags.ExpandFill;
        _canvas.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(_canvas);
    }

    /// <summary>Force a redraw (e.g. after data changes).</summary>
    public void Refresh() => _canvas.QueueRedraw();
}

/// <summary>
/// Configuration options for the advisor map display.
/// </summary>
public class AdvisorMapOptions
{
    public Vector2 MinSize { get; set; } = new Vector2(300, 180);

    // What to display
    public bool ShowTerrain { get; set; } = true;
    public bool ShowCities { get; set; } = true;
    public bool ShowUnits { get; set; } = false;
    public bool ShowTerritory { get; set; } = true;
    public bool ShowCultureRadius { get; set; } = false;

    // Filtering
    public Faction? UnitFactionFilter { get; set; } = null; // null = show all
    public Faction? CityFactionFilter { get; set; } = null; // null = show all
    public bool ShowFogOfWar { get; set; } = true;

    // Highlight specific tiles
    public HashSet<(int X, int Y)>? HighlightedTiles { get; set; } = null;
    public Color HighlightColor { get; set; } = new Color(1.0f, 1.0f, 0.0f, 0.4f);

    // City label display
    public bool ShowCityNames { get; set; } = false;

    // Custom dot sizes
    public float CityDotScale { get; set; } = 1.5f;
    public float UnitDotScale { get; set; } = 1.0f;
}

/// <summary>
/// Custom-drawn canvas that renders the advisor map using _Draw().
/// </summary>
public partial class AdvisorMapCanvas : Control
{
    private GameSimulation _sim;
    private AdvisorMapOptions _options;

    public AdvisorMapCanvas(GameSimulation sim, AdvisorMapOptions options)
    {
        _sim = sim;
        _options = options;
    }

    public override void _Draw()
    {
        if (_sim == null) return;

        int mapW = _sim.Map.Width;
        int mapH = _sim.Map.Height;
        Vector2 size = Size;

        if (size.X <= 0 || size.Y <= 0 || mapW <= 0 || mapH <= 0) return;

        float scaleX = size.X / mapW;
        float scaleY = size.Y / mapH;

        // --- TERRAIN ---
        if (_options.ShowTerrain)
        {
            for (int x = 0; x < mapW; x++)
            {
                for (int y = 0; y < mapH; y++)
                {
                    // Fog of war check
                    if (_options.ShowFogOfWar)
                    {
                        var fog = _sim.VisibilityGrid[x, y];
                        if (fog == FogState.Unexplored)
                        {
                            DrawRect(new Rect2(x * scaleX, y * scaleY, scaleX, scaleY), new Color(0.05f, 0.05f, 0.05f));
                            continue;
                        }

                        var tile = _sim.Map.GetTile(x, y);
                        Color color = tile?.Terrain.Color ?? Colors.Black;

                        if (fog == FogState.Shrouded)
                            color = color.Darkened(0.45f);

                        DrawRect(new Rect2(x * scaleX, y * scaleY, scaleX, scaleY), color);
                    }
                    else
                    {
                        var tile = _sim.Map.GetTile(x, y);
                        Color color = tile?.Terrain.Color ?? Colors.Black;
                        DrawRect(new Rect2(x * scaleX, y * scaleY, scaleX, scaleY), color);
                    }
                }
            }
        }

        // --- TERRITORY OVERLAY ---
        if (_options.ShowTerritory)
        {
            for (int x = 0; x < mapW; x++)
            {
                for (int y = 0; y < mapH; y++)
                {
                    var tile = _sim.Map.GetTile(x, y);
                    if (tile?.OwnerCityId == null) continue;

                    // Find which faction owns this tile
                    var ownerCity = _sim.Cities.FirstOrDefault(c => c.Id == tile.OwnerCityId);
                    if (ownerCity == null) continue;

                    Color territoryColor = ownerCity.Faction switch
                    {
                        Faction.Player => new Color(0.2f, 0.5f, 1.0f, 0.15f),
                        Faction.AiRival => new Color(1.0f, 0.2f, 0.2f, 0.15f),
                        _ => new Color(0.5f, 0.5f, 0.5f, 0.1f)
                    };

                    DrawRect(new Rect2(x * scaleX, y * scaleY, scaleX, scaleY), territoryColor);
                }
            }
        }

        // --- CULTURE RADIUS OVERLAY ---
        if (_options.ShowCultureRadius)
        {
            foreach (var city in _sim.Cities)
            {
                if (_options.CityFactionFilter != null && city.Faction != _options.CityFactionFilter) continue;

                int radius = 1;
                if (city.AccumulatedCulture >= 10000) radius = 5;
                else if (city.AccumulatedCulture >= 1000) radius = 4;
                else if (city.AccumulatedCulture >= 100) radius = 3;
                else if (city.AccumulatedCulture >= 10) radius = 2;

                Color cultureColor = city.Faction == Faction.Player
                    ? new Color(0.3f, 0.6f, 1.0f, 0.2f)
                    : new Color(1.0f, 0.3f, 0.3f, 0.2f);

                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) > radius) continue;
                        int tx = city.X + dx;
                        int ty = city.Y + dy;
                        if (tx < 0 || tx >= mapW || ty < 0 || ty >= mapH) continue;
                        DrawRect(new Rect2(tx * scaleX, ty * scaleY, scaleX, scaleY), cultureColor);
                    }
                }
            }
        }

        // --- HIGHLIGHTED TILES ---
        if (_options.HighlightedTiles != null)
        {
            foreach (var (hx, hy) in _options.HighlightedTiles)
            {
                if (hx >= 0 && hx < mapW && hy >= 0 && hy < mapH)
                {
                    DrawRect(new Rect2(hx * scaleX, hy * scaleY, scaleX, scaleY), _options.HighlightColor);
                }
            }
        }

        // --- CITIES ---
        if (_options.ShowCities)
        {
            foreach (var city in _sim.Cities)
            {
                if (_options.CityFactionFilter != null && city.Faction != _options.CityFactionFilter) continue;

                if (_options.ShowFogOfWar && _sim.VisibilityGrid[city.X, city.Y] == FogState.Unexplored)
                    continue;

                Color dotColor = city.Faction switch
                {
                    Faction.Player => Colors.Gold,
                    Faction.AiRival => new Color(0.9f, 0.2f, 0.2f),
                    _ => Colors.Gray
                };

                float dotSize = scaleX * _options.CityDotScale;
                Vector2 center = new Vector2(city.X * scaleX + scaleX / 2, city.Y * scaleY + scaleY / 2);
                DrawCircle(center, dotSize, dotColor);

                // City name label
                if (_options.ShowCityNames)
                {
                    // Draw a small dot-label below
                    var font = ThemeDB.FallbackFont;
                    if (font != null)
                    {
                        float fontSize = Math.Max(8, scaleX * 1.5f);
                        Vector2 textPos = center + new Vector2(-scaleX * 2, scaleY * 1.5f);
                        DrawString(font, textPos, city.Name, HorizontalAlignment.Left, -1, (int)fontSize, dotColor);
                    }
                }
            }
        }

        // --- UNITS ---
        if (_options.ShowUnits)
        {
            foreach (var unit in _sim.Units)
            {
                if (_options.UnitFactionFilter != null && unit.Faction != _options.UnitFactionFilter) continue;

                if (_options.ShowFogOfWar && _sim.VisibilityGrid[unit.X, unit.Y] != FogState.Visible)
                    continue;

                Color dotColor = unit.Faction switch
                {
                    Faction.Player => Colors.White,
                    Faction.AiRival => new Color(0.8f, 0.3f, 0.8f),
                    Faction.Barbarian => new Color(0.9f, 0.3f, 0.1f),
                    _ => Colors.Gray
                };

                float dotSize = scaleX * _options.UnitDotScale * 0.7f;
                Vector2 center = new Vector2(unit.X * scaleX + scaleX / 2, unit.Y * scaleY + scaleY / 2);
                DrawCircle(center, dotSize, dotColor);
            }
        }
    }
}
