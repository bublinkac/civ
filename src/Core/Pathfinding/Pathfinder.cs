using System;
using System.Collections.Generic;
using Godot;

namespace CivGame.Core.Pathfinding;

public class Pathfinder
{
    private readonly GameMap _map;
    private readonly GameSimulation _sim;

    // Pre-allocated flat arrays to avoid allocations during searches
    private readonly float[] _gScore;
    private readonly int[] _cameFrom;
    private readonly MinHeap _openSet;
    private readonly bool[] _closedSet;
    private readonly int _width;
    private readonly int _height;
    private readonly int _mapSize;

    public Pathfinder(GameMap map, GameSimulation sim)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _sim = sim ?? throw new ArgumentNullException(nameof(sim));
        _width = map.Width;
        _height = map.Height;
        _mapSize = _width * _height;

        _gScore = new float[_mapSize];
        _cameFrom = new int[_mapSize];
        _closedSet = new bool[_mapSize];
        _openSet = new MinHeap(_mapSize);
    }

    /// <summary>
    /// Finds the optimal path for a unit from start to target.
    /// Returns a list of coordinates including the start and target, or null if no path is found.
    /// </summary>
    public List<(int X, int Y)>? FindPath(Unit unit, int startX, int startY, int targetX, int targetY, bool ignoreUnits = false)
    {
        if (unit == null) return null;
        if (!_map.IsInBounds(startX, startY) || !_map.IsInBounds(targetX, targetY)) return null;
        if (startX == targetX && startY == targetY)
        {
            return new List<(int, int)> { (startX, startY) };
        }

        // Target tile validation (land units cannot enter ocean)
        var targetTile = _map.GetTile(targetX, targetY);
        if (targetTile == null || targetTile.Terrain.Id == "ocean") return null;

        // Reset search states for the search space (only reset the touched elements if possible, 
        // but for safety and fast flat arrays, resetting arrays is extremely fast in C#)
        Array.Fill(_gScore, float.MaxValue);
        Array.Fill(_cameFrom, -1);
        Array.Fill(_closedSet, false);
        _openSet.Clear();

        int startIndex = ToIndex(startX, startY);
        int targetIndex = ToIndex(targetX, targetY);

        float maxMovement = unit.MaxMovement;

        // Set start node score
        // At start, turns = 0, remaining movement = RemainingMovement (or MaxMovement if initialized)
        float startRemainingMovement = unit.RemainingMovement > 0.0f ? unit.RemainingMovement : maxMovement;
        _gScore[startIndex] = maxMovement - startRemainingMovement; // turnCount = 0, cost is used MP
        _openSet.InsertOrUpdate(startIndex, _gScore[startIndex] + Heuristic(startX, startY, targetX, targetY, maxMovement));

        int[] dxs = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dys = { -1, -1, -1, 0, 0, 1, 1, 1 };

        bool found = false;

        while (_openSet.Size > 0)
        {
            int currIndex = _openSet.PopMin();
            if (currIndex == targetIndex)
            {
                found = true;
                break;
            }

            _closedSet[currIndex] = true;

            int cx = currIndex % _width;
            int cy = currIndex / _width;

            // Decode current turns and remaining MP from the gScore
            float currG = _gScore[currIndex];
            int currTurn = (int)(currG / maxMovement);
            float usedMpThisTurn = currG - (currTurn * maxMovement);
            float currMpRemaining = Math.Max(0.0f, maxMovement - usedMpThisTurn);

            // Explore 8-way neighbors
            for (int i = 0; i < 8; i++)
            {
                int nx = cx + dxs[i];
                int ny = cy + dys[i];

                if (!_map.IsInBounds(nx, ny)) continue;

                int neighborIndex = ToIndex(nx, ny);
                if (_closedSet[neighborIndex]) continue;

                var neighborTile = _map.GetTile(nx, ny);
                if (neighborTile == null || neighborTile.Terrain.Id == "ocean") continue;

                // Friendly and Hostile units check (except target tile which we can select to attack)
                if (!ignoreUnits && (nx != targetX || ny != targetY))
                {
                    var existingUnit = _sim.Units.Find(u => u.X == nx && u.Y == ny);
                    if (existingUnit != null)
                    {
                        // In Civ, you cannot move through enemy units. 
                        // If it is hostile, it blocks movement entirely (impassable).
                        if (_sim.IsHostile(unit, existingUnit))
                        {
                            continue;
                        }
                    }
                }

                // Calculate movement cost for this tile
                float moveCost = _sim.GetTileMovementCostForUnit(neighborTile, unit);

                // Determine turns and remaining movement points after entering the tile
                int nextTurn = currTurn;
                float nextMpRemaining;

                if (currMpRemaining <= 0.001f)
                {
                    // No movement left this turn, must wait until next turn
                    nextTurn++;
                    nextMpRemaining = Math.Max(0.0f, maxMovement - moveCost);
                }
                else
                {
                    // Has some movement left this turn
                    if (moveCost >= currMpRemaining)
                    {
                        // Entering consumes all remaining movement points
                        nextMpRemaining = 0.0f;
                    }
                    else
                    {
                        nextMpRemaining = currMpRemaining - moveCost;
                    }
                }

                // Encode turns and remaining movement points back into gScore
                float tentativeG = nextTurn * maxMovement + (maxMovement - nextMpRemaining);

                if (tentativeG < _gScore[neighborIndex])
                {
                    _cameFrom[neighborIndex] = currIndex;
                    _gScore[neighborIndex] = tentativeG;

                    float h = Heuristic(nx, ny, targetX, targetY, maxMovement);
                    _openSet.InsertOrUpdate(neighborIndex, tentativeG + h);
                }
            }
        }

        if (!found) return null;

        // Reconstruct path
        var path = new List<(int, int)>();
        int trace = targetIndex;
        while (trace != -1)
        {
            path.Add((trace % _width, trace / _width));
            trace = _cameFrom[trace];
        }

        path.Reverse();
        return path;
    }

    private int ToIndex(int x, int y)
    {
        return y * _width + x;
    }

    private float Heuristic(int x1, int y1, int x2, int y2, float maxMovement)
    {
        // Chebyshev distance (since 8-way movement is allowed)
        int dx = Math.Abs(x1 - x2);
        int dy = Math.Abs(y1 - y2);
        int dist = Math.Max(dx, dy);

        // Min movement cost is 0.0f (railroads) or 0.333f (roads) or 1.0f (plains/grasslands).
        // Using 0.0f makes it equivalent to Dijkstra's algorithm (always admissible, finding absolute shortest path).
        // However, to make it super fast while still perfectly correct, we can assume a minimum cost of 0.0f.
        // Actually, let's look at if we can use a small cost. If we use 0.0f, Heuristic is 0, which is perfectly safe.
        // Let's use a very conservative estimate of 0.0f to guarantee mathematical perfection with zero risk of overestimation.
        // Since Dijkstra on a flat array with our MinHeap is extremely fast anyway, it runs in a fraction of a millisecond.
        return dist * 0.0f;
    }
}

