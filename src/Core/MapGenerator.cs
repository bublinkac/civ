using System;
using System.Collections.Generic;

namespace CivGame.Core;

public class MapGenerator
{
    private readonly int _seed;
    private readonly int[] _permutation;

    public MapGenerator(int seed)
    {
        _seed = seed;
        _permutation = GeneratePermutationTable(seed);
    }

    public GameMap Generate(int width, int height)
    {
        var map = new GameMap(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Scale coordinates for noise computation
                double nx = (double)x / width;
                double ny = (double)y / height;
                double latitude = Math.Abs(((double)y / Math.Max(1, height - 1)) - 0.5) * 2.0;

                // Multi-octave Perlin noise for elevation and moisture
                double elevation = NoiseOctave(nx * 3.5, ny * 3.5, 4, 0.5);
                double moisture = NoiseOctave(nx * 4.5 + 15.0, ny * 4.5 + 15.0, 3, 0.5);

                TerrainType terrainType = DetermineTerrain(elevation, moisture, latitude);
                Resource? resource = DetermineResource(terrainType, x, y);
                Terrain terrain = TerrainRegistry.Get(terrainType.ToString().ToLower())!;

                map.SetTile(x, y, new TileData(x, y, terrain, resource));
            }
        }

        return map;
    }

    private static TerrainType DetermineTerrain(double elevation, double moisture, double latitude)
    {
        // 1. Water bands
        if (elevation < 0.28)
        {
            return TerrainType.Ocean;
        }
        if (elevation < 0.36)
        {
            return TerrainType.Sea;
        }
        if (elevation < 0.44)
        {
            return TerrainType.Coast;
        }

        // 2. Polar bands
        if (latitude > 0.72)
        {
            return TerrainType.Tundra;
        }

        // 3. High altitude check
        if (elevation > 0.86)
        {
            return TerrainType.Mountain;
        }
        if (elevation > 0.77)
        {
            return TerrainType.Hills;
        }

        // 4. Moisture-based flatland/vegetation allocation
        if (moisture < 0.16)
        {
            return TerrainType.Desert;
        }
        if (moisture < 0.26)
        {
            return TerrainType.Floodplains;
        }
        if (moisture > 0.80)
        {
            return TerrainType.Marsh;
        }
        if (moisture > 0.68)
        {
            return latitude < 0.45 ? TerrainType.Jungle : TerrainType.Forest;
        }
        if (moisture > 0.54)
        {
            return TerrainType.Forest;
        }
        if (moisture > 0.36)
        {
            return TerrainType.Grassland;
        }

        return TerrainType.Plains;
    }

    private static readonly Dictionary<TerrainType, string> _terrainTypeToId = new()
    {
        {TerrainType.Coast, "coast"},
        {TerrainType.Desert, "desert"},
        {TerrainType.Floodplains, "floodplains"},
        {TerrainType.Forest, "forest"},
        {TerrainType.Grassland, "grassland"},
        {TerrainType.Hills, "hills"},
        {TerrainType.Jungle, "jungle"},
        {TerrainType.Marsh, "marsh"},
        {TerrainType.Mountain, "mountain"},
        {TerrainType.Ocean, "ocean"},
        {TerrainType.Plains, "plains"},
        {TerrainType.Sea, "sea"},
        {TerrainType.Tundra, "tundra"}
    };

    private Resource? DetermineResource(TerrainType terrainType, int x, int y)
    {
        if (!_terrainTypeToId.TryGetValue(terrainType, out var terrainId)) return null;
        
        int hash = (x * 73856093) ^ (y * 19349663) ^ _seed;
        double rand = (double)(Math.Abs(hash) % 1000) / 1000.0;

        double bonusChance = 0.08;
        double luxuryChance = 0.04;
        double strategicChance = 0.04;

        var candidates = new System.Collections.Generic.List<Resource>();
        foreach (var res in ResourceRegistry.All.Values)
        {
            if (!res.CanSpawnOn(terrainId)) continue;
            
            if (res is StrategicResource && rand < strategicChance) candidates.Add(res);
            else if (res is LuxuryResource && rand >= strategicChance && rand < strategicChance + luxuryChance) candidates.Add(res);
            else if (res is BonusResource && rand >= strategicChance + luxuryChance && rand < strategicChance + luxuryChance + bonusChance) candidates.Add(res);
        }

        if (candidates.Count == 0) return null;
        
        int pickHash = (x * 19349663) ^ (y * 73856093) ^ (_seed ^ 0x5A5A5A5A);
        int index = Math.Abs(pickHash) % candidates.Count;
        return candidates[index];
    }

    // --- Pure Mathematical Perlin Noise Engine ---

    private double NoiseOctave(double x, double y, int octaves, double persistence)
    {
        double total = 0;
        double frequency = 1;
        double amplitude = 1;
        double maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += GetNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2;
        }

        // Normalise result to [0, 1] range
        return (total / maxValue + 1.0) / 2.0;
    }

    private double GetNoise(double x, double y)
    {
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;

        double xf = x - Math.Floor(x);
        double yf = y - Math.Floor(y);

        double u = Fade(xf);
        double v = Fade(yf);

        int aa = _permutation[_permutation[X] + Y];
        int ab = _permutation[_permutation[X] + Y + 1];
        int ba = _permutation[_permutation[X + 1] + Y];
        int bb = _permutation[_permutation[X + 1] + Y + 1];

        double x1 = Lerp(u, Grad(aa, xf, yf), Grad(ba, xf - 1, yf));
        double x2 = Lerp(u, Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1));

        return Lerp(v, x1, x2);
    }

    private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
    
    private static double Lerp(double t, double a, double b) => a + t * (b - a);

    private static double Grad(int hash, double x, double y)
    {
        return (hash & 7) switch
        {
            0 => x + y,
            1 => -x + y,
            2 => x - y,
            3 => -x - y,
            4 => x,
            5 => -x,
            6 => y,
            7 => -y,
            _ => 0.0
        };
    }

    private static int[] GeneratePermutationTable(int seed)
    {
        var p = new int[512];
        var source = new int[256];
        for (int i = 0; i < 256; i++)
        {
            source[i] = i;
        }

        // Seeded Fisher-Yates shuffle
        var rand = new Random(seed);
        for (int i = 255; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            int temp = source[i];
            source[i] = source[j];
            source[j] = temp;
        }

        // Duplicate table for coordinate wrapping without array boundary checks
        for (int i = 0; i < 512; i++)
        {
            p[i] = source[i & 255];
        }

        return p;
    }
}
