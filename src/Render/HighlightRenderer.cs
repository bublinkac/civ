using System;
using System.Collections.Generic;
using Godot;
using CivGame.Core;

namespace CivGame.Render;

public partial class HighlightRenderer : TileMapLayer
{
    private const int SourceId = 0;

    public override void _Ready()
    {
        // Above terrain (0) and selection ring (1), but below fog (2)
        ZIndex = 1;
        ZAsRelative = false;

        var tileSet = new TileSet
        {
            TileShape = TileSet.TileShapeEnum.Isometric,
            TileLayout = TileSet.TileLayoutEnum.DiamondDown,
            TileSize = new Vector2I(256, 128)
        };

        var source = new TileSetAtlasSource
        {
            Texture = CreateHighlightTexture(),
            TextureRegionSize = new Vector2I(256, 128)
        };
        source.CreateTile(new Vector2I(0, 0));
        tileSet.AddSource(source, SourceId);

        this.TileSet = tileSet;
    }

    public void UpdateHighlight(GameSimulation sim, Unit? selectedUnit)
    {
        Clear();

        if (selectedUnit == null || selectedUnit.RemainingMovement <= 0) return;

        HashSet<(int X, int Y)> reachable = sim.GetReachableTiles(selectedUnit);

        foreach (var (x, y) in reachable)
        {
            // Only highlight tiles the player has already seen
            if (sim.VisibilityGrid[x, y] == FogState.Unexplored) continue;
            SetCell(new Vector2I(x, y), sourceId: SourceId, atlasCoords: new Vector2I(0, 0));
        }
    }

    private static Texture2D CreateHighlightTexture()
    {
        int width = 256;
        int height = 128;
        Image img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        Color fill = new Color(0.25f, 0.9f, 0.25f, 0.30f);
        Color border = new Color(0.15f, 0.75f, 0.15f, 0.80f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dx = Math.Abs(x - 128.0) / 128.0;
                double dy = Math.Abs(y - 64.0) / 64.0;
                double dist = dx + dy;

                if (dist <= 1.0)
                {
                    img.SetPixel(x, y, dist > 0.93 ? border : fill);
                }
            }
        }

        return ImageTexture.CreateFromImage(img);
    }
}
