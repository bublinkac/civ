using Godot;
using CivGame.Core;
using System.Linq;
using System.Collections.Generic;

namespace CivGame.UI;

public partial class TechTreePanel : PanelContainer
{
    private GameSimulation _sim;
    private TechEra _currentEra = TechEra.Ancient;
    
    private Control _canvas;
    private HBoxContainer _tabsContainer;
    private Label _advisorLabel;
    
    // UI mapping
    private Dictionary<string, TechNodeControl> _nodes = new();
    
    // Grid settings
    private const int GRID_CELL_WIDTH = 260;
    private const int GRID_CELL_HEIGHT = 100;
    private const int NODE_WIDTH = 200;
    private const int NODE_HEIGHT = 65;
    private const int MARGIN_X = 40;
    private const int MARGIN_Y = 40;

    public TechTreePanel(GameSimulation sim)
    {
        _sim = sim;

        // Panel Setup
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 120;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.92f, 0.88f, 0.78f, 1.0f), // Papyrus/Parchment background like Civ3
            BorderWidthLeft = 6, BorderWidthTop = 6, BorderWidthRight = 6, BorderWidthBottom = 6,
            BorderColor = new Color(0.85f, 0.8f, 0.65f, 1.0f),
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ShadowSize = 25,
            ShadowColor = new Color(0, 0, 0, 0.6f)
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(1000, 700);
        AnchorLeft = 0.5f;
        AnchorTop = 0.5f;
        AnchorRight = 0.5f;
        AnchorBottom = 0.5f;
        OffsetLeft = -500;
        OffsetTop = -350;
        OffsetRight = 500;
        OffsetBottom = 350;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        var mainVBox = new VBoxContainer();
        AddChild(mainVBox);

        // --- HEADER ---
        var headerMargin = new MarginContainer();
        headerMargin.AddThemeConstantOverride("margin_top", 10);
        headerMargin.AddThemeConstantOverride("margin_bottom", 10);
        
        var titleLabel = new Label { Text = "S C I E N C E   A D V I S O R", HorizontalAlignment = HorizontalAlignment.Center };
        titleLabel.AddThemeFontSizeOverride("font_size", 28);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        titleLabel.AddThemeFontOverride("font", ThemeDB.FallbackFont); // Use a serif font ideally
        headerMargin.AddChild(titleLabel);
        mainVBox.AddChild(headerMargin);

        // --- ERA TABS ---
        var tabsHBox = new HBoxContainer();
        tabsHBox.Alignment = BoxContainer.AlignmentMode.Center;
        tabsHBox.AddThemeConstantOverride("separation", 15);
        
        _tabsContainer = tabsHBox;
        mainVBox.AddChild(tabsHBox);
        
        BuildEraTabs();

        // --- MAIN CANVAS AREA (Scrollable) ---
        var canvasBorder = new PanelContainer();
        canvasBorder.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        canvasBorder.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var canvasStyle = new StyleBoxFlat {
            BgColor = new Color(0.9f, 0.86f, 0.75f, 1.0f),
            BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3,
            BorderColor = new Color(0.6f, 0.55f, 0.4f, 1.0f)
        };
        canvasBorder.AddThemeStyleboxOverride("panel", canvasStyle);
        
        var canvasWrapper = new Control();
        canvasWrapper.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvasBorder.AddChild(canvasWrapper);

        var marginCanvas = new MarginContainer();
        marginCanvas.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        marginCanvas.AddThemeConstantOverride("margin_left", 8);
        marginCanvas.AddThemeConstantOverride("margin_right", 8);
        marginCanvas.AddThemeConstantOverride("margin_top", 8);
        marginCanvas.AddThemeConstantOverride("margin_bottom", 8);
        canvasWrapper.AddChild(marginCanvas);
        
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        
        // Custom control for drawing lines
        _canvas = new TechTreeCanvas(this);
        _canvas.CustomMinimumSize = new Vector2(1600, 800); // Will be updated dynamically based on era
        scroll.AddChild(_canvas);
        marginCanvas.AddChild(scroll);
        mainVBox.AddChild(canvasBorder);

