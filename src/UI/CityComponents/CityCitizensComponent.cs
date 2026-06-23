using Godot;
using CivGame.Core;
using System;
using System.Linq;

namespace CivGame.UI.CityComponents;

public partial class CityCitizensComponent : PanelContainer
{
    public CityCitizensComponent(City city, GameSimulation sim)
    {
        // Transparent style, just displays citizen rows
        var style = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) };
        AddThemeStyleboxOverride("panel", style);

        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var hbox = new HBoxContainer();
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddThemeConstantOverride("separation", 6);
        AddChild(hbox);

        // Update citizen mood dynamically based on simulation rules
        city.UpdateCitizenMood(sim, sim.DifficultyConfig);

        int happyCount = city.HappyCitizens;
        int contentCount = city.ContentCitizens;
        int unhappyCount = city.UnhappyCitizens;
        int scientists = 0;

        // Turn some into scientists if we have educational buildings and large population
        int totalPop = city.Population;
        if (city.Buildings.Any(b => b.Id == "library" || b.Id == "university") && totalPop > 4)
        {
            scientists = Math.Min(2, totalPop - 4);
            // Deduct specialists from working citizens, favoring unhappy/content deduction first
            for (int i = 0; i < scientists; i++)
            {
                if (unhappyCount > 0) unhappyCount--;
                else if (contentCount > 0) contentCount--;
                else if (happyCount > 0) happyCount--;
            }
        }

        // Draw Happy Citizens (👑😊)
        for (int i = 0; i < happyCount; i++)
        {
            hbox.AddChild(createCitizenHead("👑🧑", "Happy Citizen", new Color(0.95f, 0.8f, 0.1f)));
        }

        // Draw Content Citizens (🧑)
        for (int i = 0; i < contentCount; i++)
        {
            hbox.AddChild(createCitizenHead("🧑", "Content Citizen", new Color(0.15f, 0.55f, 0.85f)));
        }

        // Draw Unhappy Citizens (😡🧑)
        for (int i = 0; i < unhappyCount; i++)
        {
            hbox.AddChild(createCitizenHead("😡🧑", "Unhappy Citizen (Requires Luxuries or Temples)", new Color(0.85f, 0.15f, 0.15f)));
        }

        // Draw Scientists (🧑‍🔬)
        for (int i = 0; i < scientists; i++)
        {
            hbox.AddChild(createCitizenHead("🧑‍🔬", "Specialist: Scientist (+3 Science)", new Color(0.1f, 0.75f, 0.75f)));
        }
    }

    private Control createCitizenHead(string text, string tooltip, Color color)
    {
        var box = new VBoxContainer();
        box.Alignment = BoxContainer.AlignmentMode.Center;
        box.TooltipText = tooltip;

        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 28);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(label);

        // Tiny status bar under head
        var bar = new ColorRect { CustomMinimumSize = new Vector2(24, 4), Color = color };
        bar.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        box.AddChild(bar);

        return box;
    }
}
