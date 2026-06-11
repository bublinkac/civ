using Godot;
using CivGame.Core;
using System;
using System.Linq;

namespace CivGame.UI.CityComponents;

public partial class CityCultureHeaderComponent : PanelContainer
{
    public CityCultureHeaderComponent(City city, GameSimulation sim, System.Action onClose)
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.92f, 0.88f, 0.78f, 0.0f), // transparent, let parent handle bg
            BorderColor = new Color(0.45f, 0.38f, 0.28f, 0.3f),
            BorderWidthLeft = 2
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(280, 0);

        var mainHBox = new HBoxContainer();
        mainHBox.Alignment = BoxContainer.AlignmentMode.End;
        AddChild(mainHBox);

        var infoVBox = new VBoxContainer();
        infoVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        infoVBox.Alignment = BoxContainer.AlignmentMode.Center;
        infoVBox.AddThemeConstantOverride("separation", 2);
        mainHBox.AddChild(infoVBox);

        // Calculate culture per turn
        int culturePerTurn = 0;
        foreach (var b in city.Buildings)
        {
            if (b.Id == "temple") culturePerTurn += 2;
            else if (b.Id == "library") culturePerTurn += 3;
            else if (b.Id == "monument") culturePerTurn += 1;
            else if (b.Id == "cathedral") culturePerTurn += 4;
            else if (b.Id == "university") culturePerTurn += 4;
            else if (b.Id == "research_lab") culturePerTurn += 2;
            else if (WonderRegistry.Get(b.Id) is Wonder wonder)
            {
                culturePerTurn += wonder.IsNationalWonder ? 1 : 2;
            }
        }

        // Virtual accumulated culture
        int turnsActive = Math.Max(0, sim.TurnNumber - city.FoundedYear);
        int totalCulture = turnsActive * culturePerTurn;
        int nextExpansionThreshold = 100;
        if (totalCulture >= 100) nextExpansionThreshold = 1000;
        if (totalCulture >= 1000) nextExpansionThreshold = 10000;

        int turnsToExpand = culturePerTurn > 0 
            ? (int)Math.Ceiling((double)(nextExpansionThreshold - totalCulture) / culturePerTurn) 
            : 9999;

        // Row 1: Culture Title
        var cultureTitle = new Label { Text = $"CULTURE  {culturePerTurn} per turn" };
        cultureTitle.AddThemeFontSizeOverride("font_size", 11);
        cultureTitle.AddThemeColorOverride("font_color", new Color(0.2f, 0.1f, 0.35f)); // Classic purple for culture
        cultureTitle.AddThemeConstantOverride("outline_size", 0);
        cultureTitle.HorizontalAlignment = HorizontalAlignment.Right;
        infoVBox.AddChild(cultureTitle);

        // Row 2: Progress
        var expansionLabel = new Label();
        if (culturePerTurn > 0)
            expansionLabel.Text = $"Expand in {turnsToExpand} turns";
        else
            expansionLabel.Text = "No expansion possible";
        expansionLabel.AddThemeFontSizeOverride("font_size", 10);
        expansionLabel.AddThemeColorOverride("font_color", new Color(0.35f, 0.32f, 0.25f));
        expansionLabel.AddThemeConstantOverride("outline_size", 0);
        expansionLabel.HorizontalAlignment = HorizontalAlignment.Right;
        infoVBox.AddChild(expansionLabel);

        // Row 3: Totals
        var totalLabel = new Label { Text = $"Total: {totalCulture}/{nextExpansionThreshold}" };
        totalLabel.AddThemeFontSizeOverride("font_size", 10);
        totalLabel.AddThemeColorOverride("font_color", new Color(0.35f, 0.32f, 0.25f));
        totalLabel.AddThemeConstantOverride("outline_size", 0);
        totalLabel.HorizontalAlignment = HorizontalAlignment.Right;
        infoVBox.AddChild(totalLabel);

        // Spacing before close button
        var spacer = new Control { CustomMinimumSize = new Vector2(10, 0) };
        mainHBox.AddChild(spacer);

        // Large "X" Close Button (Golden/Parchment styling)
        var closeBtn = new Button { Text = "✕", Flat = true, MouseDefaultCursorShape = CursorShape.PointingHand };
        closeBtn.AddThemeFontSizeOverride("font_size", 28);
        closeBtn.AddThemeColorOverride("font_color", new Color(0.6f, 0.15f, 0.15f)); // Reddish brown
        closeBtn.Pressed += () => onClose?.Invoke();
        mainHBox.AddChild(closeBtn);
    }
}
