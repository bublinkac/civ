using System;
using Godot;
using CivGame.Core;

namespace CivGame.Render;

public partial class PollutionRenderer : TileMapLayer
{
    private const int SourceId = 0;

    public override void _Ready()
    {
        // Render pollution on top of terrain (0), highlight/borders (1), but below fog (2)
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
            Texture = CreatePollutionTexture(),
            TextureRegionSize = new Vector2I(256, 128)
        };
        source.CreateTile(new Vector2I(0, 0));
        tileSet.AddSource(source, SourceId);

        this.TileSet = tileSet;
    }

    public void UpdatePollution(GameSimulation sim)
    {
        Clear();

        for (int x = 0; x < sim.Map.Width; x++)
        {
            for (int y = 0; y < sim.Map.Height; y++)
            {
                var tile = sim.Map.GetTile(x, y);
                if (tile != null && tile.IsPolluted)
                {
                    // Only draw pollution if it's not hidden under fog of war
                    if (sim.VisibilityGrid[x, y] != FogState.Unexplored)
                    {
                        SetCell(new Vector2I(x, y), sourceId: SourceId, atlasCoords: new Vector2I(0, 0));
                    }
                }
            }
        }
    }

    private static Texture2D CreatePollutionTexture()
    {
        int width = 256;
        int height = 128;
        Image img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        // Classic Civ3 pollution colors: orange/brown dirt and toxic green sludge spots
        Color pollutionColor = new Color(0.65f, 0.40f, 0.15f, 0.55f); // Dirty brown/orange mud
        Color sludgeColor = new Color(0.18f, 0.48f, 0.08f, 0.70f);    // Toxic green bubbling spots

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dx = Math.Abs(x - 128.0) / 128.0;
                double dy = Math.Abs(y - 64.0) / 64.0;
                double dist = dx + dy;

                if (dist <= 0.85) // Fit neatly inside the tile boundary with a small margin
                {
                    // Create a pseudo-noise pattern for organic, bubbling splatters
                    double noise = (Math.Sin(x * 0.15) * Math.Cos(y * 0.15) + Math.Sin(x * 0.05) + Math.Cos(y * 0.05)) / 4.0 + 0.5;

                    if (noise > 0.65)
                    {
                        img.SetPixel(x, y, sludgeColor);
                    }
                    else if (noise > 0.35)
                    {
                        img.SetPixel(x, y, pollutionColor);
                    }
                }
            }
        }

        return ImageTexture.CreateFromImage(img);
    }
}