        // --- ADVISOR OVERLAY (Top Right of canvas) ---
        var advisorOverlay = new MarginContainer();
        advisorOverlay.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        advisorOverlay.AddThemeConstantOverride("margin_right", 20);
        advisorOverlay.AddThemeConstantOverride("margin_top", 20);
        advisorOverlay.MouseFilter = MouseFilterEnum.Ignore;
        
        var advisorPanel = new PanelContainer();
        advisorPanel.CustomMinimumSize = new Vector2(250, 60);
        var advStyle = new StyleBoxFlat {
            BgColor = new Color(1.0f, 1.0f, 0.95f, 0.95f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.6f, 0.3f, 1.0f),
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ShadowSize = 5, ShadowColor = new Color(0, 0, 0, 0.3f)
        };
        advisorPanel.AddThemeStyleboxOverride("panel", advStyle);
        
        var advHBox = new HBoxContainer();
        advHBox.AddThemeConstantOverride("separation", 10);
        
        var advIcon = new Label { Text = "🧑‍🔬" };
        advIcon.AddThemeFontSizeOverride("font_size", 32);
        advIcon.VerticalAlignment = VerticalAlignment.Center;
        advHBox.AddChild(advIcon);
        
        _advisorLabel = new Label();
        _advisorLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        _advisorLabel.AddThemeFontSizeOverride("font_size", 12);
        _advisorLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _advisorLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _advisorLabel.VerticalAlignment = VerticalAlignment.Center;
        advHBox.AddChild(_advisorLabel);
        
        var closeAdvisorBtn = new Button { Text = "✖" };
        closeAdvisorBtn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var advCloseStyle = new StyleBoxEmpty();
        var advCloseStyleHover = new StyleBoxFlat { BgColor = new Color(0.8f, 0.2f, 0.2f, 0.2f), CornerRadiusTopLeft=3, CornerRadiusTopRight=3, CornerRadiusBottomLeft=3, CornerRadiusBottomRight=3 };
        closeAdvisorBtn.AddThemeStyleboxOverride("normal", advCloseStyle);
        closeAdvisorBtn.AddThemeStyleboxOverride("hover", advCloseStyleHover);
        closeAdvisorBtn.AddThemeColorOverride("font_color", new Color(0.6f, 0.2f, 0.2f));
        closeAdvisorBtn.Pressed += () => advisorOverlay.Visible = false;
        advHBox.AddChild(closeAdvisorBtn);
        
        var advMargin = new MarginContainer();
        advMargin.AddThemeConstantOverride("margin_left", 8);
        advMargin.AddThemeConstantOverride("margin_right", 8);
        advMargin.AddThemeConstantOverride("margin_top", 4);
        advMargin.AddThemeConstantOverride("margin_bottom", 4);
        advMargin.AddChild(advHBox);
        advisorPanel.AddChild(advMargin);
        
        advisorOverlay.AddChild(advisorPanel);
        canvasWrapper.AddChild(advisorOverlay); // Add on top of scroll container

        // --- FOOTER ---
        var footerMargin = new MarginContainer();
        footerMargin.AddThemeConstantOverride("margin_top", 10);
        footerMargin.AddThemeConstantOverride("margin_bottom", 10);
        footerMargin.AddThemeConstantOverride("margin_left", 20);
        footerMargin.AddThemeConstantOverride("margin_right", 20);
        
        var footerHBox = new HBoxContainer();
        
        // Era navigation arrows
        var leftArrow = new Button { Text = "◄ To Previous Era" };
        leftArrow.Pressed += () => ChangeEra(_currentEra - 1);
        footerHBox.AddChild(leftArrow);
        
        var footerEraLabel = new Label { Text = "Current Era" };
        footerEraLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        footerEraLabel.HorizontalAlignment = HorizontalAlignment.Center;
        footerEraLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        footerEraLabel.AddThemeFontSizeOverride("font_size", 18);
        footerHBox.AddChild(footerEraLabel);
        
        var rightArrow = new Button { Text = "To Next Era ►" };
        rightArrow.Pressed += () => ChangeEra(_currentEra + 1);
        footerHBox.AddChild(rightArrow);
        
