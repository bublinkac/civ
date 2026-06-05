using System;
using System.Collections.Generic;
using Godot;
using CivGame.Core;

namespace CivGame.Render;

public partial class UnitRenderer : Node2D
{
    private MapRenderer? _mapRenderer;
    private readonly Dictionary<string, Sprite2D> _sprites = new();
    private Sprite2D? _selectionRing;
    private string? _selectedUnitId;

    public override void _Ready()
    {
        // Stand on top of terrain, but below the Fog overlay
        ZIndex = 1;

        // Create the selection ring
        SetupSelectionRing();

        // Cache textures
        GetOrCreateUnitTexture(UnitType.Explorer);
        GetOrCreateUnitTexture(UnitType.Settler);
        GetOrCreateUnitTexture(UnitType.Warrior);
        GetOrCreateUnitTexture(UnitType.Archer);
        GetOrCreateUnitTexture(UnitType.Barbarian);
        GetOrCreateUnitTexture(UnitType.Worker);
    }

    public void Initialize(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
    }

    private void SetupSelectionRing()
    {
        _selectionRing = new Sprite2D();
        var img = Image.CreateEmpty(128, 64, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        // Draw a glowing yellow isometric ring (2:1 ellipse)
        Color ringColor = new Color(1.0f, 0.9f, 0.0f, 0.85f);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                double dx = (x - 64.0) / 64.0;
                double dy = (y - 32.0) / 32.0;
                double dist = dx * dx + dy * dy;

                if (dist >= 0.85 && dist <= 1.0)
                {
                    img.SetPixel(x, y, ringColor);
                }
            }
        }

