using Godot;
using CivGame.Core;
using CivGame.Audio;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CivGame.UI;

/// <summary>
/// A professional espionage and intelligence screen.
/// Allows establishing embassies, planting spies, and executing covert missions.
/// </summary>
public partial class EspionageScreen : CanvasLayer
{
    private readonly GameSimulation _sim;
    private readonly EspionageSystem _espionage;
    private Faction _selectedTargetFaction = Faction.AiRival;
    private City? _selectedTargetCity = null;
    
    // UI elements
    private OptionButton _factionDropdown = null!;
    private OptionButton _cityDropdown = null!;
    private VBoxContainer _missionsList = null!;
    private Label _infoLabel = null!;
    private Label _treasuryLabel = null!;

    public event Action? OnClosed;

    public EspionageScreen(GameSimulation sim)
    {
        _sim = sim;
        _espionage = new EspionageSystem(sim);
        Layer = 110;

        // Overlay
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.7f) };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        // Main Panel
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(700, 500);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        var style = new StyleBoxFlat 
        { 
            BgColor = new Color(0.1f, 0.15f, 0.2f), // Dark blue/grey intel theme
            BorderWidthTop = 4, BorderWidthBottom = 4, BorderWidthLeft = 4, BorderWidthRight = 4,
            BorderColor = new Color(0.4f, 0.5f, 0.6f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 
        };
        panel.AddThemeStyleboxOverride("panel", style);
        overlay.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.AddThemeConstantOverride("margin_left", 25);
        margin.AddThemeConstantOverride("margin_right", 25);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 15);
        margin.AddChild(vbox);

        // Title Row
        var titleRow = new HBoxContainer();
        var title = new Label { Text = "I N T E L L I G E N C E   A G E N C Y", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        title.AddThemeFontSizeOverride("font_size", 22);
        titleRow.AddChild(title);

        _treasuryLabel = new Label { Text = $"Treasury: {_sim.PlayerTreasury} Gold" };
        _treasuryLabel.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.2f));
        titleRow.AddChild(_treasuryLabel);
        vbox.AddChild(titleRow);

        vbox.AddChild(new HSeparator());

        // Target Selection Row
        var targetRow = new HBoxContainer();
        targetRow.AddThemeConstantOverride("separation", 20);
        
        var factionLabel = new Label { Text = "Target Nation:" };
        targetRow.AddChild(factionLabel);
        
        _factionDropdown = new OptionButton { CustomMinimumSize = new Vector2(200, 0) };
        _factionDropdown.AddItem(_sim.AiCiv.LeaderName, (int)Faction.AiRival);
        _factionDropdown.Select(0);
        targetRow.AddChild(_factionDropdown);

        var cityLabel = new Label { Text = "Target City:" };
        targetRow.AddChild(cityLabel);

        _cityDropdown = new OptionButton { CustomMinimumSize = new Vector2(200, 0) };
        _cityDropdown.ItemSelected += (idx) => { PlayClick(); UpdateSelectedCity(); };
        targetRow.AddChild(_cityDropdown);
        
        vbox.AddChild(targetRow);

        _infoLabel = new Label { Text = "Select a mission to view details.", HorizontalAlignment = HorizontalAlignment.Center };
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        vbox.AddChild(_infoLabel);

        vbox.AddChild(new HSeparator());

        // Missions List
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _missionsList = new VBoxContainer();
        _missionsList.AddThemeConstantOverride("separation", 10);
        _missionsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_missionsList);
        vbox.AddChild(scroll);

        // Close Button
        var closeBtn = new Button { Text = "Close Agency", CustomMinimumSize = new Vector2(150, 40), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        closeBtn.Pressed += () => 
        { 
            AudioManager.Instance?.PlaySfx("res://assets/sounds/close.wav");
            OnClosed?.Invoke(); 
            QueueFree(); 
        };
        vbox.AddChild(closeBtn);

        RefreshCitiesDropdown();
        BuildMissionList();
    }

    private void PlayClick() => AudioManager.Instance?.PlaySfx("res://assets/sounds/click.wav");

    private void RefreshCitiesDropdown()
    {
        _cityDropdown.Clear();
        var enemyCities = _sim.Cities.Where(c => c.Faction == _selectedTargetFaction).ToList();
        
        if (enemyCities.Count == 0)
        {
            _cityDropdown.AddItem("No cities visible", -1);
            _cityDropdown.Disabled = true;
            _selectedTargetCity = null;
        }
        else
        {
            _cityDropdown.Disabled = false;
            foreach (var city in enemyCities)
            {
                _cityDropdown.AddItem(city.Name);
            }
            _selectedTargetCity = enemyCities[0];
        }
    }

    private void UpdateSelectedCity()
    {
        if (_cityDropdown.Disabled) return;
        var enemyCities = _sim.Cities.Where(c => c.Faction == _selectedTargetFaction).ToList();
        int idx = (int)_cityDropdown.GetSelectedId();
        if (idx >= 0 && idx < enemyCities.Count)
        {
            _selectedTargetCity = enemyCities[idx];
        }
        BuildMissionList(); // Refresh costs
    }

    private void BuildMissionList()
    {
        foreach (Node child in _missionsList.GetChildren())
        {
            child.QueueFree();
        }

        AddMissionRow("Establish Embassy", EspionageMission.EstablishEmbassy, "Allows diplomatic agreements and reveals their capital.");
        AddMissionRow("Plant Spy", EspionageMission.PlantSpy, "Infiltrate their intelligence network. Requires Intelligence Agency.");
        AddMissionRow("Steal Technology", EspionageMission.StealTechnology, "Attempt to steal a technological advance.");
        AddMissionRow("Steal World Map", EspionageMission.StealWorldMap, "Copy their latest territorial maps.");
        
        if (_selectedTargetCity != null)
        {
            AddMissionRow($"Investigate {_selectedTargetCity.Name}", EspionageMission.InvestigateCity, "Gain temporary vision and view city production.");
            AddMissionRow($"Sabotage {_selectedTargetCity.Name}", EspionageMission.SabotageProduction, "Destroy current production shields in the city.");
        }
    }

    private void AddMissionRow(string title, EspionageMission mission, string desc)
    {
        int cost = _espionage.GetMissionCost(mission, _selectedTargetFaction, _selectedTargetCity);
        bool canAfford = _sim.PlayerTreasury >= cost;

        bool isAlreadyDone = false;
        if (mission == EspionageMission.EstablishEmbassy && _sim.Diplomacy.HasEmbassy(Faction.Player, _selectedTargetFaction))
            isAlreadyDone = true;
        else if (mission == EspionageMission.PlantSpy && _sim.Diplomacy.HasSpy(Faction.Player, _selectedTargetFaction))
            isAlreadyDone = true;

        var hbox = new HBoxContainer();
        
        var btn = new Button { Text = title, CustomMinimumSize = new Vector2(250, 0) };
        btn.Disabled = !canAfford || isAlreadyDone;
        
        var costLabel = new Label { CustomMinimumSize = new Vector2(80, 0), HorizontalAlignment = HorizontalAlignment.Right };
        if (isAlreadyDone)
        {
            costLabel.Text = "Done";
            costLabel.AddThemeColorOverride("font_color", Colors.Gray);
        }
        else
        {
            costLabel.Text = $"{cost} Gold";
            if (!canAfford) costLabel.AddThemeColorOverride("font_color", Colors.Red);
            else costLabel.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.2f));
        }

        var descLabel = new Label { Text = desc, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        descLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        
        btn.Pressed += () => ExecuteMission(mission);
        btn.MouseEntered += () => { if (!btn.Disabled) AudioManager.Instance?.PlaySfx("res://assets/sounds/hover.wav"); };

        hbox.AddChild(btn);
        hbox.AddChild(costLabel);
        hbox.AddChild(descLabel);

        _missionsList.AddChild(hbox);
    }

    private void ExecuteMission(EspionageMission mission)
    {
        PlayClick();
        int cost = _espionage.GetMissionCost(mission, _selectedTargetFaction, _selectedTargetCity);
        
        if (_sim.PlayerTreasury < cost)
        {
            _infoLabel.Text = "Not enough gold!";
            _infoLabel.AddThemeColorOverride("font_color", Colors.Red);
            return;
        }

        bool success = _espionage.ExecuteMission(Faction.Player, _selectedTargetFaction, mission, _selectedTargetCity);
        
        _treasuryLabel.Text = $"Treasury: {_sim.PlayerTreasury} Gold";
        BuildMissionList(); // Refresh affordabilities

        if (success)
        {
            AudioManager.Instance?.PlaySfx("res://assets/sounds/spy_success.wav");
            _infoLabel.Text = $"Mission '{mission}' SUCCESSFUL!";
            _infoLabel.AddThemeColorOverride("font_color", Colors.LightGreen);
        }
        else
        {
            AudioManager.Instance?.PlaySfx("res://assets/sounds/spy_fail.wav");
            _infoLabel.Text = $"Mission '{mission}' FAILED! Our agents were caught.";
            _infoLabel.AddThemeColorOverride("font_color", Colors.Red);
        }
    }
}
