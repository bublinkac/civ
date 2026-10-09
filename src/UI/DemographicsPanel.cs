using Godot;
using CivGame.Core;

namespace CivGame.UI;

/// <summary>
/// Civ3-style Demographics screen (F11): compares the player's civilization
/// against the rival across population, territory, economy, tech, military and culture.
/// </summary>
public partial class DemographicsPanel : PanelContainer
{
    private GameSimulation _sim;

    private static readonly Color DarkText = new(0.15f, 0.12f, 0.08f);
    private static readonly Color HeaderGold = new(0.45f, 0.32f, 0.10f);
    private static readonly Color RankGreen = new(0.15f, 0.45f, 0.15f);
    private static readonly Color RankRed = new(0.6f, 0.15f, 0.15f);

    public DemographicsPanel(GameSimulation sim, bool embedded = false)
    {
        _sim = sim;
        Name = "DemographicsPanel";

        if (!embedded)
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Stop;
            var styleBox = new StyleBoxFlat
            {
                BgColor = new Color(0.91f, 0.87f, 0.78f, 1.0f),
                BorderWidthTop = 5, BorderWidthBottom = 5, BorderWidthLeft = 5, BorderWidthRight = 5,
                BorderColor = new Color(0.6f, 0.5f, 0.3f),
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
            };
            AddThemeStyleboxOverride("panel", styleBox);
        }
        else
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) });
        }

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 30);
        margin.AddThemeConstantOverride("margin_right", 30);
        margin.AddThemeConstantOverride("margin_top", 15);
        margin.AddThemeConstantOverride("margin_bottom", 15);
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 12);
        mainVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.SizeFlagsVertical = SizeFlags.ExpandFill;

        if (!embedded)
        {
            var headerHBox = new HBoxContainer();
            var titleLabel = new Label
            {
                Text = "D E M O G R A P H I C S",
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 28);
            titleLabel.AddThemeColorOverride("font_color", DarkText);

            var closeBtn = new Button { Text = "✖", Flat = true, CustomMinimumSize = new Vector2(40, 40) };
            closeBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
            closeBtn.AddThemeFontSizeOverride("font_size", 24);
            closeBtn.Pressed += () => QueueFree();

            headerHBox.AddChild(titleLabel);
            headerHBox.AddChild(closeBtn);
            mainVBox.AddChild(headerHBox);
        }

        var grid = new GridContainer { Columns = 5 };
        grid.AddThemeConstantOverride("h_separation", 40);
        grid.AddThemeConstantOverride("v_separation", 6);
        grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        foreach (var col in new[] { "Category", "Value", "Rank", "Best", "Worst" })
        {
            var header = new Label { Text = col };
            header.AddThemeColorOverride("font_color", HeaderGold);
            header.AddThemeFontSizeOverride("font_size", 15);
            grid.AddChild(header);
        }

        foreach (var row in DemographicsData.Compute(_sim))
        {
            AddCell(grid, row.Category, DarkText, bold: true);
            AddCell(grid, row.PlayerValue, DarkText);
            AddCell(grid, $"{row.Rank} of {row.TotalCivs}", row.Rank == 1 ? RankGreen : RankRed);
            AddCell(grid, row.BestCiv, DarkText);
            AddCell(grid, row.WorstCiv, DarkText);
        }

        mainVBox.AddChild(grid);

        if (!embedded)
        {
            var footerMargin = new MarginContainer();
            footerMargin.AddThemeConstantOverride("margin_top", 10);
            var footerCloseBtn = new Button { Text = "Close", CustomMinimumSize = new Vector2(150, 40) };
            footerCloseBtn.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            footerCloseBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat
            {
                BgColor = new Color(0.7f, 0.2f, 0.2f, 1.0f),
                CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
                CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
            });
            footerCloseBtn.Pressed += () => QueueFree();
            footerMargin.AddChild(footerCloseBtn);
            mainVBox.AddChild(footerMargin);
        }

        margin.AddChild(mainVBox);
        AddChild(margin);
    }

    private static void AddCell(GridContainer grid, string text, Color color, bool bold = false)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", bold ? 15 : 14);
        grid.AddChild(label);
    }
}