        // Exit button
        var closeBtn = new Button { Text = "X", CustomMinimumSize = new Vector2(40, 0) };
        var closeStyle = new StyleBoxFlat { BgColor = new Color(0.8f, 0.2f, 0.2f), CornerRadiusTopLeft=3, CornerRadiusTopRight=3, CornerRadiusBottomLeft=3, CornerRadiusBottomRight=3 };
        closeBtn.AddThemeStyleboxOverride("normal", closeStyle);
        closeBtn.Pressed += QueueFree;
        footerHBox.AddChild(closeBtn);
        
        footerMargin.AddChild(footerHBox);
        mainVBox.AddChild(footerMargin);

        // Set initial state
        DetermineInitialEra();
        Refresh();
    }
    
    private void DetermineInitialEra()
    {
        // Find era with active research, or latest era with available techs
        if (_sim.Research.CurrentResearch != null)
        {
            _currentEra = _sim.Research.CurrentResearch.Era;
        }
        else
        {
            var recommended = _sim.Research.GetRecommendedResearch();
            if (recommended != null)
            {
                _currentEra = recommended.Era;
            }
        }
    }

    private void BuildEraTabs()
    {
        foreach (var child in _tabsContainer.GetChildren()) child.QueueFree();
        
        string[] eraNames = { "Ancient Times", "Middle Ages", "Industrial Ages", "Modern Times" };
        TechEra[] eras = { TechEra.Ancient, TechEra.MiddleAges, TechEra.Industrial, TechEra.Modern };
        
        for (int i = 0; i < eras.Length; i++)
        {
            TechEra era = eras[i];
            
            var btn = new Button { Text = eraNames[i] };
            btn.AddThemeFontSizeOverride("font_size", 16);
            btn.Flat = true;
            
            if (era == _currentEra)
            {
                btn.AddThemeColorOverride("font_color", new Color(0.2f, 0.5f, 0.8f));
                btn.AddThemeFontOverride("font", ThemeDB.FallbackFont); // bold ideally
            }
            else
            {
                btn.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
            }
            
            btn.Pressed += () => ChangeEra(era);
            _tabsContainer.AddChild(btn);
            
            if (i < eras.Length - 1)
            {
                var sep = new Label { Text = "|" };
                sep.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
                _tabsContainer.AddChild(sep);
            }
        }
    }
    
    private void ChangeEra(TechEra era)
    {
        if (era < TechEra.Ancient || era > TechEra.Modern) return;
        _currentEra = era;
        BuildEraTabs();
        Refresh();
    }

    public void Refresh()
    {
        // Update Advisor
        UpdateAdvisorText();
        
        // Clear nodes
        foreach (var node in _nodes.Values)
        {
            node.QueueFree();
        }
        _nodes.Clear();

        var techsInEra = _sim.Research.GetTechnologiesByEra(_currentEra);
        
        int maxCol = 0;
        int maxRow = 0;

        foreach (var tech in techsInEra)
        {
            if (tech.Column > maxCol) maxCol = tech.Column;
            if (tech.Row > maxRow) maxRow = tech.Row;

            var node = new TechNodeControl(tech, _sim, this);
            
            // Calculate absolute position
            int x = MARGIN_X + tech.Column * GRID_CELL_WIDTH;
            int y = MARGIN_Y + tech.Row * GRID_CELL_HEIGHT;
            
            node.Position = new Vector2(x, y);
            node.Size = new Vector2(NODE_WIDTH, NODE_HEIGHT);
            
            _canvas.AddChild(node);
            _nodes[tech.Id] = node;
        }
        
        // Update canvas size to fit all nodes
        _canvas.CustomMinimumSize = new Vector2(
            MARGIN_X * 2 + (maxCol + 1) * GRID_CELL_WIDTH,
            MARGIN_Y * 2 + (maxRow + 1) * GRID_CELL_HEIGHT
        );
        
        // Trigger line redraw
        _canvas.QueueRedraw();
    }
    
    private void UpdateAdvisorText()
    {
        var active = _sim.Research.CurrentResearch;
        var recommended = _sim.Research.GetRecommendedResearch();
        
        if (active != null)
        {
            _advisorLabel.Text = $"We are currently researching {active.Name}.\n" +
                               $"{_sim.Research.CurrentScienceProgress} / {active.ScienceCost} Science.\n" +
                               $"(+{_sim.Research.LastTurnScienceGenerated} per turn)";
        }
        else if (recommended != null)
        {
            _advisorLabel.Text = $"Our scientific research is idle!\nI recommend we focus on {recommended.Name}.";
        }
        else
        {
            _advisorLabel.Text = "We have discovered all technologies of this era!\nPlease advance to the next age.";
        }
    }

    public void OnTechNodeClicked(Technology tech)
    {
        if (_sim.Research.CanResearch(tech))
        {
            _sim.Research.SetResearch(tech);
            Refresh();
        }
    }
    
    public Technology? GetTechnology(string id)
    {
        return _sim.Research.AllTechnologies.Find(t => t.Id == id);
    }
    
    public Vector2 GetNodeCenter(string techId)
    {
        if (_nodes.TryGetValue(techId, out var node))
        {
            return node.Position + node.Size / 2;
        }
        return Vector2.Zero;
    }
    
    public Dictionary<string, TechNodeControl> GetAllNodesInCurrentEra()
    {
        return _nodes;
    }
}

