using System;
using Godot;
using CivGame.Core;

namespace CivGame.Render;

public partial class FogRenderer : TileMapLayer
{
    public override void _Ready()
    {
        // Renders on top of terrain and units
        ZIndex = 2;

        // Programmatically configure TileSet for Isometric layout
        SetupTileSet();
    }

    private void SetupTileSet()
    {
        var tileSet = new TileSet
        {
            TileShape = TileSet.TileShapeEnum.Isometric,
            TileLayout = TileSet.TileLayoutEnum.DiamondDown,
            TileSize = new Vector2I(256, 128)
        };

        // Cache and load textures for solid and translucent overlays
        Texture2D unexploredTex = GetOrCreateFogTexture(FogState.Unexplored);
        Texture2D shroudedTex = GetOrCreateFogTexture(FogState.Shrouded);

        var sourceUnexplored = new TileSetAtlasSource
        {
            Texture = unexploredTex,
            TextureRegionSize = new Vector2I(256, 128)
        };
        sourceUnexplored.CreateTile(new Vector2I(0, 0));
        tileSet.AddSource(sourceUnexplored, (int)FogState.Unexplored);

        var sourceShrouded = new TileSetAtlasSource
        {
            Texture = shroudedTex,
            TextureRegionSize = new Vector2I(256, 128)
        };
        sourceShrouded.CreateTile(new Vector2I(0, 0));
        tileSet.AddSource(sourceShrouded, (int)FogState.Shrouded);

        this.TileSet = tileSet;
    }

    private Texture2D GetOrCreateFogTexture(FogState state)
    {
        string dirPath = "res://assets";
        string fileName = state.ToString().ToLower() + ".png";
        string filePath = $"{dirPath}/{fileName}";

        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(dirPath);
        }

        int width = 256;
        int height = 128;
        Image img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        // Use deep opaque black for unexplored, semi-transparent black for shrouded
        Color fogColor = state == FogState.Unexplored 
            ? new Color(0, 0, 0, 1.0f) 
            : new Color(0, 0, 0, 0.65f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dx = Math.Abs(x - 128.0) / 128.0;
                double dy = Math.Abs(y - 64.0) / 64.0;
                double dist = dx + dy;

                if (dist <= 1.0)
                {
                    img.SetPixel(x, y, fogColor);
                }
            }
        }

        try
        {
            img.SavePng(filePath);
        }
        catch (Exception ex)
        {
            GD.Print($"[FogRenderer] Warning: Could not save PNG to {filePath}: {ex.Message}");
        }

        return ImageTexture.CreateFromImage(img);
    }

    public void UpdateFog(GameSimulation sim)
    {
        Clear();

        for (int x = 0; x < sim.Map.Width; x++)
        {
            for (int y = 0; y < sim.Map.Height; y++)
            {
                FogState state = sim.VisibilityGrid[x, y];
                if (state == FogState.Unexplored)
                {
                    SetCell(new Vector2I(x, y), sourceId: (int)FogState.Unexplored, atlasCoords: new Vector2I(0, 0));
                }
                else if (state == FogState.Shrouded)
                {
                    SetCell(new Vector2I(x, y), sourceId: (int)FogState.Shrouded, atlasCoords: new Vector2I(0, 0));
                }
            }
        }
    }
}
