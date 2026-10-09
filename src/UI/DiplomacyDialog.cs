using Godot;
using CivGame.Core;
using CivGame.Core.Diplomacy;
using CivGame.Audio;
using System;
using System.Linq;

namespace CivGame.UI;

/// <summary>
/// A professional trading screen where the player and AI can propose deals.
/// Features interactive tables for Gold, Techs, and Treaties.
/// </summary>
public partial class DiplomacyDialog : CanvasLayer
{
    private readonly GameSimulation _sim;
    private readonly Faction _otherFaction;
    private TradeDeal _currentDeal;
    
    // UI elements
    private ItemList _playerOfferList = null!;
    private ItemList _aiOfferList = null!;
    private Label _advisorMsg = null!;

    public DiplomacyDialog(GameSimulation sim, Faction otherFaction)
    {
        _sim = sim;
        _otherFaction = otherFaction;
        _currentDeal = new TradeDeal { FactionA = Faction.Player, FactionB = otherFaction };
        
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
            BgColor = new Color(0.9f, 0.85f, 0.75f), 
            BorderWidthTop = 4, BorderWidthBottom = 4, BorderWidthLeft = 4, BorderWidthRight = 4,
            BorderColor = new Color(0.5f, 0.4f, 0.2f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 
        };
        panel.AddThemeStyleboxOverride("panel", style);
        overlay.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 15);
        panel.AddChild(vbox);
        
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 15);
        margin.AddThemeConstantOverride("margin_bottom", 15);
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddChild(vbox);
        panel.AddChild(margin);

        // Title
        string otherName = _otherFaction == Faction.AiRival ? _sim.AiCiv.LeaderName : "Unknown Leader";
        var title = new Label { Text = $"D I P L O M A C Y   W I T H   {otherName.ToUpper()}", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        title.AddThemeFontSizeOverride("font_size", 22);
        vbox.AddChild(title);

        _advisorMsg = new Label { Text = "What brings you to our court?", HorizontalAlignment = HorizontalAlignment.Center };
        _advisorMsg.AddThemeColorOverride("font_color", new Color(0.4f, 0.3f, 0.1f));
        vbox.AddChild(_advisorMsg);

        // Trade Table (Left vs Right)
        var hbox = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        hbox.AddThemeConstantOverride("separation", 20);
        vbox.AddChild(hbox);

        // === PLAYER SIDE ===
        var playerVBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var pTitle = new Label { Text = "Our Offers", HorizontalAlignment = HorizontalAlignment.Center };
        pTitle.AddThemeColorOverride("font_color", Colors.Black);
        playerVBox.AddChild(pTitle);

        _playerOfferList = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _playerOfferList.AddThemeColorOverride("font_color", Colors.Black);
        playerVBox.AddChild(_playerOfferList);

        var pBtnGold = new Button { Text = "Offer 50 Gold" };
        pBtnGold.Pressed += () => { PlayClick(); _currentDeal.LumpSumGoldA += 50; UpdateUI(); };
        playerVBox.AddChild(pBtnGold);

        if (_sim.IsAtWarWithAi)
        {
            var pBtnPeace = new Button { Text = "Offer Peace Treaty" };
            pBtnPeace.Pressed += () => { PlayClick(); _currentDeal.PeaceTreaty = true; UpdateUI(); };
            playerVBox.AddChild(pBtnPeace);
        }
        else
        {
            var pBtnMap = new Button { Text = "Offer World Map" };
            pBtnMap.Pressed += () => { PlayClick(); _currentDeal.WorldMapA = true; UpdateUI(); };
            playerVBox.AddChild(pBtnMap);
        }
        hbox.AddChild(playerVBox);

        // === CENTER (Propose) ===
        var centerVBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        centerVBox.AddThemeConstantOverride("separation", 20);
        
        var proposeBtn = new Button { Text = "Propose Deal", CustomMinimumSize = new Vector2(120, 40) };
        proposeBtn.AddThemeColorOverride("font_color", Colors.Yellow);
        proposeBtn.Pressed += ProposeDeal;
        centerVBox.AddChild(proposeBtn);

        var cancelBtn = new Button { Text = "Cancel", CustomMinimumSize = new Vector2(120, 40) };
        cancelBtn.Pressed += () => { AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/close.wav"); QueueFree(); };
        centerVBox.AddChild(cancelBtn);
        hbox.AddChild(centerVBox);

        // === AI SIDE ===
        var aiVBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var aTitle = new Label { Text = "We Ask For", HorizontalAlignment = HorizontalAlignment.Center };
        aTitle.AddThemeColorOverride("font_color", Colors.Black);
        aiVBox.AddChild(aTitle);

        _aiOfferList = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _aiOfferList.AddThemeColorOverride("font_color", Colors.Black);
        aiVBox.AddChild(_aiOfferList);

        var aBtnGold = new Button { Text = "Ask for 50 Gold" };
        aBtnGold.Pressed += () => { PlayClick(); _currentDeal.LumpSumGoldB += 50; UpdateUI(); };
        aiVBox.AddChild(aBtnGold);

        if (_sim.IsAtWarWithAi)
        {
            var aBtnPeace = new Button { Text = "Ask for Peace Treaty" };
            aBtnPeace.Pressed += () => { PlayClick(); _currentDeal.PeaceTreaty = true; UpdateUI(); };
            aiVBox.AddChild(aBtnPeace);
        }
        else
        {
            var aBtnMap = new Button { Text = "Ask for World Map" };
            aBtnMap.Pressed += () => { PlayClick(); _currentDeal.WorldMapB = true; UpdateUI(); };
            aiVBox.AddChild(aBtnMap);
        }
        hbox.AddChild(aiVBox);

        UpdateUI();
    }

    private void PlayClick() => AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");

    private void UpdateUI()
    {
        _playerOfferList.Clear();
        if (_currentDeal.LumpSumGoldA > 0) _playerOfferList.AddItem($"{_currentDeal.LumpSumGoldA} Gold");
        if (_currentDeal.PeaceTreaty) _playerOfferList.AddItem("Peace Treaty");
        if (_currentDeal.WorldMapA) _playerOfferList.AddItem("World Map");

        _aiOfferList.Clear();
        if (_currentDeal.LumpSumGoldB > 0) _aiOfferList.AddItem($"{_currentDeal.LumpSumGoldB} Gold");
        if (_currentDeal.PeaceTreaty) _aiOfferList.AddItem("Peace Treaty");
        if (_currentDeal.WorldMapB) _aiOfferList.AddItem("World Map");
    }

    private void ProposeDeal()
    {
        PlayClick();
        
        // Very basic AI evaluation logic
        int playerValue = _currentDeal.LumpSumGoldA + (_currentDeal.WorldMapA ? 50 : 0);
        int aiValue = _currentDeal.LumpSumGoldB + (_currentDeal.WorldMapB ? 50 : 0);
        
        if (_sim.IsAtWarWithAi && _currentDeal.PeaceTreaty)
        {
            // If AI is losing, they might pay for peace. 
            // For now, let's just make them accept any peace deal if it's equal or favors them.
        }

        if (playerValue >= aiValue)
        {
            _advisorMsg.Text = "We accept your generous offer.";
            _advisorMsg.AddThemeColorOverride("font_color", Colors.DarkGreen);
            _sim.Diplomacy.ExecuteDeal(_currentDeal);
            
            // Close after 1.5 seconds
            var timer = GetTree().CreateTimer(1.5f);
            timer.Timeout += () => QueueFree();
        }
        else
        {
            _advisorMsg.Text = "This deal is an insult to our empire!";
            _advisorMsg.AddThemeColorOverride("font_color", Colors.DarkRed);
        }
    }
}