        _selectionRing.Texture = ImageTexture.CreateFromImage(img);
        _selectionRing.Visible = false;
        AddChild(_selectionRing);
    }

    public void UpdateUnits(GameSimulation sim, string? selectedUnitId)
    {
        if (_mapRenderer == null) return;

        _selectedUnitId = selectedUnitId;
        var activeIds = new HashSet<string>();

        foreach (var unit in sim.Units)
        {
            activeIds.Add(unit.Id);
            Vector2 targetPos = _mapRenderer.MapToLocal(new Vector2I(unit.X, unit.Y));
            
            // Adjust height offset so the unit stands upright on the center of the tile
            targetPos.Y -= 16; 

            if (!_sprites.TryGetValue(unit.Id, out var sprite))
            {
                sprite = new Sprite2D
                {
                    Texture = GetOrCreateUnitTexture(unit.Type),
                    Position = targetPos,
                    YSortEnabled = true
                };
                AddChild(sprite);
                _sprites[unit.Id] = sprite;
            }
            else
            {
                // Smooth slide tween for unit movements
                if (sprite.Position != targetPos)
                {
                    var tween = CreateTween();
                    tween.TweenProperty(sprite, "position", targetPos, 0.22f)
                         .SetTrans(Tween.TransitionType.Quad)
                         .SetEase(Tween.EaseType.Out);
                }
            }

            // Update selection ring position under the selected unit
            if (unit.Id == selectedUnitId)
            {
                _selectionRing!.Visible = true;
                _selectionRing.Position = _mapRenderer.MapToLocal(new Vector2I(unit.X, unit.Y));
            }
        }

        if (string.IsNullOrEmpty(selectedUnitId))
        {
            _selectionRing!.Visible = false;
        }

        // Remove sprites of obsolete/dead units
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

    private Texture2D GetOrCreateUnitTexture(UnitType type)
    {
        string dirPath = "res://assets";
        string fileName = type.ToString().ToLower() + ".png";
        string filePath = $"{dirPath}/{fileName}";

        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(dirPath);
        }

        Image img = GenerateUnitImage(type);

        try
        {
            img.SavePng(filePath);
        }
        catch (Exception ex)
        {
            GD.Print($"[UnitRenderer] Warning: Could not save PNG to {filePath}: {ex.Message}");
        }

        return ImageTexture.CreateFromImage(img);
    }

    private Image GenerateUnitImage(UnitType type)
    {
        int width = 64;
        int height = 64;
        Image img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        if (type == UnitType.Explorer)
        {
            // Green-hooded scout figure with boots
            Color cloakColor = new Color(0.2f, 0.5f, 0.15f);
            Color faceColor = new Color(0.95f, 0.8f, 0.7f);
            Color bootColor = new Color(0.4f, 0.25f, 0.1f);
            Color hatColor = new Color(0.15f, 0.35f, 0.1f);

            // Draw legs
            for (int y = 46; y <= 56; y++)
            {
                img.SetPixel(26, y, bootColor);
                img.SetPixel(36, y, bootColor);
            }
            // Draw boots
            for (int x = 24; x <= 27; x++) img.SetPixel(x, 56, bootColor);
            for (int x = 36; x <= 39; x++) img.SetPixel(x, 56, bootColor);

            // Draw cloak/body
            for (int y = 24; y <= 45; y++)
            {
                int r = y > 35 ? 12 : 10;
                for (int x = 32 - r; x <= 32 + r; x++)
                {
                    img.SetPixel(x, y, cloakColor);
                }
            }

            // Draw face
            for (int y = 14; y <= 23; y++)
            {
                for (int x = 26; x <= 38; x++)
                {
                    img.SetPixel(x, y, faceColor);
                }
            }
            img.SetPixel(29, 18, Colors.Black);
            img.SetPixel(35, 18, Colors.Black);

            // Draw scout hat
            for (int y = 6; y <= 13; y++)
            {
                int w = y == 13 ? 18 : (13 - y) * 2;
                for (int x = 32 - w; x <= 32 + w; x++)
                {
                    img.SetPixel(x, y, hatColor);
                }
            }
        }
        else if (type == UnitType.Warrior)
        {
            // Blue-shielded soldier with iron helmet
            Color armorColor = new Color(0.45f, 0.45f, 0.48f);
            Color skinColor = new Color(0.95f, 0.8f, 0.7f);
            Color shieldColor = new Color(0.15f, 0.25f, 0.75f);
            Color ironColor = new Color(0.6f, 0.62f, 0.65f);

            // Legs
            for (int y = 46; y <= 56; y++)
            {
                img.SetPixel(28, y, armorColor);
                img.SetPixel(34, y, armorColor);
            }

            // Body (armor)
            for (int y = 24; y <= 45; y++)
            {
                for (int x = 22; x <= 40; x++)
                {
                    img.SetPixel(x, y, armorColor);
                }
            }

            // Head & Helmet
            for (int y = 14; y <= 23; y++)
            {
                for (int x = 26; x <= 36; x++)
                {
                    img.SetPixel(x, y, skinColor);
                }
            }
            // Eyes
            img.SetPixel(29, 18, Colors.Black);
            img.SetPixel(33, 18, Colors.Black);

            // Iron Helmet
            for (int y = 8; y <= 14; y++)
            {
                for (int x = 25; x <= 37; x++)
                {
                    img.SetPixel(x, y, ironColor);
                }
            }

            // Draw Blue Shield in right hand
            for (int y = 28; y <= 44; y++)
            {
                for (int x = 39; x <= 45; x++)
                {
                    img.SetPixel(x, y, shieldColor);
                }
            }
        }
        else if (type == UnitType.Archer)
        {
            // Brown-orange Archer holding a wooden bow
            Color cloakColor = new Color(0.75f, 0.45f, 0.15f);
            Color shirtColor = new Color(0.85f, 0.75f, 0.25f);
            Color faceColor = new Color(0.95f, 0.8f, 0.7f);
            Color bowColor = new Color(0.48f, 0.3f, 0.1f);

            // Legs
            for (int y = 46; y <= 56; y++)
            {
                img.SetPixel(28, y, bowColor);
                img.SetPixel(34, y, bowColor);
            }

            // Body (shirt & cloak)
            for (int y = 24; y <= 45; y++)
            {
                for (int x = 22; x <= 40; x++)
                {
                    img.SetPixel(x, y, y > 34 ? cloakColor : shirtColor);
                }
            }

            // Head
            for (int y = 14; y <= 23; y++)
            {
                for (int x = 26; x <= 36; x++)
                {
                    img.SetPixel(x, y, faceColor);
                }
            }
            img.SetPixel(29, 18, Colors.Black);
            img.SetPixel(33, 18, Colors.Black);

            // Archer Hat
            for (int y = 8; y <= 13; y++)
            {
                for (int x = 27; x <= 35; x++)
                {
                    img.SetPixel(x, y, cloakColor);
                }
            }

            // Wooden bow on left hand (drawn as a curve)
            for (int y = 20; y <= 40; y++)
            {
                img.SetPixel(19, y, bowColor);
            }
            img.SetPixel(18, 22, bowColor);
            img.SetPixel(18, 38, bowColor);
        }
        else if (type == UnitType.Barbarian)
        {
            // Aggressive red-clothed warrior holding a massive black axe
            Color skinColor = new Color(0.95f, 0.8f, 0.7f);
            Color bodyColor = new Color(0.75f, 0.12f, 0.12f); // Crimson red
            Color hairColor = new Color(0.12f, 0.12f, 0.12f); // Black hair/fur
            Color axeColor = new Color(0.2f, 0.2f, 0.22f); // Dark steel

            // Legs
            for (int y = 46; y <= 56; y++)
            {
                img.SetPixel(27, y, hairColor);
                img.SetPixel(35, y, hairColor);
            }

            // Body
            for (int y = 24; y <= 45; y++)
            {
                for (int x = 22; x <= 40; x++)
                {
                    img.SetPixel(x, y, bodyColor);
                }
            }

            // Face
            for (int y = 14; y <= 23; y++)
            {
                for (int x = 26; x <= 36; x++)
                {
                    img.SetPixel(x, y, skinColor);
                }
            }
            // Angry red/black eyes
            img.SetPixel(29, 18, Colors.Red);
            img.SetPixel(33, 18, Colors.Red);

            // Shaggy black hair
            for (int y = 6; y <= 14; y++)
            {
                img.SetPixel(25, y, hairColor);
                img.SetPixel(37, y, hairColor);
            }
            for (int x = 25; x <= 37; x++) img.SetPixel(x, 6, hairColor);

            // Big Black Battleaxe on the side
            for (int y = 16; y <= 48; y++)
            {
                img.SetPixel(42, y, axeColor); // Shaft
            }
            // Blade
            for (int y = 18; y <= 26; y++)
            {
                for (int x = 43; x <= 50; x++)
                {
                    img.SetPixel(x, y, axeColor);
                }
            }
        }
        else if (type == UnitType.Worker)
        {
            // Shovel-carrying builder with leather tunic and straw hat
            Color leatherColor = new Color(0.55f, 0.4f, 0.2f);
            Color skinColor = new Color(0.95f, 0.8f, 0.7f);
            Color strawColor = new Color(0.85f, 0.82f, 0.45f);
            Color metalColor = new Color(0.7f, 0.7f, 0.75f);
            Color woodColor = new Color(0.45f, 0.3f, 0.15f);

            // Legs
            for (int y = 46; y <= 56; y++)
            {
                img.SetPixel(28, y, leatherColor);
                img.SetPixel(34, y, leatherColor);
            }

            // Body (leather tunic)
            for (int y = 24; y <= 45; y++)
            {
                for (int x = 22; x <= 40; x++)
                {
                    img.SetPixel(x, y, leatherColor);
                }
            }

            // Head
            for (int y = 14; y <= 23; y++)
            {
                for (int x = 26; x <= 36; x++)
                {
                    img.SetPixel(x, y, skinColor);
                }
            }
            img.SetPixel(29, 18, Colors.Black);
            img.SetPixel(33, 18, Colors.Black);

            // Straw Hat (flat circle hat)
            for (int y = 8; y <= 13; y++)
            {
                int w = y == 13 ? 18 : 10;
                for (int x = 32 - w; x <= 32 + w; x++)
                {
                    img.SetPixel(x, y, strawColor);
                }
            }

            // Wooden shovel shaft on the left hand
            for (int y = 20; y <= 46; y++)
            {
                img.SetPixel(18, y, woodColor);
            }
            // Shovel iron head
            for (int y = 44; y <= 50; y++)
            {
                for (int x = 15; x <= 21; x++)
                {
                    img.SetPixel(x, y, metalColor);
                }
            }
        }
        else // Settler
        {
            // Canvas cart/wagon
            Color woodColor = new Color(0.55f, 0.35f, 0.15f);
            Color ironColor = new Color(0.4f, 0.4f, 0.4f);
            Color canvasColor = new Color(0.92f, 0.9f, 0.85f);
            Color canvasShadow = new Color(0.8f, 0.77f, 0.73f);

            // Wheels
            DrawCircle(img, 18, 48, 8, ironColor, woodColor);
            DrawCircle(img, 46, 48, 8, ironColor, woodColor);

            // Wagon base
            for (int y = 34; y <= 42; y++)
            {
                for (int x = 10; x <= 54; x++)
                {
                    img.SetPixel(x, y, woodColor);
                }
            }
            // Iron outlines
            for (int x = 10; x <= 54; x++)
            {
                img.SetPixel(x, 34, ironColor);
                img.SetPixel(x, 42, ironColor);
            }

            // Wagon canvas arch
            for (int y = 12; y <= 33; y++)
            {
                for (int x = 12; x <= 52; x++)
                {
                    double cx = (x - 32.0) / 20.0;
                    double cy = (y - 33.0) / 21.0;
                    double dist = cx * cx + cy * cy;

                    if (dist <= 1.0)
                    {
                        Color c = x % 10 == 0 ? canvasShadow : canvasColor;
                        img.SetPixel(x, y, c);
                    }
                }
            }
        }

        return img;
    }

    private void DrawCircle(Image img, int cx, int cy, int r, Color border, Color fill)
    {
        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x >= 0 && x < img.GetWidth() && y >= 0 && y < img.GetHeight())
                {
                    double dist = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist <= r)
                    {
                        img.SetPixel(x, y, dist >= r - 1.5 ? border : fill);
                    }
                }
            }
        }
    }
}
