using Godot;
using CivGame.Core;

namespace CivGame.UI.CityComponents;

public partial class CityHeaderComponent : PanelContainer
{
    public CityHeaderComponent(City city, GameSimulation sim, System.Action onCyclePrev, System.Action onCycleNext)
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.92f, 0.88f, 0.78f, 0.0f), // transparent, let parent handle bg
            BorderColor = new Color(0.45f, 0.38f, 0.28f, 0.3f),
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
        AddThemeStyleboxOverride("panel", style);

        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var mainHBox = new HBoxContainer();
        mainHBox.Alignment = BoxContainer.AlignmentMode.Center;
        mainHBox.AddThemeConstantOverride("separation", 25);
        AddChild(mainHBox);

        // 1. Prev Button ◀
        var prevBtn = new Button { Text = "◀", Flat = true, MouseDefaultCursorShape = CursorShape.PointingHand };
        prevBtn.AddThemeFontSizeOverride("font_size", 28);
        prevBtn.AddThemeColorOverride("font_color", new Color(0.4f, 0.3f, 0.15f));
        prevBtn.Pressed += () => onCyclePrev?.Invoke();
        mainHBox.AddChild(prevBtn);

        // 2. Info VBox
        var infoVBox = new VBoxContainer();
        infoVBox.Alignment = BoxContainer.AlignmentMode.Center;
        infoVBox.AddThemeConstantOverride("separation", 1);
        mainHBox.AddChild(infoVBox);

        // City Name
        var nameLabel = new Label { Text = city.Name.ToUpper() };
        nameLabel.AddThemeFontSizeOverride("font_size", 26);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.12f, 0.1f, 0.05f));
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.AddThemeConstantOverride("outline_size", 0);
        infoVBox.AddChild(nameLabel);

        // Sub Stats Grid (Founded, Gold, Gov, Pop, Year)
        var grid = new GridContainer { Columns = 3 };
        grid.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        grid.AddThemeConstantOverride("h_separation", 15);
        grid.AddThemeConstantOverride("v_separation", 1);
        infoVBox.AddChild(grid);

        // Row 1: Founded, Gold, Gov
        var foundedLabel = new Label { Text = $"Founded: {city.FoundedYear * 50} BC" };
        styleHeaderLabel(foundedLabel);
        grid.AddChild(foundedLabel);

        var goldLabel = new Label { Text = $"{sim.PlayerTreasury} GOLD" };
        styleHeaderLabel(goldLabel);
        goldLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.45f, 0.1f));
        grid.AddChild(goldLabel);

        var govLabel = new Label { Text = "REPUBLIC" };
        styleHeaderLabel(govLabel);
        govLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.35f, 0.55f));
        grid.AddChild(govLabel);

        // Row 2: Population count, Current year, and empty spacer
        long realPop = 10000;
        if (city.Population > 1)
        {
            realPop = (long)(city.Population * (city.Population + 1) / 2) * 25000;
        }
        var popLabel = new Label { Text = $"POP: {realPop:N0}" };
        styleHeaderLabel(popLabel);
        grid.AddChild(popLabel);

        var yearLabel = new Label { Text = $"{sim.TurnNumber * 40} AD" }; // Turn base year
        styleHeaderLabel(yearLabel);
        grid.AddChild(yearLabel);

        // Spacer
        grid.AddChild(new Control());

        // 3. Next Button ▶
        var nextBtn = new Button { Text = "▶", Flat = true, MouseDefaultCursorShape = CursorShape.PointingHand };
        nextBtn.AddThemeFontSizeOverride("font_size", 28);
        nextBtn.AddThemeColorOverride("font_color", new Color(0.4f, 0.3f, 0.15f));
        nextBtn.Pressed += () => onCycleNext?.Invoke();
        mainHBox.AddChild(nextBtn);
    }

    private void styleHeaderLabel(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", new Color(0.35f, 0.32f, 0.25f));
        label.AddThemeConstantOverride("outline_size", 0);
        label.HorizontalAlignment = HorizontalAlignment.Center;
    }
}
