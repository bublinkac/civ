using Godot;
using CivGame.Core;
using System;
using System.Linq;

namespace CivGame.UI.CityComponents;

public partial class CityCitizensComponent : PanelContainer
{
    public CityCitizensComponent(City city, GameSimulation sim, Action onCitizensChanged)
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
        // We no longer deduct fake scientists here.
        // The real Specialists are stored in city.Specialists.

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

        // Draw Real Specialists
        for (int i = 0; i < city.Specialists.Count; i++)
        {
            int specIndex = i; // capture for closure
            var spec = city.Specialists[i];
            string icon = spec == SpecialistType.Entertainer ? "🤹" : (spec == SpecialistType.Taxman ? "💰" : "🧑‍🔬");
            string tt = spec == SpecialistType.Entertainer ? "Entertainer (+1 Happy)" : (spec == SpecialistType.Taxman ? "Tax Collector (+2 Gold)" : "Scientist (+3 Science)");
            Color col = spec == SpecialistType.Entertainer ? new Color(0.8f, 0.4f, 0.8f) : (spec == SpecialistType.Taxman ? new Color(0.8f, 0.7f, 0.2f) : new Color(0.1f, 0.75f, 0.75f));
            
            hbox.AddChild(createCitizenHead(icon, "Specialist: " + tt + " (Click to change)", col, () => {
                // Cycle: Entertainer -> Taxman -> Scientist -> Entertainer
                if (city.Specialists[specIndex] == SpecialistType.Entertainer) city.Specialists[specIndex] = SpecialistType.Taxman;
                else if (city.Specialists[specIndex] == SpecialistType.Taxman) city.Specialists[specIndex] = SpecialistType.Scientist;
                else city.Specialists[specIndex] = SpecialistType.Entertainer;
                
                onCitizensChanged?.Invoke();
            }));
        }
        // Draft Button
        if (city.Faction == Faction.Player && city.CanDraft(sim))
        {
            var sep = new VSeparator();
            hbox.AddChild(sep);

            var draftBtn = new Button { Text = "Draft", TooltipText = "Draft a citizen into military service (Consumes 1 Pop, adds Unhappiness)", MouseDefaultCursorShape = CursorShape.PointingHand };
            draftBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.3f));
            draftBtn.Pressed += () => {
                city.Draft(sim);
                onCitizensChanged?.Invoke();
            };
            hbox.AddChild(draftBtn);
        }
    }

    private Control createCitizenHead(string text, string tooltip, Color color, Action onClick = null)
    {
        var box = new VBoxContainer();
        box.Alignment = BoxContainer.AlignmentMode.Center;
        box.TooltipText = tooltip;

        if (onClick != null)
        {
            var btn = new Button { Text = text, Flat = true, MouseDefaultCursorShape = CursorShape.PointingHand };
            btn.AddThemeFontSizeOverride("font_size", 28);
            btn.Pressed += onClick;
            box.AddChild(btn);
        }
        else
        {
            var label = new Label { Text = text };
            label.AddThemeFontSizeOverride("font_size", 28);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(label);
        }

        // Tiny status bar under head
        var bar = new ColorRect { CustomMinimumSize = new Vector2(24, 4), Color = color };
        bar.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        box.AddChild(bar);

        return box;
    }
}
