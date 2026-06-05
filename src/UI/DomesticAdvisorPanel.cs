using Godot;
using CivGame.Core;
using System.Linq;

namespace CivGame.UI;

public partial class DomesticAdvisorPanel : PanelContainer
{
    private GameSimulation _sim;
    
    // UI elements to update
    private Label _incomeLabel;
    private Label _expensesLabel;
    private Label _scienceExpenseLabel;
    private Label _treasuryLabel;
    private Label _netGainLabel;
    private Label _scienceSliderLabel;
    private VBoxContainer _citiesList;
    private Label _advisorLabel;

    public DomesticAdvisorPanel(GameSimulation sim)
    {
        _sim = sim;
        
        SetAnchorsPreset(Control.LayoutPreset.FullRect);
        
        // Parchment background
        var bgStyle = new StyleBoxFlat {
            BgColor = new Color(0.9f, 0.86f, 0.75f, 1.0f),
            BorderWidthLeft = 4, BorderWidthTop = 4, BorderWidthRight = 4, BorderWidthBottom = 4,
            BorderColor = new Color(0.6f, 0.55f, 0.4f, 1.0f)
        };
        AddThemeStyleboxOverride("panel", bgStyle);

        var mainVBox = new VBoxContainer();
        
        // --- HEADER ---
        var headerLabel = new Label { Text = "D O M E S T I C   A D V I S O R" };
        headerLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        headerLabel.AddThemeFontSizeOverride("font_size", 32);
        headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        var headerMargin = new MarginContainer();
        headerMargin.AddThemeConstantOverride("margin_top", 10);
        headerMargin.AddThemeConstantOverride("margin_bottom", 10);
        headerMargin.AddChild(headerLabel);
        mainVBox.AddChild(headerMargin);

        // --- TOP SECTION (Economy Overview) ---
        var topSectionHBox = new HBoxContainer();
        topSectionHBox.Alignment = BoxContainer.AlignmentMode.Center;
        topSectionHBox.AddThemeConstantOverride("separation", 20);
        
        // 1. Income Box
        var incomePanel = new PanelContainer();
        var incomeStyle = new StyleBoxFlat { BgColor = new Color(0.85f, 0.95f, 0.75f, 1.0f), BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color(0.5f, 0.6f, 0.4f, 1.0f) };
        incomePanel.AddThemeStyleboxOverride("panel", incomeStyle);
        incomePanel.CustomMinimumSize = new Vector2(250, 100);
        var incomeMargin = new MarginContainer();
        incomeMargin.AddThemeConstantOverride("margin_left", 10);
        incomeMargin.AddThemeConstantOverride("margin_top", 10);
        
        _incomeLabel = new Label();
        _incomeLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.3f, 0.1f));
        _incomeLabel.AddThemeFontSizeOverride("font_size", 16);
        incomeMargin.AddChild(_incomeLabel);
        incomePanel.AddChild(incomeMargin);
        topSectionHBox.AddChild(incomePanel);

        // 2. Expenses Box
        var expensesPanel = new PanelContainer();
        var expensesStyle = new StyleBoxFlat { BgColor = new Color(0.95f, 0.75f, 0.75f, 1.0f), BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color(0.7f, 0.4f, 0.4f, 1.0f) };
        expensesPanel.AddThemeStyleboxOverride("panel", expensesStyle);
        expensesPanel.CustomMinimumSize = new Vector2(250, 100);
        var expMargin = new MarginContainer();
        expMargin.AddThemeConstantOverride("margin_left", 10);
        expMargin.AddThemeConstantOverride("margin_top", 10);
        
        var expVBox = new VBoxContainer();
        _expensesLabel = new Label();
        _expensesLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.1f, 0.1f));
        _expensesLabel.AddThemeFontSizeOverride("font_size", 16);
        expVBox.AddChild(_expensesLabel);
        
        _scienceExpenseLabel = new Label();
        _scienceExpenseLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.4f));
        _scienceExpenseLabel.AddThemeFontSizeOverride("font_size", 16);
        expVBox.AddChild(_scienceExpenseLabel);
        
        expMargin.AddChild(expVBox);
        expensesPanel.AddChild(expMargin);
        topSectionHBox.AddChild(expensesPanel);

        // 3. Sliders Box
        var slidersPanel = new PanelContainer();
        var slidersStyle = new StyleBoxFlat { BgColor = new Color(0.9f, 0.9f, 0.8f, 1.0f), BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color(0.6f, 0.6f, 0.5f, 1.0f) };
        slidersPanel.AddThemeStyleboxOverride("panel", slidersStyle);
        slidersPanel.CustomMinimumSize = new Vector2(300, 100);
        var sMargin = new MarginContainer();
        sMargin.AddThemeConstantOverride("margin_left", 15);
        sMargin.AddThemeConstantOverride("margin_right", 15);
        sMargin.AddThemeConstantOverride("margin_top", 15);
        
        var sliderVBox = new VBoxContainer();
        
        var scienceSliderHBox = new HBoxContainer();
        var scIcon = new Label { Text = "🧪" };
        var scSlider = new HSlider();
        scSlider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scSlider.MinValue = 0;
        scSlider.MaxValue = 100;
        scSlider.Step = 10;
        scSlider.Value = 100 - _sim.PlayerTaxRate; // Science is 100 - tax
        
        _scienceSliderLabel = new Label { Text = $"{scSlider.Value}%" };
        _scienceSliderLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.2f, 0.6f));
        _scienceSliderLabel.CustomMinimumSize = new Vector2(40, 0);
        
        scSlider.ValueChanged += (val) => {
            _sim.PlayerTaxRate = 100 - (int)val;
            _scienceSliderLabel.Text = $"{val}%";
            UpdateUI();
        };
        
        scienceSliderHBox.AddChild(scIcon);
        scienceSliderHBox.AddChild(scSlider);
        scienceSliderHBox.AddChild(_scienceSliderLabel);
        sliderVBox.AddChild(scienceSliderHBox);
        
        var sliderHelpText = new Label { Text = "Tax Rate adjusts automatically." };
        sliderHelpText.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
        sliderHelpText.AddThemeFontSizeOverride("font_size", 12);
        sliderHelpText.HorizontalAlignment = HorizontalAlignment.Center;
        sliderVBox.AddChild(sliderHelpText);

        sMargin.AddChild(sliderVBox);
        slidersPanel.AddChild(sMargin);
        topSectionHBox.AddChild(slidersPanel);

        // 4. Advisor Box
        var advPanel = new PanelContainer();
        var advStyle = new StyleBoxFlat { BgColor = new Color(1.0f, 1.0f, 0.95f, 1.0f), BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color(0.5f, 0.5f, 0.5f, 1.0f) };
        advPanel.AddThemeStyleboxOverride("panel", advStyle);
        advPanel.CustomMinimumSize = new Vector2(250, 100);
        
        var advMargin = new MarginContainer();
        advMargin.AddThemeConstantOverride("margin_left", 10);
        advMargin.AddThemeConstantOverride("margin_right", 10);
        advMargin.AddThemeConstantOverride("margin_top", 10);
        
        var advHBox = new HBoxContainer();
        var advPortrait = new Label { Text = "🧑‍💼" };
        advPortrait.AddThemeFontSizeOverride("font_size", 48);
        advPortrait.VerticalAlignment = VerticalAlignment.Center;
        
        _advisorLabel = new Label();
        _advisorLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        _advisorLabel.AddThemeFontSizeOverride("font_size", 14);
        _advisorLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _advisorLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _advisorLabel.VerticalAlignment = VerticalAlignment.Center;
        
        advHBox.AddChild(advPortrait);
        advHBox.AddChild(_advisorLabel);
        advMargin.AddChild(advHBox);
        advPanel.AddChild(advMargin);
        topSectionHBox.AddChild(advPanel);

        mainVBox.AddChild(topSectionHBox);
        
        // --- TREASURY BAR ---
        var treasuryHBox = new HBoxContainer();
        treasuryHBox.Alignment = BoxContainer.AlignmentMode.Center;
        treasuryHBox.AddThemeConstantOverride("separation", 50);
        var tMargin = new MarginContainer();
        tMargin.AddThemeConstantOverride("margin_top", 10);
        tMargin.AddThemeConstantOverride("margin_bottom", 15);
        
        _treasuryLabel = new Label();
        _treasuryLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.5f, 0.1f));
        _treasuryLabel.AddThemeFontSizeOverride("font_size", 20);
        
        _netGainLabel = new Label();
        _netGainLabel.AddThemeFontSizeOverride("font_size", 20);
        
        treasuryHBox.AddChild(_treasuryLabel);
        treasuryHBox.AddChild(_netGainLabel);
        tMargin.AddChild(treasuryHBox);
        mainVBox.AddChild(tMargin);

        // --- CITIES LIST (Bottom Section) ---
        var citiesScroll = new ScrollContainer();
        citiesScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        citiesScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var scrollStyle = new StyleBoxFlat { BgColor = new Color(0.92f, 0.89f, 0.8f, 1.0f), BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color(0.7f, 0.65f, 0.55f, 1.0f) };
        citiesScroll.AddThemeStyleboxOverride("panel", scrollStyle);
        
        var cMargin = new MarginContainer();
        cMargin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        cMargin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        cMargin.AddThemeConstantOverride("margin_left", 20);
        cMargin.AddThemeConstantOverride("margin_right", 20);
        cMargin.AddThemeConstantOverride("margin_top", 10);
        cMargin.AddThemeConstantOverride("margin_bottom", 10);
        
        _citiesList = new VBoxContainer();
        _citiesList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        
        cMargin.AddChild(_citiesList);
        citiesScroll.AddChild(cMargin);
        
        var citiesWrapperMargin = new MarginContainer();
        citiesWrapperMargin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        citiesWrapperMargin.AddThemeConstantOverride("margin_left", 30);
        citiesWrapperMargin.AddThemeConstantOverride("margin_right", 30);
        citiesWrapperMargin.AddChild(citiesScroll);
        
        mainVBox.AddChild(citiesWrapperMargin);
        
        // --- FOOTER ---
        var footerMargin = new MarginContainer();
        footerMargin.AddThemeConstantOverride("margin_top", 10);
        footerMargin.AddThemeConstantOverride("margin_bottom", 10);
        footerMargin.AddThemeConstantOverride("margin_right", 20);
        
        var closeBtn = new Button { Text = "Close Advisor" };
        closeBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        closeBtn.CustomMinimumSize = new Vector2(150, 40);
        var btnStyle = new StyleBoxFlat { BgColor = new Color(0.7f, 0.2f, 0.2f, 1.0f), CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5 };
        closeBtn.AddThemeStyleboxOverride("normal", btnStyle);
        closeBtn.Pressed += () => QueueFree();
        
        footerMargin.AddChild(closeBtn);
        mainVBox.AddChild(footerMargin);

        AddChild(mainVBox);
        
        UpdateUI();
        BuildCitiesList();
    }

    private void UpdateUI()
    {
        _incomeLabel.Text = $"From cities: +{_sim.LastTurnIncome}";
        _expensesLabel.Text = $"Maintenance: -{_sim.LastTurnMaintenance}";
        _scienceExpenseLabel.Text = $"Science: -{_sim.LastTurnScience}";
        
        _treasuryLabel.Text = $"Treasury: {_sim.PlayerTreasury} Gold";
        
        if (_sim.LastTurnNetGold >= 0)
        {
            _netGainLabel.Text = $"Net Gain: +{_sim.LastTurnNetGold}";
            _netGainLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.6f, 0.2f));
            _advisorLabel.Text = "Sire, our economy is stable and our treasury grows!";
        }
        else
        {
            _netGainLabel.Text = $"Net Gain: {_sim.LastTurnNetGold}";
            _netGainLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
            _advisorLabel.Text = "We are losing money! We must reduce maintenance or lower science spending!";
        }
    }

    private void BuildCitiesList()
    {
        // Clear previous
        foreach (var child in _citiesList.GetChildren())
            child.QueueFree();
            
        // Header
        var headerRow = CreateRow("Cities", "🌾", "🔨", "🪙", "🏛️", "Population", "Producing", true);
        _citiesList.AddChild(headerRow);
        
        var separator = new HSeparator();
        _citiesList.AddChild(separator);
        
        var playerCities = _sim.Cities.Where(c => c.Faction == Faction.Player).ToList();
        
        foreach (var city in playerCities)
        {
            // Calculate yields for this city manually to display them accurately
            int food = 0, prod = 0, comm = 0;
            var centerTile = _sim.Map.GetTile(city.X, city.Y);
            if (centerTile != null)
            {
                food += centerTile.TotalYield.Food;
                prod += centerTile.TotalYield.Production;
                comm += centerTile.TotalYield.Commerce;
            }
            
            foreach (var tilePos in city.WorkedTiles)
            {
                var t = _sim.Map.GetTile(tilePos.X, tilePos.Y);
                if (t != null && t.OwnerCityId == city.Id)
                {
                    food += t.TotalYield.Food;
                    prod += t.TotalYield.Production;
                    comm += t.TotalYield.Commerce;
                }
            }
            
            int maint = city.GetTotalMaintenance();
            string prodText = city.CurrentProject == ProductionProject.None ? "Idle" : city.CurrentProject.ToString();
            if (city.CurrentProject != ProductionProject.None)
            {
                int cost = city.GetProjectCost(city.CurrentProject);
                int turnsLeft = prod > 0 ? (int)System.Math.Ceiling((double)(cost - city.CurrentProductionProgress) / prod) : 999;
                prodText = $"{prodText}\n({turnsLeft} turns)";
            }
            
            var row = CreateRow(
                city.Name,
                food.ToString(),
                prod.ToString(),
                comm.ToString(),
                maint.ToString(),
                city.Population.ToString(),
                prodText,
                false
            );
            _citiesList.AddChild(row);
        }
    }

    private HBoxContainer CreateRow(string col1, string col2, string col3, string col4, string col5, string colPop, string colProd, bool isHeader)
    {
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        
        var c1 = new Label { Text = col1, CustomMinimumSize = new Vector2(150, 0), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        var c2 = new Label { Text = col2, CustomMinimumSize = new Vector2(40, 0), HorizontalAlignment = HorizontalAlignment.Center };
        var c3 = new Label { Text = col3, CustomMinimumSize = new Vector2(40, 0), HorizontalAlignment = HorizontalAlignment.Center };
        var c4 = new Label { Text = col4, CustomMinimumSize = new Vector2(40, 0), HorizontalAlignment = HorizontalAlignment.Center };
        var c5 = new Label { Text = col5, CustomMinimumSize = new Vector2(40, 0), HorizontalAlignment = HorizontalAlignment.Center };
        
        var cPop = new Label();
        cPop.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        cPop.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        
        if (isHeader)
        {
            cPop.Text = colPop;
            cPop.HorizontalAlignment = HorizontalAlignment.Center;
        }
        else
        {
            // Draw citizens!
            int pop = int.Parse(colPop);
            cPop.Text = string.Concat(Enumerable.Repeat("🧑", pop));
            cPop.AddThemeFontSizeOverride("font_size", 16);
            cPop.HorizontalAlignment = HorizontalAlignment.Left;
        }
        
        var cProd = new Label { Text = colProd, CustomMinimumSize = new Vector2(150, 0), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        
        if (isHeader)
        {
            Color headerColor = new Color(0.3f, 0.1f, 0.3f);
            c1.AddThemeColorOverride("font_color", headerColor);
            c2.AddThemeColorOverride("font_color", headerColor);
            c3.AddThemeColorOverride("font_color", headerColor);
            c4.AddThemeColorOverride("font_color", headerColor);
            c5.AddThemeColorOverride("font_color", headerColor);
            cPop.AddThemeColorOverride("font_color", headerColor);
            cProd.AddThemeColorOverride("font_color", headerColor);
        }
        else
        {
            Color dataColor = new Color(0.4f, 0.1f, 0.3f); // Purplish text from the screenshot
            c1.AddThemeColorOverride("font_color", dataColor);
            c2.AddThemeColorOverride("font_color", dataColor);
            c3.AddThemeColorOverride("font_color", dataColor);
            c4.AddThemeColorOverride("font_color", dataColor);
            c5.AddThemeColorOverride("font_color", dataColor);
            cProd.AddThemeColorOverride("font_color", new Color(0.3f, 0.1f, 0.6f)); // Bluer for production
        }

        row.AddChild(c1);
        row.AddChild(c2);
        row.AddChild(c3);
        row.AddChild(c4);
        row.AddChild(c5);
        row.AddChild(cPop);
        row.AddChild(cProd);
        
        return row;
    }
}
