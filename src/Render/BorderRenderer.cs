using System;
using Godot;
using CivGame.Core;

namespace CivGame.Render;

public partial class BorderRenderer : TileMapLayer
{
    private const int PlayerSourceId = 0;
    private const int AiSourceId = 1;

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

        // Player border: Gold (85% alpha)
        var goldColor = new Color(0.95f, 0.72f, 0.12f, 0.85f);
        var playerSource = new TileSetAtlasSource
        {
            Texture = CreateBorderTexture(goldColor, "border.png"),
            TextureRegionSize = new Vector2I(256, 128)
        };
        playerSource.CreateTile(new Vector2I(0, 0));
        tileSet.AddSource(playerSource, PlayerSourceId);

        // AI border: Royal Purple (85% alpha)
        var purpleColor = new Color(0.6f, 0.2f, 0.9f, 0.85f);
        var aiSource = new TileSetAtlasSource
        {
            Texture = CreateBorderTexture(purpleColor, "border_ai.png"),
            TextureRegionSize = new Vector2I(256, 128)
        };
        aiSource.CreateTile(new Vector2I(0, 0));
        tileSet.AddSource(aiSource, AiSourceId);

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
                            int sourceId = city.Faction == Faction.AiRival ? AiSourceId : PlayerSourceId;
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
