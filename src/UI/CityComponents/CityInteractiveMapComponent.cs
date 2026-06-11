using Godot;
using CivGame.Core;
using System.Collections.Generic;

namespace CivGame.UI.CityComponents;

public partial class CityInteractiveMapComponent : PanelContainer
{
    private City _city;
    private GameSimulation _sim;
    private Control _mapAnchor;

    private const float TileWidth = 120f;
    private const float TileHeight = 60f;

    public CityInteractiveMapComponent(City city, GameSimulation sim)
    {
        _city = city;
        _sim = sim;
        
        // Match the parchment style of the UI
        var style = new StyleBoxFlat { 
            BgColor = new Color(0.9f, 0.85f, 0.7f, 0.15f), // Parchment-like tint
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.5f, 0.4f, 0.3f, 0.5f)
        };
        AddThemeStyleboxOverride("panel", style);

        // Make the component expand to fill space, but maintain a beautiful minimum size
        CustomMinimumSize = new Vector2(650, 400);
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var centerContainer = new CenterContainer();
        centerContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        centerContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        AddChild(centerContainer);

        // This control acts as the (0,0) center of our isometric coordinate space
        _mapAnchor = new Control();
        // Since we are centering inside CenterContainer, _mapAnchor will be at the very center of the panel.
        // We will shift all tiles relative to this center point.
        centerContainer.AddChild(_mapAnchor);

        PopulateGrid();
    }

    private void PopulateGrid()
    {
        foreach (var child in _mapAnchor.GetChildren()) child.QueueFree();

        // Render from back (top) to front (bottom) to respect isometric depth overlap
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int tileX = _city.X + dx;
                int tileY = _city.Y + dy;
                
                // Calculate isometric position relative to center anchor
                float posX = (dx - dy) * (TileWidth / 2f);
                float posY = (dx + dy) * (TileHeight / 2f);

                // Create container for this tile, centered on its isometric coordinate
                var tileControl = new Control();
                tileControl.Position = new Vector2(posX - TileWidth / 2f, posY - TileHeight / 2f);
                tileControl.CustomMinimumSize = new Vector2(TileWidth, TileHeight);
                _mapAnchor.AddChild(tileControl);
                
                // Isometric diamond polygon
                var poly = new Polygon2D();
                poly.Polygon = new Vector2[] {
                    new Vector2(TileWidth / 2f, 0),          // Top
                    new Vector2(TileWidth, TileHeight / 2f),  // Right
                    new Vector2(TileWidth / 2f, TileHeight),  // Bottom
                    new Vector2(0, TileHeight / 2f)           // Left
                };
                
                // Base terrain color
                var tileData = _sim.Map.GetTile(tileX, tileY);
                Color baseColor = tileData?.Terrain.Color ?? new Color(0.1f, 0.1f, 0.1f);
                poly.Color = baseColor;
                tileControl.AddChild(poly);

                // Draw thin border to distinguish tiles
                var line = new Line2D();
                line.Points = new Vector2[] {
                    new Vector2(TileWidth / 2f, 0),
                    new Vector2(TileWidth, TileHeight / 2f),
                    new Vector2(TileWidth / 2f, TileHeight),
                    new Vector2(0, TileHeight / 2f),
                    new Vector2(TileWidth / 2f, 0)
                };
                line.Width = 1f;
                line.DefaultColor = new Color(0.15f, 0.15f, 0.15f, 0.4f); // Subtle dark outline
                tileControl.AddChild(line);

                // City Center Icon (🏰)
                if (tileX == _city.X && tileY == _city.Y)
                {
                    var label = new Label { 
                        Text = "🏰", 
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Size = new Vector2(TileWidth, TileHeight)
                    };
                    label.AddThemeFontSizeOverride("font_size", 24);
                    // Add outline for legibility
                    label.AddThemeColorOverride("font_outline_color", Colors.Black);
                    label.AddThemeConstantOverride("outline_size", 4);
                    tileControl.AddChild(label);
                }
                // Worked tile indicator (👤)
                else if (_city.WorkedTiles.Contains((tileX, tileY)))
                {
                    var label = new Label { 
                        Text = "👤", 
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Size = new Vector2(TileWidth, TileHeight)
                    };
                    label.AddThemeFontSizeOverride("font_size", 20);
                    label.AddThemeColorOverride("font_outline_color", Colors.Black);
                    label.AddThemeConstantOverride("outline_size", 4);
                    tileControl.AddChild(label);
                }

                // Invisible button to capture clicks within the rectangular bound of the tile
                var btn = new Button { 
                    Flat = true, 
                    CustomMinimumSize = new Vector2(TileWidth, TileHeight),
                    Size = new Vector2(TileWidth, TileHeight),
                    MouseDefaultCursorShape = CursorShape.PointingHand
                };
                // We don't want the button styling to mess up our look, but we want it to receive clicks.
                btn.Pressed += () => ToggleWorkedTile(tileX, tileY);
                tileControl.AddChild(btn);
            }
        }
    }

    private void ToggleWorkedTile(int x, int y)
    {
        // Don't toggle city center itself
        if (x == _city.X && y == _city.Y) return;

        if (_city.WorkedTiles.Contains((x, y)))
            _city.WorkedTiles.Remove((x, y));
        else
            _city.WorkedTiles.Add((x, y));
            
        PopulateGrid(); // Refresh
    }
}
