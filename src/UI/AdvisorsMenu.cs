using Godot;
using CivGame.Core;
using System;

namespace CivGame.UI;

/// <summary>
/// Civ3-style Advisors selection menu. Matches the original advisor screen layout:
/// F1 = Domestic, F2 = Trade, F3 = Military, F4 = Foreign, F5 = Cultural, F6 = Science
/// </summary>
public partial class AdvisorsMenu : PanelContainer
{
    public event Action? OnOpenScienceAdvisor;
    public event Action? OnOpenDomesticAdvisor;
    public event Action? OnOpenMilitaryAdvisor;
    public event Action? OnOpenForeignAdvisor;
    public event Action? OnOpenCulturalAdvisor;
    public event Action? OnOpenTradeAdvisor;
    
    public AdvisorsMenu()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 110;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.10f, 0.97f),
            BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3,
            BorderColor = new Color(0.95f, 0.72f, 0.12f, 0.9f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ShadowSize = 15, ShadowColor = new Color(0, 0, 0, 0.5f)
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(340, 560);
        AnchorLeft = 0.5f;
        AnchorTop = 0.5f;
        AnchorRight = 0.5f;
        AnchorBottom = 0.5f;
        OffsetLeft = -170;
        OffsetTop = -280;
        OffsetRight = 170;
        OffsetBottom = 280;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        var title = new Label { Text = "EMPIRE ADVISORS", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f));
        vbox.AddChild(title);

        var subtitle = new Label { Text = "Select an advisor to consult", HorizontalAlignment = HorizontalAlignment.Center };
        subtitle.AddThemeFontSizeOverride("font_size", 12);
        subtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        vbox.AddChild(subtitle);

        vbox.AddChild(new HSeparator());

        // F1 — Domestic Advisor
        var domesticBtn = CreateAdvisorButton("F1  DOMESTIC ADVISOR", "Economy, cities & happiness",
            new Color(0.15f, 0.45f, 0.15f));
        domesticBtn.Pressed += () => { OnOpenDomesticAdvisor?.Invoke(); QueueFree(); };
        vbox.AddChild(domesticBtn);

        // F2 — Trade Advisor
        var tradeBtn = CreateAdvisorButton("F2  TRADE ADVISOR", "Trade routes, luxuries & resources",
            new Color(0.50f, 0.35f, 0.10f));
        tradeBtn.Pressed += () => { OnOpenTradeAdvisor?.Invoke(); QueueFree(); };
        vbox.AddChild(tradeBtn);

        // F3 — Military Advisor
        var militaryBtn = CreateAdvisorButton("F3  MILITARY ADVISOR", "Army overview & threat assessment",
            new Color(0.45f, 0.15f, 0.15f));
        militaryBtn.Pressed += () => { OnOpenMilitaryAdvisor?.Invoke(); QueueFree(); };
        vbox.AddChild(militaryBtn);

        // F4 — Foreign Advisor
        var foreignBtn = CreateAdvisorButton("F4  FOREIGN ADVISOR", "Diplomacy & foreign relations",
            new Color(0.40f, 0.40f, 0.15f));
        foreignBtn.Pressed += () => { OnOpenForeignAdvisor?.Invoke(); QueueFree(); };
        vbox.AddChild(foreignBtn);

        // F5 — Cultural Advisor
        var culturalBtn = CreateAdvisorButton("F5  CULTURAL ADVISOR", "Culture, borders & wonders",
            new Color(0.40f, 0.15f, 0.50f));
        culturalBtn.Pressed += () => { OnOpenCulturalAdvisor?.Invoke(); QueueFree(); };
        vbox.AddChild(culturalBtn);

        // F6 — Science Advisor
        var scienceBtn = CreateAdvisorButton("F6  SCIENCE ADVISOR", "Technology research & progress",
            new Color(0.15f, 0.15f, 0.50f));
        scienceBtn.Pressed += () => { OnOpenScienceAdvisor?.Invoke(); QueueFree(); };
        vbox.AddChild(scienceBtn);

        vbox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

        var closeBtn = new Button { Text = "BACK TO MAP", CustomMinimumSize = new Vector2(0, 40) };
        var closeBtnStyle = new StyleBoxFlat { BgColor = new Color(0.25f, 0.25f, 0.25f), CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5 };
        closeBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
        closeBtn.Pressed += QueueFree;
        vbox.AddChild(closeBtn);
    }

    private Button CreateAdvisorButton(string title, string description, Color color)
    {
        var btn = new Button { CustomMinimumSize = new Vector2(0, 55) };
        btn.ClipText = false;
        btn.Text = $"{title}\n  {description}";
        btn.Alignment = HorizontalAlignment.Left;
        btn.AddThemeFontSizeOverride("font_size", 14);

        var normal = new StyleBoxFlat { BgColor = color, CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, ContentMarginLeft = 12, ContentMarginTop = 6, ContentMarginBottom = 6 };
        var hover = new StyleBoxFlat { BgColor = color.Lightened(0.2f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, ContentMarginLeft = 12, ContentMarginTop = 6, ContentMarginBottom = 6, BorderWidthLeft = 3, BorderColor = new Color(0.95f, 0.72f, 0.12f) };
        var pressed = new StyleBoxFlat { BgColor = color.Darkened(0.2f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, ContentMarginLeft = 12, ContentMarginTop = 6, ContentMarginBottom = 6 };

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", pressed);
        
        return btn;
    }
}
