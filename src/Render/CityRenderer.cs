using System;
using System.Collections.Generic;
using Godot;
using CivGame.Core;

namespace CivGame.Render;

public partial class CityRenderer : Node2D
{
    private MapRenderer? _mapRenderer;
    private readonly Dictionary<string, Sprite2D> _sprites = new();

    public override void _Ready()
    {
        // Stand on top of terrain, aligned with units
        ZIndex = 1;
        YSortEnabled = true;

        GetOrCreateCityTexture();
    }

    public void Initialize(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
    }

    public void UpdateCities(GameSimulation sim)
    {
        if (_mapRenderer == null) return;

        var activeIds = new HashSet<string>();

        foreach (var city in sim.Cities)
        {
            activeIds.Add(city.Id);
            Vector2 targetPos = _mapRenderer.MapToLocal(new Vector2I(city.X, city.Y));
            
            // Adjust height offset so the castle sits nicely in the isometric tile center
            targetPos.Y -= 16; 

            if (!_sprites.TryGetValue(city.Id, out var sprite))
            {
                sprite = new Sprite2D
                {
                    Texture = GetOrCreateCityTexture(),
                    Position = targetPos,
                    YSortEnabled = true
                };
                AddChild(sprite);
                _sprites[city.Id] = sprite;

                // Create a floating text label above the city showing its name
                var label = new Label
                {
                    Text = city.Name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Position = new Vector2(-60, -42),
                    Size = new Vector2(120, 20)
                };
                // Style city labels so they are clearly readable above the map
                label.AddThemeFontSizeOverride("font_size", 14);
                label.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.95f));
                label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
                label.AddThemeConstantOverride("outline_size", 4);
                sprite.AddChild(label);
            }
        }

        // Remove sprites of destroyed or non-existent cities
        var toRemove = new List<string>();
        foreach (var id in _sprites.Keys)
        {
            if (!activeIds.Contains(id))
            {
                _sprites[id].QueueFree();
                toRemove.Add(id);
            }
        }
        foreach (var id in toRemove)
        {
            _sprites.Remove(id);
        }
    }

    private Texture2D GetOrCreateCityTexture()
    {
        string dirPath = "res://assets";
        string filePath = $"{dirPath}/city.png";

        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(dirPath);
        }

        Image img = GenerateCityImage();

        try
        {
            img.SavePng(filePath);
        }
        catch (Exception ex)
        {
            GD.Print($"[CityRenderer] Warning: Could not save PNG to {filePath}: {ex.Message}");
        }

        return ImageTexture.CreateFromImage(img);
    }

    private Image GenerateCityImage()
    {
        int width = 64;
        int height = 64;
        Image img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        // Procedural gray fortress with a red flag
        Color stoneColor = new Color(0.6f, 0.6f, 0.62f);
        Color stoneShadow = new Color(0.42f, 0.42f, 0.45f);
        Color roofColor = new Color(0.7f, 0.2f, 0.2f);
        Color flagColor = new Color(0.9f, 0.1f, 0.1f);
        Color woodColor = new Color(0.42f, 0.25f, 0.12f);
        Color outlineColor = new Color(0.12f, 0.12f, 0.15f);

        // Draw left and right towers
        DrawTower(img, 10, 22, stoneColor, stoneShadow, roofColor, outlineColor);
        DrawTower(img, 42, 54, stoneColor, stoneShadow, roofColor, outlineColor);

        // Main wall (middle section)
        for (int y = 35; y <= 56; y++)
        {
            for (int x = 18; x <= 46; x++)
            {
                Color fill = x < 32 ? stoneColor : stoneShadow;
                if (x == 18 || x == 46 || y == 35 || y == 56)
                {
                    img.SetPixel(x, y, outlineColor);
                }
                else
                {
                    img.SetPixel(x, y, fill);
                }
            }
        }

        // Castle gate / arch
        for (int y = 45; y <= 55; y++)
        {
            for (int x = 27; x <= 37; x++)
            {
                if (x == 27 || x == 37 || y == 45)
                {
                    img.SetPixel(x, y, outlineColor);
                }
                else
                {
                    img.SetPixel(x, y, woodColor);
                }
            }
        }

        // Draw a flag on top of the left tower (pole at x=16)
        for (int y = 10; y <= 21; y++)
        {
            img.SetPixel(16, y, outlineColor); // flagpole
        }
        for (int y = 10; y <= 14; y++)
        {
            for (int x = 17; x <= 26; x++)
            {
                img.SetPixel(x, y, flagColor);
            }
        }

        return img;
    }

    private void DrawTower(Image img, int lx, int rx, Color baseCol, Color shadowCol, Color roofCol, Color outlineCol)
    {
        int bottom = 56;
        int top = 22;

        // Tower body
        for (int y = top; y <= bottom; y++)
        {
            for (int x = lx; x <= rx; x++)
            {
                Color fill = x < (lx + rx) / 2 ? baseCol : shadowCol;
                if (x == lx || x == rx || y == top)
                {
                    img.SetPixel(x, y, outlineCol);
                }
                else
                {
                    img.SetPixel(x, y, fill);
                }
            }
        }

        // Roof
        int roofHeight = 7;
        for (int y = top - roofHeight; y < top; y++)
        {
            int diff = y - (top - roofHeight);
            int width = diff * (rx - lx) / (roofHeight * 2);
            int mid = (lx + rx) / 2;

            for (int x = mid - width; x <= mid + width; x++)
            {
                if (x == mid - width || x == mid + width || y == top - roofHeight)
                {
                    img.SetPixel(x, y, outlineCol);
                }
                else
                {
                    img.SetPixel(x, y, roofCol);
                }
            }
        }
    }
}