// Custom control for drawing dependency lines
public partial class TechTreeCanvas : Control
{
    private TechTreePanel _panel;
    
    public TechTreeCanvas(TechTreePanel panel)
    {
        _panel = panel;
    }
    
    public override void _Draw()
    {
        var nodes = _panel.GetAllNodesInCurrentEra();
        Color lineColor = new Color(0.6f, 0.4f, 0.2f, 0.6f); // Brown-ish lines like Civ3
        float lineWidth = 3.0f;
        
        foreach (var pair in nodes)
        {
            var destTech = pair.Value.Technology;
            Vector2 destCenter = pair.Value.Position + new Vector2(0, pair.Value.Size.Y / 2); // Left edge center
            
            foreach (var prereqId in destTech.PrerequisiteIds)
            {
                var prereqTech = _panel.GetTechnology(prereqId);
                if (prereqTech == null) continue;
                
                Vector2 startPos = Vector2.Zero;
                
                // If prereq is in the same era, we have its node
                if (nodes.TryGetValue(prereqId, out var prereqNode))
                {
                    startPos = prereqNode.Position + new Vector2(prereqNode.Size.X, prereqNode.Size.Y / 2); // Right edge center
                }
                else if (prereqTech.Era < destTech.Era)
                {
                    // Prereq is in a previous era - draw from the left edge of the screen
                    startPos = new Vector2(0, destCenter.Y);
                }
                
                if (startPos != Vector2.Zero)
                {
                    // Draw a broken line (bezier-like or orthogonal)
                    DrawOrthogonalLine(startPos, destCenter, lineColor, lineWidth);
                }
            }
        }
    }
    
    private void DrawOrthogonalLine(Vector2 start, Vector2 end, Color color, float width)
    {
        // Simple 3-segment orthogonal line
        float midX = start.X + (end.X - start.X) / 2;
        
        Vector2 p1 = start;
        Vector2 p2 = new Vector2(midX, start.Y);
        Vector2 p3 = new Vector2(midX, end.Y);
        Vector2 p4 = end;
        
        DrawLine(p1, p2, color, width, true);
        DrawLine(p2, p3, color, width, true);
        
        // Draw arrow head at the end
        DrawLine(p3, p4, color, width, true);
        
        // Arrowhead
        float arrowSize = 8.0f;
        Vector2[] arrowPoints = {
            end,
            end + new Vector2(-arrowSize, -arrowSize*0.6f),
            end + new Vector2(-arrowSize, arrowSize*0.6f)
        };
        DrawPolygon(arrowPoints, new Color[] { color, color, color });
    }
}

// Visual representation of a single technology
public partial class TechNodeControl : PanelContainer
{
    public Technology Technology { get; }
    private GameSimulation _sim;
    private TechTreePanel _parent;

