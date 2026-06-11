using Godot;
using CivGame.Core;

namespace CivGame.UI.CityComponents;

public partial class CityHeaderComponent : HBoxContainer
{
    public CityHeaderComponent(City city)
    {
        AddThemeConstantOverride("separation", 20);

        // 2. Navigácia (Šípky) - Zástupný text, neskôr nahradíme textúrami
        AddChild(new Button { Text = "◀" });

        // 3. Info (Meno, Pop, Dátum založenia)
        var infoVBox = new VBoxContainer();
        var nameLabel = new Label { Text = city.Name.ToUpper() };
        nameLabel.AddThemeFontSizeOverride("font_size", 24);
        nameLabel.AddThemeColorOverride("font_color", Colors.Black);
        infoVBox.AddChild(nameLabel);
        
        var statsLabel = new Label { Text = $"POP: {city.Population} | Founded: Turn {city.FoundedYear}" };
        statsLabel.AddThemeColorOverride("font_color", Colors.Black);
        infoVBox.AddChild(statsLabel);
        AddChild(infoVBox);

        // 4. Kultúra a Zlato (Zástupné zobrazenie)
        var economyHBox = new HBoxContainer();
        economyHBox.AddChild(new Label { Text = "💰", TooltipText = "Gold" });
        economyHBox.AddChild(new Label { Text = " 🏛️", TooltipText = "Culture" });
        AddChild(economyHBox);

        // 5. Zavretie
        var closeBtn = new Button { Text = "X" };
        closeBtn.Pressed += () => GetParent().GetParent().QueueFree(); 
        AddChild(closeBtn);
    }
}
