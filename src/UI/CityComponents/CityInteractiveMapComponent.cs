using Godot;
using CivGame.Core;
using System.Collections.Generic;

namespace CivGame.UI.CityComponents;

public partial class CityInteractiveMapComponent : PanelContainer
{
    private City _city;
    private GameSimulation _sim;
    private GridContainer _mapGrid;

    public CityInteractiveMapComponent(City city, GameSimulation sim)
    {
        _city = city;
        _sim = sim;
        
        // Match the parchment style of the UI
        var style = new StyleBoxFlat { 
            BgColor = new Color(0.9f, 0.85f, 0.7f, 0.3f), // Parchment-like tint
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        };
        AddThemeStyleboxOverride("panel", style);

        // CenterContainer ensures the grid is centered within this component
        var centerContainer = new CenterContainer();
        AddChild(centerContainer);

        _mapGrid = new GridContainer { Columns = 5 };
        _mapGrid.AddThemeConstantOverride("h_separation", 5);
        _mapGrid.AddThemeConstantOverride("v_separation", 5);
        centerContainer.AddChild(_mapGrid);

        PopulateGrid();
    }

    private void PopulateGrid()
    {
        foreach (var child in _mapGrid.GetChildren()) child.QueueFree();

        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int tileX = _city.X + dx;
                int tileY = _city.Y + dy;
                
                // Vytvoríme kontajner pre izometrický vzhľad
                var container = new Control { CustomMinimumSize = new Vector2(80, 40) };
                
                // Použijeme jednoduchý Sprite alebo Polygon2D na vykreslenie kosoštvorca
                var poly = new Polygon2D();
                poly.Polygon = new Vector2[] {
                    new Vector2(40, 0),   // Top
                    new Vector2(80, 20),  // Right
                    new Vector2(40, 40),  // Bottom
                    new Vector2(0, 20)    // Left
                };
                
                // Nastavenie farby podľa terénu
                var tileData = _sim.Map.GetTile(tileX, tileY);
                poly.Color = tileData?.Terrain.Color ?? new Color(0.1f, 0.1f, 0.1f);
                
                container.AddChild(poly);
                
                // Zobrazenie mesta
                if (tileX == _city.X && tileY == _city.Y)
                {
                    var label = new Label { Text = "🏰", Position = new Vector2(25, 5) };
                    container.AddChild(label);
                }
                else if (_city.WorkedTiles.Contains((tileX, tileY)))
                {
                    var label = new Label { Text = "👤", Position = new Vector2(25, 5) };
                    container.AddChild(label);
                }

                // Klikateľná oblasť (Area2D alebo jednoduchý TextureButton)
                var btn = new Button { 
                    Flat = true, 
                    CustomMinimumSize = new Vector2(80, 40) 
                };
                btn.Pressed += () => ToggleWorkedTile(tileX, tileY);
                container.AddChild(btn);
                
                _mapGrid.AddChild(container);
            }
        }
    }

    private void ToggleWorkedTile(int x, int y)
    {
        if (_city.WorkedTiles.Contains((x, y)))
            _city.WorkedTiles.Remove((x, y));
        else
            _city.WorkedTiles.Add((x, y));
            
        PopulateGrid(); // Refresh
    }
}
