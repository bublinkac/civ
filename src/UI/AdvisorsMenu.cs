using Godot;
using CivGame.Core;
using System;

namespace CivGame.UI;

public partial class AdvisorsMenu : PanelContainer
{
    public event Action? OnOpenScienceAdvisor;
    public event Action? OnOpenDomesticAdvisor;
    
    public AdvisorsMenu()
    {
        // Setup Modal
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 110;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f),
            BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3,
            BorderColor = new Color(0.95f, 0.72f, 0.12f, 0.9f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(300, 450);
        AnchorLeft = 0.5f;
        AnchorTop = 0.5f;
        AnchorRight = 0.5f;
        AnchorBottom = 0.5f;
        OffsetLeft = -150;
        OffsetTop = -225;
        OffsetRight = 150;
        OffsetBottom = 225;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 15);
        margin.AddChild(vbox);

        var title = new Label { Text = "EMPIRE ADVISORS", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f));
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        // Science Advisor
        var scienceBtn = CreateAdvisorButton("🔬 SCIENCE ADVISOR", new Color(0.15f, 0.15f, 0.45f));
        scienceBtn.Pressed += () => {
            OnOpenScienceAdvisor?.Invoke();
            QueueFree();
        };
        vbox.AddChild(scienceBtn);

        // Placeholder Advisors
        var militaryBtn = CreateAdvisorButton("⚔️ MILITARY ADVISOR", new Color(0.45f, 0.15f, 0.15f));
        militaryBtn.Disabled = true; // Not implemented yet
        militaryBtn.TooltipText = "Consult with your Generals about military status (Coming Soon)";
        vbox.AddChild(militaryBtn);

        var domesticBtn = CreateAdvisorButton("🏛️ DOMESTIC ADVISOR", new Color(0.15f, 0.45f, 0.15f));
        domesticBtn.Pressed += () => {
            OnOpenDomesticAdvisor?.Invoke();
            QueueFree();
        };
        vbox.AddChild(domesticBtn);

        var foreignBtn = CreateAdvisorButton("🌐 FOREIGN ADVISOR", new Color(0.4f, 0.4f, 0.15f));
        foreignBtn.Disabled = true;
        vbox.AddChild(foreignBtn);

        vbox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

        var closeBtn = new Button { Text = "BACK TO MAP", CustomMinimumSize = new Vector2(0, 40) };
        closeBtn.Pressed += QueueFree;
        vbox.AddChild(closeBtn);
    }

    private Button CreateAdvisorButton(string text, Color color)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(0, 55) };
        
        var normal = new StyleBoxFlat { BgColor = color, CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5 };
        var hover = new StyleBoxFlat { BgColor = color.Lightened(0.2f), CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5, BorderWidthLeft = 2, BorderColor = Colors.White };
        var pressed = new StyleBoxFlat { BgColor = color.Darkened(0.2f), CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5 };
        var disabled = new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f), CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5 };

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", pressed);
        btn.AddThemeStyleboxOverride("disabled", disabled);
        
        return btn;
    }
}
