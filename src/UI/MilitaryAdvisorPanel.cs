using System;
using Godot;
using CivGame.Core;
using System.Linq;
using System.Collections.Generic;

namespace CivGame.UI;

public partial class MilitaryAdvisorPanel : PanelContainer
{
    private GameSimulation _sim;

    public MilitaryAdvisorPanel(GameSimulation sim)
    {
        _sim = sim;
        Name = "MilitaryAdvisorPanel";
        
        // Ensure it acts as an overlay
        SetAnchorsPreset(LayoutPreset.FullRect);

        // Semi-transparent background
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.9f, 0.88f, 0.8f, 0.95f), // Civ 3 parchment style
            BorderWidthTop = 4,
            BorderWidthBottom = 4,
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderColor = new Color(0.7f, 0.6f, 0.4f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        AddThemeStyleboxOverride("panel", styleBox);

        // Handle input to prevent clicks from passing through
        MouseFilter = MouseFilterEnum.Stop;

        // --- MAIN CANVAS AREA ---
        var canvasBorder = new PanelContainer();
        canvasBorder.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        canvasBorder.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var canvasStyle = new StyleBoxEmpty();
        canvasBorder.AddThemeStyleboxOverride("panel", canvasStyle);
        
        // Outer Margin
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.AddThemeConstantOverride("margin_left", 30);
        margin.AddThemeConstantOverride("margin_right", 30);
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        margin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 20);

        // --- HEADER ---
        var headerHBox = new HBoxContainer();
        
        var titleLabel = new Label
        {
            Text = "M I L I T A R Y   A D V I S O R",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 28);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        
        // Close Button
        var closeBtn = new Button { Text = "✖", Flat = true, CustomMinimumSize = new Vector2(40, 40) };
        closeBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
        closeBtn.AddThemeFontSizeOverride("font_size", 24);
        closeBtn.Pressed += () => QueueFree();

        headerHBox.AddChild(titleLabel);
        headerHBox.AddChild(closeBtn);
        mainVBox.AddChild(headerHBox);

        // --- TOP STATS ROW ---
        var topStatsHBox = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        topStatsHBox.AddThemeConstantOverride("separation", 30);

        int playerUnitCount = _sim.Units.Count(u => u.Faction == Faction.Player);
        int aiUnitCount = _sim.Units.Count(u => u.Faction == Faction.AiRival);

        // Army size stats
        var armySizeVBox = new VBoxContainer();
        armySizeVBox.AddThemeConstantOverride("separation", 0);
        
        var totalUnitsBox = CreateStatBox("Total Units", playerUnitCount.ToString(), new Color(0.9f, 0.9f, 0.6f));
        var allowedUnitsBox = CreateStatBox("Allowed Units", "0", new Color(0.6f, 0.9f, 0.6f));
        var supportCostBox = CreateStatBox("Army Support Cost", $"{playerUnitCount} gold/turn", new Color(0.9f, 0.6f, 0.6f));
        
        armySizeVBox.AddChild(totalUnitsBox);
        armySizeVBox.AddChild(allowedUnitsBox);
        armySizeVBox.AddChild(supportCostBox);

        // Advisor Message
        string message = "We are evenly matched.";
        if (playerUnitCount > aiUnitCount * 1.5f) message = "Compared to these guys, we have a strong military!";
        else if (playerUnitCount > aiUnitCount) message = "We have a slightly larger military.";
        else if (playerUnitCount < aiUnitCount * 0.5f) message = "We are significantly outnumbered!";
        else if (playerUnitCount < aiUnitCount) message = "They have a larger military than us.";

        var advisorBox = new HBoxContainer();
        advisorBox.AddThemeConstantOverride("separation", 10);
        var advisorPortrait = new Label { Text = "🧑‍✈️" };
        advisorPortrait.AddThemeFontSizeOverride("font_size", 48);
        
        var messagePanel = new PanelContainer();
        var msgStyle = new StyleBoxFlat { BgColor = Colors.White, BorderColor = Colors.DarkGreen, BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2 };
        messagePanel.AddThemeStyleboxOverride("panel", msgStyle);
        var msgMargin = new MarginContainer { CustomMinimumSize = new Vector2(300, 100) };
        msgMargin.AddThemeConstantOverride("margin_left", 10);
        msgMargin.AddThemeConstantOverride("margin_right", 10);
        msgMargin.AddThemeConstantOverride("margin_top", 10);
        var msgLabel = new Label { Text = message, AutowrapMode = TextServer.AutowrapMode.Word, HorizontalAlignment = HorizontalAlignment.Left };
        msgLabel.AddThemeColorOverride("font_color", Colors.Black);
        msgMargin.AddChild(msgLabel);
        messagePanel.AddChild(msgMargin);

