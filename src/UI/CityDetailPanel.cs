using Godot;
using CivGame.Core;
using System.Linq;
using System.Collections.Generic;

namespace CivGame.UI;

public partial class CityDetailPanel : PanelContainer
{
    private City _city;
    private GameSimulation _sim;
    
    private HBoxContainer? _bottomBar;
    private VBoxContainer? _buildingsContainer;
    private VBoxContainer? _productionContainer;
    private GridContainer? _mapGrid;
    private Label? _titleLabel;
    private Label? _popLabel;

    public CityDetailPanel(City city, GameSimulation sim)
    {
        _city = city;
        _sim = sim;

        ConfigurePanel();
        CreateLayout();
        Refresh();
    }

    private void ConfigurePanel()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 100;
        AnchorLeft = 0.5f;
        AnchorTop = 0.5f;
        AnchorRight = 0.5f;
        AnchorBottom = 0.5f;
        OffsetLeft = -500;
        OffsetTop = -350;
        OffsetRight = 500;
        OffsetBottom = 350;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.96f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.5f, 0.7f, 1.0f, 0.8f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        };
        AddThemeStyleboxOverride("panel", style);
    }

    private void CreateLayout()
    {
        var vbox = new VBoxContainer();
        AddChild(vbox);

        // Header
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 20);
        vbox.AddChild(header);

        _titleLabel = new Label { Text = _city.Name.ToUpper() };
        _titleLabel.AddThemeFontSizeOverride("font_size", 28);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.4f));
        header.AddChild(_titleLabel);

        header.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        _popLabel = new Label { Text = $"POP: {_city.Population}" };
        _popLabel.AddThemeFontSizeOverride("font_size", 22);
        _popLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        header.AddChild(_popLabel);

        vbox.AddChild(new HSeparator());

        // Main content - Map view centered
        var centerContainer = new CenterContainer();
        centerContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(centerContainer);

        var mapPanel = new PanelContainer();
        mapPanel.CustomMinimumSize = new Vector2(400, 300);
        centerContainer.AddChild(mapPanel);

        _mapGrid = new GridContainer { Columns = 5, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _mapGrid.AddThemeConstantOverride("h_separation", 1);
        _mapGrid.AddThemeConstantOverride("v_separation", 1);
        mapPanel.AddChild(_mapGrid);

        vbox.AddChild(new HSeparator());

        // Bottom bar - Buildings (left) and Production (right)
        _bottomBar = new HBoxContainer();
        _bottomBar.AddThemeConstantOverride("separation", 15);
        vbox.AddChild(_bottomBar);

        // Left: Buildings
        _buildingsContainer = new VBoxContainer();
        _buildingsContainer.CustomMinimumSize = new Vector2(180, 0);
        _bottomBar.AddChild(_buildingsContainer);

        // Right: Production
        _productionContainer = new VBoxContainer();
        _productionContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _bottomBar.AddChild(_productionContainer);

        // Close button
        var footer = new HBoxContainer();
        footer.Alignment = BoxContainer.AlignmentMode.End;
        footer.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(footer);

        var closeBtn = new Button { Text = "CLOSE" };
        closeBtn.CustomMinimumSize = new Vector2(120, 32);
        closeBtn.Pressed += QueueFree;
        footer.AddChild(closeBtn);
    }

    public void Refresh()
    {
        _titleLabel.Text = _city.Name.ToUpper();
        _popLabel.Text = $"POP: {_city.Population}";

        RefreshMapGrid();
        RefreshBuildings();
        RefreshProduction();
    }

    private void RefreshMapGrid()
    {
        foreach (var child in _mapGrid.GetChildren()) child.QueueFree();

        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int tileX = _city.X + dx;
                int tileY = _city.Y + dy;

                var tileData = _sim.Map.GetTile(tileX, tileY);
                var terrain = tileData?.Terrain;
                var tileColor = terrain?.Color ?? Colors.Black;

                // Determine yields
                int food = 0, production = 0, commerce = 0;
                if (tileData != null && _city.WorkedTiles.Contains((tileX, tileY)))
                {
                    food = CalculateFoodYield(tileData);
                    production = CalculateProductionYield(tileData);
                    commerce = CalculateCommerceYield(tileData);
                }

                var panel = CreateTilePanel(tileColor, food, production, commerce, dx, dy);
                _mapGrid.AddChild(panel);
            }
        }
    }

    private int CalculateFoodYield(CivGame.Core.TileData tile)
    {
        return tile.TotalYield.Food;
    }

    private int CalculateProductionYield(CivGame.Core.TileData tile)
    {
        return tile.TotalYield.Production;
    }

    private int CalculateCommerceYield(CivGame.Core.TileData tile)
    {
        return tile.TotalYield.Commerce;
    }

    private Panel CreateTilePanel(Color baseColor, int food, int production, int commerce, int dx, int dy)
    {
        var panel = new Panel();
        panel.CustomMinimumSize = new Vector2(50, 50);

        // Visual indicator for yields
        Color bgColor = baseColor;
        if (food > 0) bgColor = new Color(bgColor.R, Mathf.Min(1, bgColor.G + 0.3f), bgColor.B, bgColor.A);
        if (production > 0) bgColor = new Color(Mathf.Min(1, bgColor.R + 0.3f), bgColor.G, bgColor.B, bgColor.A);
        if (commerce > 0) bgColor = new Color(bgColor.R, bgColor.G, Mathf.Min(1, bgColor.B + 0.3f), bgColor.A);

        var style = new StyleBoxFlat { BgColor = bgColor, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = Colors.Black };
        panel.AddThemeStyleboxOverride("panel", style);

        // Show yield indicators
        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        if (food > 0 || production > 0 || commerce > 0)
        {
            var yieldLabel = new Label 
            { 
                Text = $"{food}\n{production}\n{commerce}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            yieldLabel.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(yieldLabel);
        }

        // Highlight city center
        if (dx == 0 && dy == 0)
        {
            var centerLabel = new Label { Text = "🏛️", HorizontalAlignment = HorizontalAlignment.Center };
            centerLabel.AddThemeFontSizeOverride("font_size", 24);
            panel.AddChild(centerLabel);
        }

        // Worked tile indicator
        int tileX = _city.X + dx;
        int tileY = _city.Y + dy;
        if (_city.WorkedTiles.Contains((tileX, tileY)) && !(dx == 0 && dy == 0))
        {
            var workedLabel = new Label { Text = "👤", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            workedLabel.AddThemeFontSizeOverride("font_size", 16);
            panel.AddChild(workedLabel);
        }

        return panel;
    }

    private void RefreshBuildings()
    {
        foreach (var child in _buildingsContainer.GetChildren()) child.QueueFree();

        var title = new Label { Text = "BUILDINGS" };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1.0f));
        _buildingsContainer.AddChild(title);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _buildingsContainer.AddChild(scroll);

        var list = new VBoxContainer();
        scroll.AddChild(list);

        if (_city.Buildings.Count == 0)
        {
            list.AddChild(new Label { Text = "(none)" });
        }
        else
        {
            foreach (var b in _city.Buildings)
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 8);

                var name = new Label { Text = b.Name };
                row.AddChild(name);

                if (WonderRegistry.Get(b.Id) is Wonder wonder && !wonder.IsNationalWonder)
                {
                    var wonderTag = new Label { Text = "★" };
                    wonderTag.AddThemeColorOverride("font_color", new Color(1.0f, 0.8f, 0.2f));
                    row.AddChild(wonderTag);
                }

                list.AddChild(row);
            }
        }
    }

    private void RefreshProduction()
    {
        foreach (var child in _productionContainer.GetChildren()) child.QueueFree();

        var title = new Label { Text = "PRODUCTION" };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1.0f));
        _productionContainer.AddChild(title);

        // Production yield
        int totalShields = _city.WorkedTiles.Sum(pos => {
            var tile = _sim.Map.GetTile(pos.X, pos.Y);
            return tile?.TotalYield.Production ?? 0;
        });
        var shieldsLabel = new Label { Text = $"Shields: {totalShields}" };
        shieldsLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1.0f));
        _productionContainer.AddChild(shieldsLabel);

        // Production progress bar
        var progressBox = new VBoxContainer();
        _productionContainer.AddChild(progressBox);

        var progressBar = new ProgressBar();
        progressBar.MinValue = 0;
        progressBar.MaxValue = _city.CurrentProject != ProductionProject.None ? _city.GetProjectCost(_city.CurrentProject) : 1;
        progressBar.Value = _city.CurrentProductionProgress;
        progressBar.CustomMinimumSize = new Vector2(0, 28);
        progressBar.ShowPercentage = true;

        string projectText = _city.CurrentProject == ProductionProject.None 
            ? "Idle" 
            : $"{_city.CurrentProject} • {_city.CurrentProductionProgress}/{_city.GetProjectCost(_city.CurrentProject)}";

        var projectLabel = new Label { Text = projectText };
        projectLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        progressBox.AddChild(projectLabel);
        progressBox.AddChild(progressBar);

        // Cycle production button
        var cycleBtn = new Button { Text = "Cycle Production" };
        cycleBtn.CustomMinimumSize = new Vector2(0, 32);
        cycleBtn.Pressed += () => {
            _sim.CycleCityProduction(_city);
            Refresh();
        };
        _productionContainer.AddChild(cycleBtn);
    }
}