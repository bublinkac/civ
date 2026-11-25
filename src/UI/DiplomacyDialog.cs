using Godot;
using CivGame.Core;
using CivGame.Core.Diplomacy;
using CivGame.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.UI;

/// <summary>
/// Full-featured diplomatic negotiation screen for trading Gold, GPT, Technologies,
/// Resources, World Maps, Peace Treaties, Right of Passage, Mutual Protection Pacts, and Alliances.
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
    private OptionButton _playerTechDropdown = null!;
    private OptionButton _aiTechDropdown = null!;
    private OptionButton _playerResDropdown = null!;
    private OptionButton _aiResDropdown = null!;

    private List<Technology> _playerTradableTechs = new();
    private List<Technology> _aiTradableTechs = new();
    private List<string> _playerTradableRes = new();
    private List<string> _aiTradableRes = new();

    public event Action? OnDealExecuted;

    public DiplomacyDialog(GameSimulation sim, Faction otherFaction, TradeDeal? initialDeal = null, string? initialMessage = null)
    {
        _sim = sim;
        _otherFaction = otherFaction;
        _currentDeal = initialDeal ?? new TradeDeal { FactionA = Faction.Player, FactionB = otherFaction };

        Layer = 110;

        // Overlay
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.72f) };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        // Main Panel
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(920, 600);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.92f, 0.87f, 0.76f),
            BorderWidthTop = 4, BorderWidthBottom = 4, BorderWidthLeft = 4, BorderWidthRight = 4,
            BorderColor = new Color(0.55f, 0.42f, 0.20f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
        };
        panel.AddThemeStyleboxOverride("panel", style);
        overlay.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(vbox);

        // Title & Attitude Header
        var otherCiv = _sim.GetCivilization(_otherFaction);
        var attitude = _sim.Diplomacy.GetAttitude(Faction.Player, _otherFaction);
        string attitudeText = DiplomacyManager.GetAttitudeLabel(attitude);
        bool isAtWar = _sim.IsAtWar(Faction.Player, _otherFaction);
        bool hasEmbassy = _sim.Diplomacy.HasEmbassy(Faction.Player, _otherFaction) || _sim.Diplomacy.HasEmbassy(_otherFaction, Faction.Player);

        var title = new Label
        {
            Text = $"DIPLOMACY WITH {otherCiv.LeaderName.ToUpper()} ({otherCiv.Name.ToUpper()})  —  {attitudeText}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeColorOverride("font_color", new Color(0.12f, 0.10f, 0.08f));
        title.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(title);

        _advisorMsg = new Label
        {
            Text = initialMessage ?? (isAtWar
                ? "Our nations are at war. What terms do you propose for peace?"
                : $"Greetings, {_sim.PlayerCiv.LeaderName}. What brings you to our court?"),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        _advisorMsg.AddThemeColorOverride("font_color", isAtWar ? new Color(0.7f, 0.1f, 0.1f) : new Color(0.35f, 0.25f, 0.10f));
        _advisorMsg.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(_advisorMsg);

        // Trade Table (Left = Player, Center = Treaties/Actions, Right = AI)
        var hbox = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        hbox.AddThemeConstantOverride("separation", 14);
        vbox.AddChild(hbox);

        // === PLAYER SIDE ===
        var playerVBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        playerVBox.AddThemeConstantOverride("separation", 6);
        var pTitle = new Label
        {
            Text = $"Our Offers (Treasury: {_sim.GetTreasury(Faction.Player)}g)",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        pTitle.AddThemeColorOverride("font_color", Colors.Black);
        pTitle.AddThemeFontSizeOverride("font_size", 14);
        playerVBox.AddChild(pTitle);

        _playerOfferList = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 160) };
        _playerOfferList.AddThemeColorOverride("font_color", Colors.Black);
        playerVBox.AddChild(_playerOfferList);

        // Player Gold buttons
        var pGoldRow = new HBoxContainer();
        pGoldRow.AddThemeConstantOverride("separation", 6);
        var pBtnGold50 = new Button { Text = "+50g", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        pBtnGold50.Pressed += () => { PlayClick(); _currentDeal.LumpSumGoldA += 50; UpdateUI(); };
        var pBtnGold200 = new Button { Text = "+200g", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        pBtnGold200.Pressed += () => { PlayClick(); _currentDeal.LumpSumGoldA += 200; UpdateUI(); };
        var pBtnGpt5 = new Button { Text = "+5 GPT", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        pBtnGpt5.Pressed += () => { PlayClick(); _currentDeal.GoldPerTurnA += 5; UpdateUI(); };
        pGoldRow.AddChild(pBtnGold50);
        pGoldRow.AddChild(pBtnGold200);
        pGoldRow.AddChild(pBtnGpt5);
        playerVBox.AddChild(pGoldRow);

        // Player Tech offer
        _playerTradableTechs = _sim.Diplomacy.GetTradableTechs(Faction.Player, _otherFaction);
        var pTechRow = new HBoxContainer();
        pTechRow.AddThemeConstantOverride("separation", 6);
        _playerTechDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        if (_playerTradableTechs.Count == 0)
        {
            _playerTechDropdown.AddItem("(No tradable techs)");
            _playerTechDropdown.Disabled = true;
        }
        else
        {
            foreach (var t in _playerTradableTechs) _playerTechDropdown.AddItem($"{t.Name} ({t.ScienceCost * 3}v)");
        }
        var pAddTechBtn = new Button { Text = "+Tech", CustomMinimumSize = new Vector2(70, 0), Disabled = _playerTradableTechs.Count == 0 };
        pAddTechBtn.Pressed += () =>
        {
            PlayClick();
            int idx = _playerTechDropdown.Selected;
            if (idx >= 0 && idx < _playerTradableTechs.Count)
            {
                string tid = _playerTradableTechs[idx].Id;
                if (!_currentDeal.TechsA.Contains(tid)) _currentDeal.TechsA.Add(tid);
                UpdateUI();
            }
        };
        pTechRow.AddChild(_playerTechDropdown);
        pTechRow.AddChild(pAddTechBtn);
        playerVBox.AddChild(pTechRow);

        // Player Resource offer
        _playerTradableRes = _sim.Diplomacy.GetTradableResources(Faction.Player);
        var pResRow = new HBoxContainer();
        pResRow.AddThemeConstantOverride("separation", 6);
        _playerResDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        if (_playerTradableRes.Count == 0)
        {
            _playerResDropdown.AddItem("(No connected resources)");
            _playerResDropdown.Disabled = true;
        }
        else
        {
            foreach (var rId in _playerTradableRes)
            {
                var r = ResourceRegistry.Get(rId);
                _playerResDropdown.AddItem(r?.Name ?? rId);
            }
        }
        var pAddResBtn = new Button { Text = "+Res", CustomMinimumSize = new Vector2(70, 0), Disabled = _playerTradableRes.Count == 0 };
        pAddResBtn.Pressed += () =>
        {
            PlayClick();
            int idx = _playerResDropdown.Selected;
            if (idx >= 0 && idx < _playerTradableRes.Count)
            {
                string rid = _playerTradableRes[idx];
                if (!_currentDeal.ResourcesA.Contains(rid)) _currentDeal.ResourcesA.Add(rid);
                UpdateUI();
            }
        };
        pResRow.AddChild(_playerResDropdown);
        pResRow.AddChild(pAddResBtn);
        playerVBox.AddChild(pResRow);

        if (!isAtWar)
        {
            var pBtnMap = new Button { Text = "Offer World Map" };
            pBtnMap.Pressed += () => { PlayClick(); _currentDeal.WorldMapA = !_currentDeal.WorldMapA; UpdateUI(); };
            playerVBox.AddChild(pBtnMap);
        }

        hbox.AddChild(playerVBox);

        // === CENTER (Treaties & Actions) ===
        var centerVBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, CustomMinimumSize = new Vector2(210, 0) };
        centerVBox.AddThemeConstantOverride("separation", 8);

        var treatiesTitle = new Label { Text = "Treaties & Pacts", HorizontalAlignment = HorizontalAlignment.Center };
        treatiesTitle.AddThemeColorOverride("font_color", new Color(0.2f, 0.15f, 0.05f));
        treatiesTitle.AddThemeFontSizeOverride("font_size", 14);
        centerVBox.AddChild(treatiesTitle);

        if (isAtWar)
        {
            var peaceBtn = new Button { Text = "🕊️ Peace Treaty", CustomMinimumSize = new Vector2(190, 34) };
            peaceBtn.Pressed += () => { PlayClick(); _currentDeal.PeaceTreaty = !_currentDeal.PeaceTreaty; UpdateUI(); };
            centerVBox.AddChild(peaceBtn);
        }
        else
        {
            var ropBtn = new Button
            {
                Text = "🚶 Right of Passage",
                CustomMinimumSize = new Vector2(190, 32),
                Disabled = !hasEmbassy,
                TooltipText = hasEmbassy ? "Allow mutual unit movement across borders (20 turns)" : "Requires an Embassy first"
            };
            ropBtn.Pressed += () => { PlayClick(); _currentDeal.RightOfPassage = !_currentDeal.RightOfPassage; UpdateUI(); };
            centerVBox.AddChild(ropBtn);

            var mppBtn = new Button
            {
                Text = "🛡️ Mutual Protection Pact",
                CustomMinimumSize = new Vector2(190, 32),
                Disabled = !hasEmbassy,
                TooltipText = hasEmbassy ? "Auto-declare war if either partner is attacked (20 turns)" : "Requires an Embassy first"
            };
            mppBtn.Pressed += () => { PlayClick(); _currentDeal.MutualProtectionPact = !_currentDeal.MutualProtectionPact; UpdateUI(); };
            centerVBox.AddChild(mppBtn);

            var allyBtn = new Button
            {
                Text = "⚔️ Military Alliance",
                CustomMinimumSize = new Vector2(190, 32),
                Disabled = !hasEmbassy,
                TooltipText = hasEmbassy ? "Formal military alliance (20 turns)" : "Requires an Embassy first"
            };
            allyBtn.Pressed += () => { PlayClick(); _currentDeal.MilitaryAlliance = !_currentDeal.MilitaryAlliance; UpdateUI(); };
            centerVBox.AddChild(allyBtn);
        }

        centerVBox.AddChild(new HSeparator());

        var proposeBtn = new Button { Text = "🤝 Propose Deal", CustomMinimumSize = new Vector2(190, 40) };
        proposeBtn.AddThemeColorOverride("font_color", Colors.Yellow);
        proposeBtn.Pressed += ProposeDeal;
        centerVBox.AddChild(proposeBtn);

        var clearBtn = new Button { Text = "🗑️ Clear Table", CustomMinimumSize = new Vector2(190, 32) };
        clearBtn.Pressed += () =>
        {
            PlayClick();
            _currentDeal = new TradeDeal { FactionA = Faction.Player, FactionB = _otherFaction };
            UpdateUI();
        };
        centerVBox.AddChild(clearBtn);

        var cancelBtn = new Button { Text = "✕ Close", CustomMinimumSize = new Vector2(190, 36) };
        cancelBtn.Pressed += () => { AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/close.wav"); QueueFree(); };
        centerVBox.AddChild(cancelBtn);

        hbox.AddChild(centerVBox);

        // === AI SIDE ===
        var aiVBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        aiVBox.AddThemeConstantOverride("separation", 6);
        var aTitle = new Label
        {
            Text = $"We Ask For (Treasury: {_sim.GetTreasury(_otherFaction)}g)",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        aTitle.AddThemeColorOverride("font_color", Colors.Black);
        aTitle.AddThemeFontSizeOverride("font_size", 14);
        aiVBox.AddChild(aTitle);

        _aiOfferList = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 160) };
        _aiOfferList.AddThemeColorOverride("font_color", Colors.Black);
        aiVBox.AddChild(_aiOfferList);

        // AI Gold buttons
        var aGoldRow = new HBoxContainer();
        aGoldRow.AddThemeConstantOverride("separation", 6);
        var aBtnGold50 = new Button { Text = "+50g", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        aBtnGold50.Pressed += () => { PlayClick(); _currentDeal.LumpSumGoldB += 50; UpdateUI(); };
        var aBtnGold200 = new Button { Text = "+200g", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        aBtnGold200.Pressed += () => { PlayClick(); _currentDeal.LumpSumGoldB += 200; UpdateUI(); };
        var aBtnGpt5 = new Button { Text = "+5 GPT", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        aBtnGpt5.Pressed += () => { PlayClick(); _currentDeal.GoldPerTurnB += 5; UpdateUI(); };
        aGoldRow.AddChild(aBtnGold50);
        aGoldRow.AddChild(aBtnGold200);
        aGoldRow.AddChild(aBtnGpt5);
        aiVBox.AddChild(aGoldRow);

        // AI Tech ask
        _aiTradableTechs = _sim.Diplomacy.GetTradableTechs(_otherFaction, Faction.Player);
        var aTechRow = new HBoxContainer();
        aTechRow.AddThemeConstantOverride("separation", 6);
        _aiTechDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        if (_aiTradableTechs.Count == 0)
        {
            _aiTechDropdown.AddItem("(No tradable techs)");
            _aiTechDropdown.Disabled = true;
        }
        else
        {
            foreach (var t in _aiTradableTechs) _aiTechDropdown.AddItem($"{t.Name} ({t.ScienceCost * 3}v)");
        }
        var aAddTechBtn = new Button { Text = "+Tech", CustomMinimumSize = new Vector2(70, 0), Disabled = _aiTradableTechs.Count == 0 };
        aAddTechBtn.Pressed += () =>
        {
            PlayClick();
            int idx = _aiTechDropdown.Selected;
            if (idx >= 0 && idx < _aiTradableTechs.Count)
            {
                string tid = _aiTradableTechs[idx].Id;
                if (!_currentDeal.TechsB.Contains(tid)) _currentDeal.TechsB.Add(tid);
                UpdateUI();
            }
        };
        aTechRow.AddChild(_aiTechDropdown);
        aTechRow.AddChild(aAddTechBtn);
        aiVBox.AddChild(aTechRow);

        // AI Resource ask
        _aiTradableRes = _sim.Diplomacy.GetTradableResources(_otherFaction);
        var aResRow = new HBoxContainer();
        aResRow.AddThemeConstantOverride("separation", 6);
        _aiResDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        if (_aiTradableRes.Count == 0)
        {
            _aiResDropdown.AddItem("(No connected resources)");
            _aiResDropdown.Disabled = true;
        }
        else
        {
            foreach (var rId in _aiTradableRes)
            {
                var r = ResourceRegistry.Get(rId);
                _aiResDropdown.AddItem(r?.Name ?? rId);
            }
        }
        var aAddResBtn = new Button { Text = "+Res", CustomMinimumSize = new Vector2(70, 0), Disabled = _aiTradableRes.Count == 0 };
        aAddResBtn.Pressed += () =>
        {
            PlayClick();
            int idx = _aiResDropdown.Selected;
            if (idx >= 0 && idx < _aiTradableRes.Count)
            {
                string rid = _aiTradableRes[idx];
                if (!_currentDeal.ResourcesB.Contains(rid)) _currentDeal.ResourcesB.Add(rid);
                UpdateUI();
            }
        };
        aResRow.AddChild(_aiResDropdown);
        aResRow.AddChild(aAddResBtn);
        aiVBox.AddChild(aResRow);

        if (!isAtWar)
        {
            var aBtnMap = new Button { Text = "Ask for World Map" };
            aBtnMap.Pressed += () => { PlayClick(); _currentDeal.WorldMapB = !_currentDeal.WorldMapB; UpdateUI(); };
            aiVBox.AddChild(aBtnMap);
        }

        hbox.AddChild(aiVBox);

        UpdateUI();
    }

    private void PlayClick() => AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");

    private void UpdateUI()
    {
        _playerOfferList.Clear();
        if (_currentDeal.LumpSumGoldA > 0) _playerOfferList.AddItem($"💰 {_currentDeal.LumpSumGoldA} Gold");
        if (_currentDeal.GoldPerTurnA > 0) _playerOfferList.AddItem($"🪙 {_currentDeal.GoldPerTurnA} Gold / Turn (20t)");
        if (_currentDeal.PeaceTreaty) _playerOfferList.AddItem("🕊️ Peace Treaty");
        if (_currentDeal.RightOfPassage) _playerOfferList.AddItem("🚶 Right of Passage (20t)");
        if (_currentDeal.MutualProtectionPact) _playerOfferList.AddItem("🛡️ Mutual Protection Pact (20t)");
        if (_currentDeal.MilitaryAlliance) _playerOfferList.AddItem("⚔️ Military Alliance (20t)");
        if (_currentDeal.WorldMapA) _playerOfferList.AddItem("🗺️ World Map");
        foreach (var t in _currentDeal.TechsA)
        {
            var tech = _sim.Research.AllTechnologies.Find(x => x.Id == t);
            _playerOfferList.AddItem($"🔬 Tech: {tech?.Name ?? t}");
        }
        foreach (var r in _currentDeal.ResourcesA)
        {
            var res = ResourceRegistry.Get(r);
            _playerOfferList.AddItem($"📦 Resource: {res?.Name ?? r} (20t)");
        }

        _aiOfferList.Clear();
        if (_currentDeal.LumpSumGoldB > 0) _aiOfferList.AddItem($"💰 {_currentDeal.LumpSumGoldB} Gold");
        if (_currentDeal.GoldPerTurnB > 0) _aiOfferList.AddItem($"🪙 {_currentDeal.GoldPerTurnB} Gold / Turn (20t)");
        if (_currentDeal.PeaceTreaty) _aiOfferList.AddItem("🕊️ Peace Treaty");
        if (_currentDeal.RightOfPassage) _aiOfferList.AddItem("🚶 Right of Passage (20t)");
        if (_currentDeal.MutualProtectionPact) _aiOfferList.AddItem("🛡️ Mutual Protection Pact (20t)");
        if (_currentDeal.MilitaryAlliance) _aiOfferList.AddItem("⚔️ Military Alliance (20t)");
        if (_currentDeal.WorldMapB) _aiOfferList.AddItem("🗺️ World Map");
        foreach (var t in _currentDeal.TechsB)
        {
            var tech = _sim.Research.AllTechnologies.Find(x => x.Id == t);
            _aiOfferList.AddItem($"🔬 Tech: {tech?.Name ?? t}");
        }
        foreach (var r in _currentDeal.ResourcesB)
        {
            var res = ResourceRegistry.Get(r);
            _aiOfferList.AddItem($"📦 Resource: {res?.Name ?? r} (20t)");
        }
    }

    private void ProposeDeal()
    {
        PlayClick();

        var (accepted, msg) = _sim.Diplomacy.EvaluateDeal(_currentDeal);
        _advisorMsg.Text = msg;

        if (accepted)
        {
            _advisorMsg.AddThemeColorOverride("font_color", new Color(0.05f, 0.45f, 0.12f));
            _sim.Diplomacy.ExecuteDeal(_currentDeal);
            OnDealExecuted?.Invoke();

            var timer = GetTree().CreateTimer(1.2f);
            timer.Timeout += () => { if (IsInstanceValid(this)) QueueFree(); };
        }
        else
        {
            _advisorMsg.AddThemeColorOverride("font_color", new Color(0.65f, 0.08f, 0.08f));
        }
    }
}