        advisorBox.AddChild(advisorPortrait);
        advisorBox.AddChild(messagePanel);

        topStatsHBox.AddChild(armySizeVBox);
        topStatsHBox.AddChild(advisorBox);
        
        mainVBox.AddChild(topStatsHBox);

        // --- DIVIDER ---
        var divider = new ColorRect { CustomMinimumSize = new Vector2(0, 2), Color = new Color(0.5f, 0.4f, 0.3f) };
        mainVBox.AddChild(divider);

        // --- BOTTOM COLUMNS ---
        var columnsHBox = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        columnsHBox.AddThemeConstantOverride("separation", 20);
        
        // Left Column (Player)
        var leftColumn = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var leftTitle = new Label { Text = $"The Army of {_sim.PlayerCiv.Name}", HorizontalAlignment = HorizontalAlignment.Center };
        leftTitle.AddThemeColorOverride("font_color", Colors.Black);
        leftTitle.AddThemeFontSizeOverride("font_size", 20);
        var leftTitleBg = new ColorRect { Color = new Color(0.85f, 0.8f, 0.7f), CustomMinimumSize = new Vector2(0, 30) };
        leftTitleBg.AddChild(new CenterContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }.AddChildWithReturn(leftTitle));
        leftColumn.AddChild(leftTitleBg);

        var leftScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var leftList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        leftList.AddThemeConstantOverride("separation", 15);
        
        // Group player units by type
        var playerUnits = _sim.Units.Where(u => u.Faction == Faction.Player).GroupBy(u => u.Type).OrderBy(g => g.Key.ToString());
        foreach (var group in playerUnits)
        {
            leftList.AddChild(CreateUnitRow(group.Key, group.Count()));
        }
        leftScroll.AddChild(leftList);
        leftColumn.AddChild(leftScroll);

        // Middle Divider line
        var colDivider = new ColorRect { CustomMinimumSize = new Vector2(2, 0), Color = new Color(0.7f, 0.6f, 0.5f), SizeFlagsVertical = Control.SizeFlags.ExpandFill };

        // Right Column (AI Rival)
        var rightColumn = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var rightTitle = new Label { Text = $"The Army of {_sim.AiCiv.Name}", HorizontalAlignment = HorizontalAlignment.Center };
        rightTitle.AddThemeColorOverride("font_color", Colors.Black);
        rightTitle.AddThemeFontSizeOverride("font_size", 20);
        var rightTitleBg = new ColorRect { Color = new Color(0.85f, 0.8f, 0.7f), CustomMinimumSize = new Vector2(0, 30) };
        rightTitleBg.AddChild(new CenterContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }.AddChildWithReturn(rightTitle));
        rightColumn.AddChild(rightTitleBg);

        var rightStatus = new Label { Text = _sim.IsAtWarWithAi ? "At WAR" : "At Peace", HorizontalAlignment = HorizontalAlignment.Center };
        rightStatus.AddThemeColorOverride("font_color", _sim.IsAtWarWithAi ? Colors.Red : Colors.DarkGreen);
        rightStatus.AddThemeFontSizeOverride("font_size", 18);
        rightColumn.AddChild(rightStatus);

        var espionageMsg = new Label { Text = "(Espionage Required to view enemy troops)", HorizontalAlignment = HorizontalAlignment.Center };
        espionageMsg.AddThemeColorOverride("font_color", Colors.Gray);
        rightColumn.AddChild(espionageMsg);

        columnsHBox.AddChild(leftColumn);
        columnsHBox.AddChild(colDivider);
        columnsHBox.AddChild(rightColumn);

        mainVBox.AddChild(columnsHBox);
        margin.AddChild(mainVBox);
        canvasBorder.AddChild(margin);
        AddChild(canvasBorder);
    }

    private Control CreateUnitRow(UnitType type, int count)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        
        var nameLabel = new Label { Text = $"({count}) {type.ToString()}", CustomMinimumSize = new Vector2(150, 0) };
        nameLabel.AddThemeColorOverride("font_color", Colors.Black);
        row.AddChild(nameLabel);

        var spritesHBox = new HBoxContainer();
        spritesHBox.AddThemeConstantOverride("separation", 2);
        
        // Load Texture based on unit type
        string baseName = type switch
        {
            UnitType.Explorer => "explorer",
            UnitType.Settler => "settler",
            UnitType.Warrior => "warrior",
            UnitType.Archer => "archer",
            UnitType.Barbarian => "warrior",
            UnitType.Worker => "worker",
            _ => type.ToString().ToLower()
        };
        string texturePath = $"res://assets/{baseName}_orig.webp";
        if (!FileAccess.FileExists(texturePath))
        {
            texturePath = $"res://assets/{type.ToString().ToLower()}.png";
        }
        Texture2D? tex = null;
        if (FileAccess.FileExists(texturePath))
        {
            try
            {
                var img = Image.LoadFromFile(texturePath);
                if (img != null && !img.IsEmpty())
                {
                    if (texturePath.Contains("_orig.webp"))
                    {
                        img = MakeBackgroundTransparentBFS(img, Colors.White);
                    }
                    tex = ImageTexture.CreateFromImage(img);
                }
            }
            catch (Exception ex)
            {
                GD.Print($"[MilitaryAdvisorPanel] Warning: Could not load texture from file {texturePath}: {ex.Message}");
            }
        }

        // Draw up to 20 icons, if more, add a + indicator or just clamp
        int drawCount = Mathf.Min(count, 30);
        for (int i = 0; i < drawCount; i++)
        {
            if (tex != null)
            {
                var texRect = new TextureRect 
                { 
                    Texture = tex, 
                    ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    CustomMinimumSize = new Vector2(24, 24)
                };
                spritesHBox.AddChild(texRect);
            }
            else
            {
                // Fallback emoji if no texture found
                var fallback = new Label { Text = "♟️" };
                spritesHBox.AddChild(fallback);
            }
        }
        
        if (count > 30)
        {
            var plusLabel = new Label { Text = "+" };
            plusLabel.AddThemeColorOverride("font_color", Colors.Black);
            spritesHBox.AddChild(plusLabel);
        }

        row.AddChild(spritesHBox);
        return row;
    }

    private PanelContainer CreateStatBox(string title, string value, Color bgColor)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(180, 0) };
        var style = new StyleBoxFlat { BgColor = bgColor, BorderColor = new Color(0.7f, 0.7f, 0.7f), BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1 };
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        var titleLabel = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
        titleLabel.AddThemeColorOverride("font_color", Colors.Black);
        titleLabel.AddThemeFontSizeOverride("font_size", 14);
        
        var valLabel = new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Center };
        valLabel.AddThemeColorOverride("font_color", Colors.DarkBlue);
        valLabel.AddThemeFontSizeOverride("font_size", 18);

        vbox.AddChild(titleLabel);
        vbox.AddChild(valLabel);
        panel.AddChild(vbox);
        return panel;
    }

    private static Image MakeBackgroundTransparentBFS(Image img, Color keyColor, float threshold = 0.08f)
    {
        img.Convert(Image.Format.Rgba8);
        int width = img.GetWidth();
        int height = img.GetHeight();
        
        bool[,] visited = new bool[width, height];
        Queue<Vector2I> queue = new Queue<Vector2I>();
        
        // Add all edge pixels as starting points
        for (int x = 0; x < width; x++)
        {
            queue.Enqueue(new Vector2I(x, 0));
            queue.Enqueue(new Vector2I(x, height - 1));
            visited[x, 0] = true;
            visited[x, height - 1] = true;
        }
        for (int y = 1; y < height - 1; y++)
        {
            queue.Enqueue(new Vector2I(0, y));
            queue.Enqueue(new Vector2I(width - 1, y));
            visited[0, y] = true;
            visited[width - 1, y] = true;
        }
        
        while (queue.Count > 0)
        {
            Vector2I curr = queue.Dequeue();
            Color pixel = img.GetPixel(curr.X, curr.Y);
            
            float diffR = Math.Abs(pixel.R - keyColor.R);
            float diffG = Math.Abs(pixel.G - keyColor.G);
            float diffB = Math.Abs(pixel.B - keyColor.B);
            
            if (diffR <= threshold && diffG <= threshold && diffB <= threshold)
            {
                // Make it transparent
                img.SetPixel(curr.X, curr.Y, new Color(pixel.R, pixel.G, pixel.B, 0.0f));
                
                // Add neighbors
                Vector2I[] neighbors = new Vector2I[]
                {
                    new Vector2I(curr.X + 1, curr.Y),
                    new Vector2I(curr.X - 1, curr.Y),
                    new Vector2I(curr.X, curr.Y + 1),
                    new Vector2I(curr.X, curr.Y - 1)
                };
                
                foreach (var n in neighbors)
                {
                    if (n.X >= 0 && n.X < width && n.Y >= 0 && n.Y < height)
                    {
                        if (!visited[n.X, n.Y])
                        {
                            visited[n.X, n.Y] = true;
                            queue.Enqueue(n);
                        }
                    }
                }
            }
        }
        
        return img;
    }
}

// Extension to cleanly chain AddChild returning the child
public static class NodeExtensions
{
    public static T AddChildWithReturn<T>(this Node parent, T child) where T : Node
    {
        parent.AddChild(child);
        return child;
    }
}
