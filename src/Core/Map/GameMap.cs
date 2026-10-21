using System;

namespace CivGame.Core;

public class GameMap
{
    public int Width { get; }
    public int Height { get; }
    private readonly TileData[,] _tiles;

    public GameMap(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");

        Width = width;
        Height = height;
        _tiles = new TileData[width, height];
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public TileData? GetTile(int x, int y)
    {
        return IsInBounds(x, y) ? _tiles[x, y] : null;
    }

    public void SetTile(int x, int y, TileData tile)
    {
        if (!IsInBounds(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Coordinates ({x}, {y}) are out of map bounds ({Width}x{Height}).");
        }
        _tiles[x, y] = tile;
    }
}
