using System;
using Godot;
using CivGame.Core;

namespace CivGame.Render;

public partial class BorderRenderer : TileMapLayer
{
    private static readonly (Faction Faction, Color Color, string FileName)[] FactionBorderStyles = new[]
    {
        (Faction.Player, new Color(0.95f, 0.72f, 0.12f, 0.85f), "border.png"),
        (Faction.AiRival, new Color(0.6f, 0.2f, 0.9f, 0.85f), "border_ai.png"),
        (Faction.AiRival2, new Color(0.85f, 0.15f, 0.15f, 0.85f), "border_ai2.png"),
        (Faction.AiRival3, new Color(0.1f, 0.75f, 0.8f, 0.85f), "border_ai3.png"),
        (Faction.AiRival4, new Color(0.15f, 0.75f, 0.25f, 0.85f), "border_ai4.png"),
        (Faction.AiRival5, new Color(0.95f, 0.50f, 0.10f, 0.85f), "border_ai5.png"),
        (Faction.AiRival6, new Color(0.2f, 0.35f, 0.85f, 0.85f), "border_ai6.png"),
        (Faction.AiRival7, new Color(0.85f, 0.2f, 0.6f, 0.85f), "border_ai7.png"),
    };

    public override void _Ready()
    {
        // Render borders on top of terrain (0), but below units/cities (1) and fog (2)
        ZIndex = 1;
        ZAsRelative = false;

        var tileSet = new TileSet
        {
            TileShape = TileSet.TileShapeEnum.Isometric,
            TileLayout = TileSet.TileLayoutEnum.DiamondDown,
            TileSize = new Vector2I(256, 128)
        };

        foreach (var style in FactionBorderStyles)
        {
            var source = new TileSetAtlasSource
            {
                Texture = CreateBorderTexture(style.Color, style.FileName),
                TextureRegionSize = new Vector2I(256, 128)
            };
            source.CreateTile(new Vector2I(0, 0));
            tileSet.AddSource(source, (int)style.Faction);
        }

        this.TileSet = tileSet;
    }

    public void UpdateBorders(GameSimulation sim)
    {
        Clear();

        for (int x = 0; x < sim.Map.Width; x++)
        {
            for (int y = 0; y < sim.Map.Height; y++)
            {
                var tile = sim.Map.GetTile(x, y);
                if (tile != null && !string.IsNullOrEmpty(tile.OwnerCityId))
                {
                    // Find city of this tile to determine its faction
                    var city = sim.Cities.Find(c => c.Id == tile.OwnerCityId);
                    if (city != null)
                    {
                        // Only render borders on explored/visible tiles
                        if (sim.VisibilityGrid[x, y] != FogState.Unexplored)
                        {
                            int sourceId = (int)city.Faction;
                            SetCell(new Vector2I(x, y), sourceId: sourceId, atlasCoords: new Vector2I(0, 0));
                        }
                    }
                }
            }
        }
    }

    private static Texture2D CreateBorderTexture(Color borderColor, string fileName)
    {
        int width = 256;
        int height = 128;
        Image img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dx = Math.Abs(x - 128.0) / 128.0;
                double dy = Math.Abs(y - 64.0) / 64.0;
                double dist = dx + dy;

                // Draw a thin outline at the outer edges of the tile
                if (dist >= 0.94 && dist <= 1.0)
                {
                    img.SetPixel(x, y, borderColor);
                }
            }
        }

        string dirPath = "res://assets";
        string filePath = $"{dirPath}/{fileName}";

        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(dirPath);
        }

        try
        {
            img.SavePng(filePath);
        }
        catch (Exception ex)
        {
            GD.Print($"[BorderRenderer] Warning: Could not save PNG to {filePath}: {ex.Message}");
        }

        return ImageTexture.CreateFromImage(img);
    }
}
