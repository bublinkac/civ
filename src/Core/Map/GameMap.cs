using System;

namespace CivGame.Core;

public class GameMap
{
    public int Width { get; }
    public int Height { get; }
    public bool WrapsX { get; set; } = true;
    private readonly TileData[,] _tiles;

    public GameMap(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");

        Width = width;
        Height = height;
        _tiles = new TileData[width, height];
    }

    /// <summary>
    /// Normalizes X coordinate into the valid range [0, Width - 1] if WrapsX is enabled.
    /// </summary>
    public int NormalizeX(int x)
    {
        if (!WrapsX || Width <= 0) return x;
        int rem = x % Width;
        return rem < 0 ? rem + Width : rem;
    }

    /// <summary>
    /// Calculates shortest distance between two X coordinates on the cylindrical globe.
    /// </summary>
    public int GetDeltaX(int x1, int x2)
    {
        if (!WrapsX) return Math.Abs(x1 - x2);
        int dx = Math.Abs(NormalizeX(x1) - NormalizeX(x2));
        return Math.Min(dx, Width - dx);
    }

    /// <summary>
    /// Calculates Chebyshev distance (max(dx, dy)) taking world wrap into account.
    /// </summary>
    public int GetChebyshevDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Max(GetDeltaX(x1, x2), Math.Abs(y1 - y2));
    }

    /// <summary>
    /// Calculates Manhattan distance (dx + dy) taking world wrap into account.
    /// </summary>
    public int GetManhattanDistance(int x1, int y1, int x2, int y2)
    {
        return GetDeltaX(x1, x2) + Math.Abs(y1 - y2);
    }

    /// <summary>
    /// Calculates Civ3 distance (max(dx, dy) + min(dx, dy) / 2) taking world wrap into account.
    /// </summary>
    public int GetCiv3Distance(int x1, int y1, int x2, int y2)
    {
        int dx = GetDeltaX(x1, x2);
        int dy = Math.Abs(y1 - y2);
        return Math.Max(dx, dy) + Math.Min(dx, dy) / 2;
    }

    /// <summary>
    /// Determines the step direction (-1, 0, +1) along the X axis for the shortest path from fromX to toX.
    /// </summary>
    public int GetStepDirectionX(int fromX, int toX)
    {
        if (!WrapsX) return Math.Sign(toX - fromX);
        int normFrom = NormalizeX(fromX);
        int normTo = NormalizeX(toX);
        int diff = normTo - normFrom;
        if (diff == 0) return 0;
        if (diff > Width / 2) return -1;
        if (diff < -Width / 2) return 1;
        return Math.Sign(diff);
    }

    public bool IsInBounds(int x, int y)
    {
        if (y < 0 || y >= Height) return false;
        if (WrapsX) return true;
        return x >= 0 && x < Width;
    }

    public TileData? GetTile(int x, int y)
    {
        if (y < 0 || y >= Height) return null;
        int nx = NormalizeX(x);
        if (!WrapsX && (nx < 0 || nx >= Width)) return null;
        return _tiles[nx, y];
    }

    public void SetTile(int x, int y, TileData tile)
    {
        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), $"Coordinates ({x}, {y}) are out of map height bounds ({Height}).");
        }
        int nx = NormalizeX(x);
        if (!WrapsX && (nx < 0 || nx >= Width))
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Coordinates ({x}, {y}) are out of map width bounds ({Width}).");
        }
        _tiles[nx, y] = tile;
    }

    /// <summary>
    /// Computes landmass/continent IDs for all non-ocean tiles using flood-fill.
    /// Ocean tiles receive ContinentId = 0.
    /// Supports cylindrical world wrap across the East/West seam.
    /// </summary>
    public void CalculateContinents()
    {
        var visited = new bool[Width, Height];
        int currentContinent = 0;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var tile = _tiles[x, y];
                if (tile == null || tile.IsOcean || visited[x, y])
                {
                    if (tile != null && tile.IsOcean) tile.ContinentId = 0;
                    continue;
                }

                currentContinent++;
                var queue = new System.Collections.Generic.Queue<(int X, int Y)>();
                queue.Enqueue((x, y));
                visited[x, y] = true;
                tile.ContinentId = currentContinent;

                while (queue.Count > 0)
                {
                    var (currX, currY) = queue.Dequeue();
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = NormalizeX(currX + dx);
                            int ny = currY + dy;
                            if (ny >= 0 && ny < Height && !visited[nx, ny])
                            {
                                var nTile = _tiles[nx, ny];
                                if (nTile != null && !nTile.IsOcean)
                                {
                                    visited[nx, ny] = true;
                                    nTile.ContinentId = currentContinent;
                                    queue.Enqueue((nx, ny));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks whether two tile coordinates belong to the same connected landmass/continent.
    /// </summary>
    public bool AreOnSameContinent(int x1, int y1, int x2, int y2)
    {
        var t1 = GetTile(x1, y1);
        var t2 = GetTile(x2, y2);
        if (t1 == null || t2 == null || t1.IsOcean || t2.IsOcean) return false;
        return t1.ContinentId != 0 && t1.ContinentId == t2.ContinentId;
    }
}