/// <summary>
/// A highly-optimized, zero-allocation binary min-heap priority queue designed specifically for Pathfinder nodes.
/// </summary>
internal class MinHeap
{
    private struct PathNode
    {
        public int Index;
        public float FScore;
    }

    private readonly PathNode[] _nodes;
    private readonly int[] _heapPos; // Maps mapIndex -> position in the heap (1-indexed), or -1 if not in heap
    private int _size;

    public MinHeap(int maxMapSize)
    {
        _nodes = new PathNode[maxMapSize + 1];
        _heapPos = new int[maxMapSize];
        Array.Fill(_heapPos, -1);
        _size = 0;
    }

    public int Size => _size;

    public void InsertOrUpdate(int index, float fScore)
    {
        int pos = _heapPos[index];
        if (pos == -1)
        {
            // Insert
            _size++;
            _nodes[_size] = new PathNode { Index = index, FScore = fScore };
            _heapPos[index] = _size;
            BubbleUp(_size);
        }
        else
        {
            // Update
            float oldScore = _nodes[pos].FScore;
            _nodes[pos].FScore = fScore;
            if (fScore < oldScore)
            {
                BubbleUp(pos);
            }
            else
            {
                BubbleDown(pos);
            }
        }
    }

    public int PopMin()
    {
        if (_size == 0) throw new InvalidOperationException("Heap is empty");
        int minIndex = _nodes[1].Index;
        _heapPos[minIndex] = -1;

        if (_size > 1)
        {
            _nodes[1] = _nodes[_size];
            _heapPos[_nodes[1].Index] = 1;
            _size--;
            BubbleDown(1);
        }
        else
        {
            _size = 0;
        }

        return minIndex;
    }

    public void Clear()
    {
        // Only clear the elements actually in the heap to avoid O(N) overhead of clearing the whole map
        for (int i = 1; i <= _size; i++)
        {
            _heapPos[_nodes[i].Index] = -1;
        }
        _size = 0;
    }

    private void BubbleUp(int pos)
    {
        var node = _nodes[pos];
        while (pos > 1)
        {
            int parent = pos / 2;
            if (_nodes[parent].FScore <= node.FScore) break;

            _nodes[pos] = _nodes[parent];
            _heapPos[_nodes[pos].Index] = pos;
            pos = parent;
        }
        _nodes[pos] = node;
        _heapPos[node.Index] = pos;
    }

    private void BubbleDown(int pos)
    {
        var node = _nodes[pos];
        while (pos * 2 <= _size)
        {
            int child = pos * 2;
            if (child < _size && _nodes[child + 1].FScore < _nodes[child].FScore)
            {
                child++;
            }
            if (node.FScore <= _nodes[child].FScore) break;

            _nodes[pos] = _nodes[child];
            _heapPos[_nodes[pos].Index] = pos;
            pos = child;
        }
        _nodes[pos] = node;
        _heapPos[node.Index] = pos;
    }
}
