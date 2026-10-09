using Godot;
using CivGame.Core;
using System.Linq;
using System.Text;

namespace CivGame.UI;

/// <summary>
/// Civ3-style "Wonders of the World" screen (F7): lists great wonders already built
/// (with owning civilization and city) and wonders still available to build.
/// </summary>
public partial class WondersPanel : PanelContainer
{
    private GameSimulation _sim;

    private static readonly Color DarkText = new(0.15f, 0.12f, 0.08f);
    private static readonly Color PlayerGreen = new(0.15f, 0.45f, 0.15f);
    private static readonly Color AiPurple = new(0.40f, 0.15f, 0.50f);
    private static readonly Color HeaderGold = new(0.45f, 0.32f, 0.10f);
    private static readonly Color GoodGreen = new(0.15f, 0.45f, 0.15f);
    private static readonly Color DimGray = new(0.45f, 0.45f, 0.45f);

    public WondersPanel(GameSimulation sim, bool embedded = false)
    {
        _sim = sim;
        Name = "WondersPanel";

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
                Text = "W O N D E R S   O F   T H E   W O R L D",
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

        // --- BUILT WONDERS ---
        mainVBox.AddChild(CreateSectionHeader("BUILT WONDERS"));

        var builtGrid = new GridContainer { Columns = 4 };
        builtGrid.AddThemeConstantOverride("h_separation", 25);
        builtGrid.AddThemeConstantOverride("v_separation", 4);
        builtGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        AddGridHeader(builtGrid, "Wonder", "Civilization", "City", "Effect");

        var greatWonders = WonderRegistry.All.Values
            .Where(w => !w.IsNationalWonder)
            .OrderBy(w => w.ProductionCost)
            .ToList();

        var builtWonders = greatWonders
            .Select(w => (Wonder: w, City: _sim.Cities.FirstOrDefault(c => c.Buildings.Any(b => b.Id == w.Id))))
            .Where(t => t.City != null)
            .ToList();

        if (builtWonders.Count == 0)
        {
            var noneLabel = new Label { Text = "No great wonders have been built yet." };
            noneLabel.AddThemeColorOverride("font_color", DimGray);
            noneLabel.AddThemeFontSizeOverride("font_size", 14);
            builtGrid.AddChild(noneLabel);
        }
        else
        {
            foreach (var (wonder, city) in builtWonders)
            {
                string civName = city!.Civilization?.Name ?? city.Faction.ToString();
                var civColor = city.Faction == Faction.Player ? PlayerGreen : AiPurple;
                string effect = wonder is GenericWonder gw ? gw.Description : wonder.GetDescription();

                AddGridCell(builtGrid, wonder.Name, DarkText, bold: true);
                AddGridCell(builtGrid, civName, civColor);
                AddGridCell(builtGrid, city.Name, DarkText);
                AddGridCell(builtGrid, effect, DarkText, expand: true);
            }
        }

        mainVBox.AddChild(builtGrid);
        mainVBox.AddChild(new HSeparator());

        // --- AVAILABLE WONDERS ---
        mainVBox.AddChild(CreateSectionHeader("AVAILABLE WONDERS"));

        var availGrid = new GridContainer { Columns = 4 };
        availGrid.AddThemeConstantOverride("h_separation", 25);
        availGrid.AddThemeConstantOverride("v_separation", 4);
        availGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        AddGridHeader(availGrid, "Wonder", "Cost", "Requires", "Status");

        var availableWonders = greatWonders
            .Where(w => !_sim.Cities.Any(c => c.Buildings.Any(b => b.Id == w.Id)))
            .ToList();

        foreach (var wonder in availableWonders)
        {
            string techName = wonder.RequiredTechId != null
                ? _sim.Research.AllTechnologies.Find(t => t.Id == wonder.RequiredTechId)?.Name ?? wonder.RequiredTechId
                : "—";
            bool hasPrereq = wonder.RequiredTechId == null || _sim.Research.IsResearched(wonder.RequiredTechId);

            var builder = FindCityBuildingWonder(wonder);
            string status;
            Color statusColor;
            if (builder != null)
            {
                string builderCiv = builder.Civilization?.Name ?? builder.Faction.ToString();
                status = $"Being built in: {builder.Name} ({builderCiv})";
                statusColor = DarkText;
            }
            else if (hasPrereq)
            {
                status = "✓";
                statusColor = GoodGreen;
            }
            else
            {
                status = $"✗ {techName}";
                statusColor = DimGray;
            }

            AddGridCell(availGrid, wonder.Name, DarkText, bold: true);
            AddGridCell(availGrid, $"{wonder.ProductionCost} shields", DarkText);
            AddGridCell(availGrid, techName, DarkText);
            AddGridCell(availGrid, status, statusColor, expand: true);
        }

        mainVBox.AddChild(availGrid);

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

    /// <summary>
    /// Finds a city currently producing this wonder (CurrentProject or ProductionQueue),
    /// mapping ProductionProject enum names to snake_case building ids.
    /// </summary>
    private City? FindCityBuildingWonder(Wonder wonder)
    {
        foreach (var city in _sim.Cities)
        {
            if (ProjectToBuildingId(city.CurrentProject) == wonder.Id)
                return city;
            if (city.ProductionQueue.Any(p => ProjectToBuildingId(p) == wonder.Id))
                return city;
        }
        return null;
    }

    private static string? ProjectToBuildingId(ProductionProject project)
    {
        if (project == ProductionProject.None) return null;
        var sb = new StringBuilder();
        string name = project.ToString();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static Label CreateSectionHeader(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", HeaderGold);
        label.AddThemeFontSizeOverride("font_size", 18);
        return label;
    }

    private static void AddGridHeader(GridContainer grid, params string[] columns)
    {
        foreach (var col in columns)
        {
            var label = new Label { Text = col };
            label.AddThemeColorOverride("font_color", HeaderGold);
            label.AddThemeFontSizeOverride("font_size", 14);
            grid.AddChild(label);
        }
    }

    private static void AddGridCell(GridContainer grid, string text, Color color, bool bold = false, bool expand = false)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.Word };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", bold ? 15 : 13);
        if (expand) label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        grid.AddChild(label);
    }
}
