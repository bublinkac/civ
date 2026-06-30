using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

/// <summary>
/// Civ3-style Histograph panel: line graph showing Player vs AI stats over time.
/// Categories: Score, Population, Territory, Culture, Military.
/// </summary>
public partial class HistographPanel : PanelContainer
{
    private GameSimulation _sim;
    private string _currentCategory = "Score";
    private HBoxContainer _categoryButtons;
    private Control _graphArea;
    private Label _titleLabel;

    private static readonly Color PlayerColor = new(0.2f, 0.6f, 1.0f);
    private static readonly Color AiColor = new(0.9f, 0.3f, 0.2f);
    private static readonly Color GridColor = new(0.4f, 0.4f, 0.4f, 0.3f);
    private static readonly Color BgDark = new(0.08f, 0.08f, 0.12f, 0.95f);

    public HistographPanel(GameSimulation sim)
    {
        _sim = sim;

        SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.1f, 0.97f),
            BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3,
            BorderColor = new Color(0.7f, 0.6f, 0.3f, 0.9f)
        };
        AddThemeStyleboxOverride("panel", bgStyle);

        var mainVBox = new VBoxContainer();

        // --- HEADER ---
        _titleLabel = new Label { Text = "H I S T O G R A P H  —  Score" };
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.4f));
        _titleLabel.AddThemeFontSizeOverride("font_size", 28);
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        var headerMargin = new MarginContainer();
        headerMargin.AddThemeConstantOverride("margin_top", 10);
        headerMargin.AddThemeConstantOverride("margin_bottom", 5);
        headerMargin.AddChild(_titleLabel);
        mainVBox.AddChild(headerMargin);

        // --- CATEGORY BUTTONS ---
        _categoryButtons = new HBoxContainer();
        _categoryButtons.Alignment = BoxContainer.AlignmentMode.Center;
        _categoryButtons.AddThemeConstantOverride("separation", 8);

        foreach (var cat in HistographData.Categories)
        {
            var btn = new Button { Text = cat };
            btn.CustomMinimumSize = new Vector2(110, 32);
            btn.AddThemeColorOverride("font_color", Colors.White);
            btn.AddThemeFontSizeOverride("font_size", 14);

            bool isActive = cat == _currentCategory;
            btn.AddThemeStyleboxOverride("normal", CreateCategoryBtnStyle(isActive));

            var catCapture = cat;
            btn.Pressed += () => SwitchCategory(catCapture);
            _categoryButtons.AddChild(btn);
        }

        var catMargin = new MarginContainer();
        catMargin.AddThemeConstantOverride("margin_left", 20);
        catMargin.AddThemeConstantOverride("margin_right", 20);
        catMargin.AddThemeConstantOverride("margin_bottom", 5);
        catMargin.AddChild(_categoryButtons);
        mainVBox.AddChild(catMargin);

        // --- LEGEND ---
        var legendHBox = new HBoxContainer();
        legendHBox.Alignment = BoxContainer.AlignmentMode.Center;
        legendHBox.AddThemeConstantOverride("separation", 30);

        legendHBox.AddChild(CreateLegendItem(PlayerColor, $"Player ({_sim.PlayerCiv.Name})"));
        legendHBox.AddChild(CreateLegendItem(AiColor, $"AI ({_sim.AiCiv.Name})"));

        var legendMargin = new MarginContainer();
        legendMargin.AddThemeConstantOverride("margin_bottom", 5);
        legendMargin.AddChild(legendHBox);
        mainVBox.AddChild(legendMargin);

        // --- GRAPH AREA ---
        _graphArea = new Control();
        _graphArea.SizeFlagsVertical = SizeFlags.ExpandFill;
        _graphArea.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _graphArea.CustomMinimumSize = new Vector2(600, 300);

        var graphMargin = new MarginContainer();
        graphMargin.AddThemeConstantOverride("margin_left", 60);
        graphMargin.AddThemeConstantOverride("margin_right", 30);
        graphMargin.AddThemeConstantOverride("margin_top", 5);
        graphMargin.AddThemeConstantOverride("margin_bottom", 10);
        graphMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        graphMargin.AddChild(_graphArea);
        mainVBox.AddChild(graphMargin);

        // --- CLOSE BUTTON ---
        var closeMargin = new MarginContainer();
        closeMargin.AddThemeConstantOverride("margin_bottom", 10);

        var closeBtn = new Button { Text = "Close" };
        closeBtn.CustomMinimumSize = new Vector2(120, 36);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        closeBtn.AddThemeColorOverride("font_color", Colors.White);
        closeBtn.AddThemeFontSizeOverride("font_size", 16);
        var closeBtnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.4f, 0.25f, 0.15f, 1.0f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.6f, 0.5f, 0.3f, 1.0f),
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4
        };
        closeBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
        closeBtn.Pressed += () => QueueFree();
        closeMargin.AddChild(closeBtn);
        mainVBox.AddChild(closeMargin);

        AddChild(mainVBox);
    }

    public override void _Draw()
    {
        base._Draw();
        DrawGraph();
    }

    private void DrawGraph()
    {
        if (_graphArea == null || !IsInstanceValid(_graphArea)) return;

        var playerData = _sim.Histograph.PlayerHistory;
        var aiData = _sim.Histograph.AiHistory;

        if (playerData.Count < 2) return;

        var graphRect = _graphArea.GetGlobalRect();
        var localOffset = _graphArea.GetGlobalTransform().Origin - GetGlobalTransform().Origin;

        float gx = localOffset.X;
        float gy = localOffset.Y;
        float gw = graphRect.Size.X;
        float gh = graphRect.Size.Y;

        if (gw < 10 || gh < 10) return;

        // Graph background
        DrawRect(new Rect2(gx, gy, gw, gh), new Color(0.05f, 0.05f, 0.08f, 0.8f));

        // Find max value for Y axis scaling
        int maxVal = 1;
        for (int i = 0; i < playerData.Count; i++)
        {
            int pv = HistographData.GetValue(playerData[i], _currentCategory);
            int av = i < aiData.Count ? HistographData.GetValue(aiData[i], _currentCategory) : 0;
            maxVal = Math.Max(maxVal, Math.Max(pv, av));
        }
        maxVal = (int)(maxVal * 1.1f) + 1; // 10% headroom

        int totalTurns = playerData.Count;

        // Grid lines (horizontal)
        int gridLines = 5;
        for (int i = 0; i <= gridLines; i++)
        {
            float yPos = gy + gh - (gh * i / gridLines);
            DrawLine(new Vector2(gx, yPos), new Vector2(gx + gw, yPos), GridColor, 1.0f);

            // Y-axis labels
            int labelVal = maxVal * i / gridLines;
            string labelText = labelVal >= 1000 ? $"{labelVal / 1000}k" : labelVal.ToString();
            DrawString(ThemeDB.FallbackFont, new Vector2(gx - 50, yPos + 5), labelText, HorizontalAlignment.Right, 45, 12, new Color(0.6f, 0.6f, 0.6f));
        }

        // Grid lines (vertical) + X-axis labels
        int xGridCount = Math.Min(totalTurns - 1, 10);
        if (xGridCount > 0)
        {
            for (int i = 0; i <= xGridCount; i++)
            {
                int turnIdx = totalTurns * i / xGridCount;
                if (turnIdx >= totalTurns) turnIdx = totalTurns - 1;
                float xPos = gx + (gw * turnIdx / (totalTurns - 1));
                DrawLine(new Vector2(xPos, gy), new Vector2(xPos, gy + gh), GridColor, 1.0f);

                int turnNum = playerData[turnIdx].Turn;
                DrawString(ThemeDB.FallbackFont, new Vector2(xPos - 10, gy + gh + 16), $"T{turnNum}", HorizontalAlignment.Left, 40, 11, new Color(0.6f, 0.6f, 0.6f));
            }
        }

        // Draw lines: Player
        DrawDataLine(playerData, gx, gy, gw, gh, totalTurns, maxVal, PlayerColor, 2.5f);

        // Draw lines: AI
        if (aiData.Count >= 2)
            DrawDataLine(aiData, gx, gy, gw, gh, totalTurns, maxVal, AiColor, 2.5f);

        // Border
        DrawRect(new Rect2(gx, gy, gw, gh), new Color(0.5f, 0.5f, 0.5f, 0.6f), false, 1.5f);
    }

    private void DrawDataLine(List<HistographEntry> data, float gx, float gy, float gw, float gh, int totalTurns, int maxVal, Color color, float width)
    {
        int count = Math.Min(data.Count, totalTurns);
        if (count < 2) return;

        for (int i = 1; i < count; i++)
        {
            float x1 = gx + (gw * (i - 1) / (totalTurns - 1));
            float x2 = gx + (gw * i / (totalTurns - 1));

            int v1 = HistographData.GetValue(data[i - 1], _currentCategory);
            int v2 = HistographData.GetValue(data[i], _currentCategory);

            float y1 = gy + gh - (gh * v1 / maxVal);
            float y2 = gy + gh - (gh * v2 / maxVal);

            DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), color, width);
        }

        // Draw endpoint dot
        if (count > 0)
        {
            int lastIdx = count - 1;
            float lastX = gx + (gw * lastIdx / (totalTurns - 1));
            int lastV = HistographData.GetValue(data[lastIdx], _currentCategory);
            float lastY = gy + gh - (gh * lastV / maxVal);
            DrawCircle(new Vector2(lastX, lastY), 4.0f, color);

            // Value label at endpoint
            string valText = lastV >= 1000 ? $"{lastV / 1000}k" : lastV.ToString();
            DrawString(ThemeDB.FallbackFont, new Vector2(lastX + 8, lastY + 4), valText, HorizontalAlignment.Left, 50, 12, color);
        }
    }

    private void SwitchCategory(string category)
    {
        _currentCategory = category;
        _titleLabel.Text = $"H I S T O G R A P H  —  {category}";

        // Update button styles
        int idx = 0;
        foreach (var child in _categoryButtons.GetChildren())
        {
            if (child is Button btn)
            {
                bool isActive = HistographData.Categories[idx] == category;
                btn.AddThemeStyleboxOverride("normal", CreateCategoryBtnStyle(isActive));
                idx++;
            }
        }

        QueueRedraw();
    }

    private static StyleBoxFlat CreateCategoryBtnStyle(bool active)
    {
        return new StyleBoxFlat
        {
            BgColor = active ? new Color(0.3f, 0.4f, 0.6f, 1.0f) : new Color(0.15f, 0.15f, 0.2f, 0.8f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = active ? new Color(0.7f, 0.8f, 1.0f, 0.9f) : new Color(0.3f, 0.3f, 0.4f, 0.6f),
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4
        };
    }

    private static HBoxContainer CreateLegendItem(Color color, string text)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);

        var colorRect = new ColorRect();
        colorRect.Color = color;
        colorRect.CustomMinimumSize = new Vector2(18, 4);
        hbox.AddChild(colorRect);

        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 13);
        hbox.AddChild(label);

        return hbox;
    }
}
