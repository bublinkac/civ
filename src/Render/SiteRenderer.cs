using System;
using System.Collections.Generic;
using Godot;
using CivGame.Core;

namespace CivGame.Render;

public partial class SiteRenderer : Node2D
{
    private MapRenderer? _mapRenderer;
    private readonly Dictionary<string, Node2D> _nodes = new();

    public override void _Ready()
    {
        ZIndex = 1;
        YSortEnabled = true;
    }

    public void Initialize(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
    }

    public void UpdateSites(GameSimulation sim)
    {
        if (_mapRenderer == null) return;

        var activeIds = new HashSet<string>();

        // Render Goody Huts
        foreach (var hut in sim.GoodyHuts)
        {
            string id = $"hut_{hut.X}_{hut.Y}";
            activeIds.Add(id);
            if (!_nodes.ContainsKey(id))
            {
                var node = CreateTextNode(hut.X, hut.Y, "🛖", new Color(0.8f, 0.8f, 0.2f));
                AddChild(node);
                _nodes[id] = node;
            }
        }

        // Render Barbarian Camps
        foreach (var camp in sim.BarbarianCamps)
        {
            string id = $"camp_{camp.X}_{camp.Y}";
            activeIds.Add(id);
            if (!_nodes.ContainsKey(id))
            {
                var node = CreateTextNode(camp.X, camp.Y, "⛺", new Color(0.8f, 0.2f, 0.2f));
                AddChild(node);
                _nodes[id] = node;
            }
        }

        var toRemove = new List<string>();
        foreach (var id in _nodes.Keys)
        {
            if (!activeIds.Contains(id))
            {
                _nodes[id].QueueFree();
                toRemove.Add(id);
            }
        }
        foreach (var id in toRemove)
        {
            _nodes.Remove(id);
        }
    }

    private Node2D CreateTextNode(int tx, int ty, string text, Color color)
    {
        var node = new Node2D();
        node.Position = _mapRenderer!.MapToLocal(new Vector2I(tx, ty));
        node.YSortEnabled = true;
        
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = new Vector2(-20, -32),
            Size = new Vector2(40, 40)
        };
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
        label.AddThemeConstantOverride("outline_size", 4);

        node.AddChild(label);
        return node;
    }
}