    public TechNodeControl(Technology tech, GameSimulation sim, TechTreePanel parent)
    {
        Technology = tech;
        _sim = sim;
        _parent = parent;
        
        MouseFilter = MouseFilterEnum.Pass; // Important for GuiInput
        
        bool isResearched = _sim.Research.IsResearched(tech.Id);
        bool isActive = _sim.Research.CurrentResearch?.Id == tech.Id;
        bool isAvailable = _sim.Research.CanResearch(tech);
        
        // Setup style based on state
        var style = new StyleBoxFlat
        {
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            ShadowSize = 4, ShadowOffset = new Vector2(2, 2)
        };

        if (isResearched)
        {
            // Civ3 Researched = Green/Beige with no restrictions
            style.BgColor = new Color(0.85f, 0.85f, 0.75f, 1.0f);
            style.BorderColor = new Color(0.3f, 0.5f, 0.3f, 1.0f);
        }
        else if (isActive)
        {
            // Active = Blue
            style.BgColor = new Color(0.4f, 0.6f, 0.8f, 1.0f);
            style.BorderColor = new Color(0.1f, 0.3f, 0.6f, 1.0f);
            
            // Add progress bar if active
            var progress = new ProgressBar();
            progress.MaxValue = tech.ScienceCost;
            progress.Value = _sim.Research.CurrentScienceProgress;
            progress.CustomMinimumSize = new Vector2(0, 6);
            progress.ShowPercentage = false;
            progress.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
            progress.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(0.2f, 0.8f, 0.2f) });
            AddChild(progress);
        }
        else if (isAvailable)
        {
            // Available = Light Beige
            style.BgColor = new Color(0.95f, 0.95f, 0.85f, 1.0f);
            style.BorderColor = new Color(0.6f, 0.5f, 0.3f, 1.0f);
        }
        else
        {
            // Locked = Grayed out with locked icon
            style.BgColor = new Color(0.7f, 0.7f, 0.65f, 0.8f);
            style.BorderColor = new Color(0.5f, 0.5f, 0.45f, 0.5f);
        }

        AddThemeStyleboxOverride("panel", style);

        // Content
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        margin.AddChild(hbox);
        AddChild(margin);
        
        // Icon
        var iconLabel = new Label { Text = tech.Icon };
        iconLabel.AddThemeFontSizeOverride("font_size", 24);
        iconLabel.VerticalAlignment = VerticalAlignment.Center;
        
        if (!isResearched && !isAvailable && !isActive)
        {
            // Overlay lock icon for unavailable techs
            var lockOverlay = new Label { Text = "🚫" };
            lockOverlay.AddThemeFontSizeOverride("font_size", 16);
            lockOverlay.SetAnchorsPreset(Control.LayoutPreset.TopRight);
            iconLabel.AddChild(lockOverlay);
        }
        
        hbox.AddChild(iconLabel);
        
        // Texts
        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        
        var nameLabel = new Label { Text = tech.Name };
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.AddThemeColorOverride("font_color", isActive ? Colors.White : new Color(0.1f, 0.1f, 0.1f));
        nameLabel.AddThemeFontOverride("font", ThemeDB.FallbackFont); // bold
        vbox.AddChild(nameLabel);
        
        var costLabel = new Label { Text = $"{tech.ScienceCost} Turns" }; // Simplified as turns for UI, even though it's raw science points
        costLabel.AddThemeFontSizeOverride("font_size", 11);
        costLabel.AddThemeColorOverride("font_color", isActive ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.4f, 0.4f, 0.4f));
        vbox.AddChild(costLabel);
        
        hbox.AddChild(vbox);
        
        // Interactions
        if (isAvailable)
        {
            GuiInput += (InputEvent @event) =>
            {
                if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                {
                    _parent.OnTechNodeClicked(tech);
                }
            };
            
            // Mouse hover effects
            MouseEntered += () => {
                var hoverStyle = (StyleBoxFlat)style.Duplicate();
                hoverStyle.BorderColor = new Color(0.8f, 0.6f, 0.1f);
                hoverStyle.BorderWidthLeft = 3; hoverStyle.BorderWidthTop = 3; hoverStyle.BorderWidthRight = 3; hoverStyle.BorderWidthBottom = 3;
                AddThemeStyleboxOverride("panel", hoverStyle);
            };
            
            MouseExited += () => {
                AddThemeStyleboxOverride("panel", style);
            };
            
            TooltipText = $"Click to research {tech.Name}\nCost: {tech.ScienceCost} Science";
        }
        else if (isResearched)
        {
            TooltipText = $"Already researched: {tech.Name}\n{tech.Description}";
        }
        else if (!isActive)
        {
            TooltipText = $"Requires: {string.Join(", ", tech.PrerequisiteIds.Select(id => _sim.Research.AllTechnologies.Find(t => t.Id == id)?.Name))}";
        }
    }
}
