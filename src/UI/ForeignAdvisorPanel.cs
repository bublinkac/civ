using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CivGame.Core;
using CivGame.Core.Diplomacy;
using CivGame.Audio;

namespace CivGame.UI;

/// <summary>
/// Civ3 Foreign Advisor (F4): Displays diplomatic relations, attitudes, embassies,
/// active treaties/trade agreements, tech & resource intelligence, and direct diplomatic actions.
/// </summary>
public partial class ForeignAdvisorPanel : PanelContainer
{
    private readonly GameSimulation _sim;
    private readonly bool _embedded;
    private Faction _selectedRival;

    private VBoxContainer _rootContainer = null!;

    public ForeignAdvisorPanel(GameSimulation sim, bool embedded = false)
    {
        _sim = sim;
        _embedded = embedded;
        Name = "ForeignAdvisorPanel";

        var rivals = _sim.ActiveFactions.Where(f => f.IsAi()).ToList();
        _selectedRival = rivals.Count > 0 ? rivals[0] : Faction.AiRival;

        if (!embedded)
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Stop;
            var styleBox = new StyleBoxFlat
            {
                BgColor = new Color(0.9f, 0.88f, 0.8f, 0.96f),
                BorderWidthTop = 4, BorderWidthBottom = 4, BorderWidthLeft = 4, BorderWidthRight = 4,
                BorderColor = new Color(0.7f, 0.65f, 0.4f),
                CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
            };
            AddThemeStyleboxOverride("panel", styleBox);
        }
        else
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) });
        }

        _rootContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        AddChild(_rootContainer);

        BuildUI();
    }

    private void RebuildUI()
    {
        foreach (Node child in _rootContainer.GetChildren())
        {
            child.QueueFree();
        }
        BuildUI();
    }

    private void BuildUI()
    {
        var margin = new MarginContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        margin.AddThemeConstantOverride("margin_top", _embedded ? 8 : 16);
        margin.AddThemeConstantOverride("margin_bottom", _embedded ? 8 : 16);
        margin.AddThemeConstantOverride("margin_left", _embedded ? 12 : 24);
        margin.AddThemeConstantOverride("margin_right", _embedded ? 12 : 24);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 12);
        mainVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.SizeFlagsVertical = SizeFlags.ExpandFill;

        if (!_embedded)
        {
            var headerHBox = new HBoxContainer();
            var titleLabel = new Label
            {
                Text = "F O R E I G N   A D V I S O R",
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 26);
            titleLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));

            var closeBtn = new Button { Text = "\u2716", Flat = true, CustomMinimumSize = new Vector2(40, 40) };
            closeBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
            closeBtn.AddThemeFontSizeOverride("font_size", 22);
            closeBtn.Pressed += () => QueueFree();

            headerHBox.AddChild(titleLabel);
            headerHBox.AddChild(closeBtn);
            mainVBox.AddChild(headerHBox);
        }

        var activeRivals = _sim.ActiveFactions.Where(f => f.IsAi()).ToList();
        if (activeRivals.Count > 0 && !activeRivals.Contains(_selectedRival))
        {
            _selectedRival = activeRivals[0];
        }

        // --- ADVISOR BRIEFING ---
        mainVBox.AddChild(BuildAdvisorBriefing(activeRivals));
        mainVBox.AddChild(new ColorRect { CustomMinimumSize = new Vector2(0, 2), Color = new Color(0.55f, 0.45f, 0.3f) });

        // --- MAIN SPLIT CONTENT ---
        var contentSplit = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        contentSplit.AddThemeConstantOverride("separation", 14);

        // LEFT COLUMN: Map + Selected Rival Dossier
        var leftCol = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(360, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        leftCol.AddThemeConstantOverride("separation", 10);

        var foreignMap = new AdvisorMapPanel(_sim, new AdvisorMapOptions
        {
            MinSize = new Vector2(340, 175),
            ShowCities = true,
            ShowUnits = false,
            ShowTerritory = true,
            ShowCityNames = true,
            ShowFogOfWar = true,
            CityDotScale = 2.0f
        });
        leftCol.AddChild(foreignMap);

        if (activeRivals.Count > 0)
        {
            leftCol.AddChild(BuildRivalDossier(_selectedRival));
        }
        contentSplit.AddChild(leftCol);

        // RIGHT COLUMN: Known Civilizations Table + Active Agreements
        var rightCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        rightCol.AddThemeConstantOverride("separation", 10);

        rightCol.AddChild(BuildCivilizationsTable(activeRivals));
        rightCol.AddChild(BuildActiveTreatiesPanel());

        contentSplit.AddChild(rightCol);
        mainVBox.AddChild(contentSplit);

        if (!_embedded)
        {
            var footerCloseBtn = new Button
            {
                Text = "Close Advisor",
                CustomMinimumSize = new Vector2(150, 36),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd
            };
            var btnStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.7f, 0.2f, 0.2f, 1.0f),
                CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
                CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
            };
            footerCloseBtn.AddThemeStyleboxOverride("normal", btnStyle);
            footerCloseBtn.Pressed += () => QueueFree();
            mainVBox.AddChild(footerCloseBtn);
        }

        margin.AddChild(mainVBox);
        _rootContainer.AddChild(margin);
    }

    private HBoxContainer BuildAdvisorBriefing(List<Faction> activeRivals)
    {
        var advisorBox = new HBoxContainer();
        advisorBox.AddThemeConstantOverride("separation", 10);

        var advisorPortrait = new Label { Text = "🧑‍⚖️" };
        advisorPortrait.AddThemeFontSizeOverride("font_size", 42);

        var wars = activeRivals.Where(f => _sim.IsAtWar(Faction.Player, f)).ToList();
        int embassiesCount = activeRivals.Count(f => _sim.Diplomacy.HasEmbassy(Faction.Player, f));
        int dealsCount = _sim.Diplomacy.ActiveDeals.Count(d => d.FactionA == Faction.Player || d.FactionB == Faction.Player);

        string advisorMessage;
        if (wars.Count > 0)
        {
            var warCivNames = string.Join(", ", wars.Select(w => _sim.GetCivilization(w).Name));
            advisorMessage = $"We are at WAR with: {warCivNames}! Our diplomats stand ready to negotiate peace or rally allies.\n" +
                             $"Active Embassies: {embassiesCount}/{activeRivals.Count}  |  Active Agreements: {dealsCount}";
        }
        else if (activeRivals.Count == 0)
        {
            advisorMessage = "No rival civilizations remain in the world. Our supremacy is unchallenged.";
        }
        else
        {
            advisorMessage = $"We are at PEACE with all {activeRivals.Count} known civilizations. " +
                             $"Establish embassies (50g) to unlock Right of Passage, Mutual Protection Pacts, and deeper intelligence.\n" +
                             $"Active Embassies: {embassiesCount}/{activeRivals.Count}  |  Active Agreements: {dealsCount}";
        }

        var messagePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        messagePanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Colors.White,
            BorderColor = new Color(0.55f, 0.50f, 0.35f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        });

        var msgMargin = new MarginContainer();
        msgMargin.AddThemeConstantOverride("margin_left", 12);
        msgMargin.AddThemeConstantOverride("margin_right", 12);
        msgMargin.AddThemeConstantOverride("margin_top", 8);
        msgMargin.AddThemeConstantOverride("margin_bottom", 8);

        var msgLabel = new Label
        {
            Text = advisorMessage,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        msgLabel.AddThemeColorOverride("font_color", Colors.Black);
        msgLabel.AddThemeFontSizeOverride("font_size", 13);
        msgMargin.AddChild(msgLabel);
        messagePanel.AddChild(msgMargin);

        advisorBox.AddChild(advisorPortrait);
        advisorBox.AddChild(messagePanel);
        return advisorBox;
    }

    private PanelContainer BuildRivalDossier(Faction rival)
    {
        var panel = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.96f, 0.94f, 0.88f),
            BorderColor = new Color(0.55f, 0.45f, 0.25f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        });

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);

        var vbox = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddThemeConstantOverride("separation", 6);

        var civ = _sim.GetCivilization(rival);
        var attitude = _sim.Diplomacy.GetAttitude(Faction.Player, rival);
        bool isAtWar = _sim.IsAtWar(Faction.Player, rival);
        bool hasEmbassy = _sim.Diplomacy.HasEmbassy(Faction.Player, rival);
        bool hasSpy = _sim.Diplomacy.HasSpy(Faction.Player, rival);
        bool hasRop = _sim.Diplomacy.HasRightOfPassage(Faction.Player, rival);
        bool hasMpp = _sim.Diplomacy.HasMutualProtectionPact(Faction.Player, rival);
        bool hasAlly = _sim.Diplomacy.HasMilitaryAlliance(Faction.Player, rival);

        var header = new Label
        {
            Text = $"📋 DOSSIER: {civ.Name.ToUpper()} ({civ.LeaderName})",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        header.AddThemeFontSizeOverride("font_size", 15);
        header.AddThemeColorOverride("font_color", new Color(0.15f, 0.12f, 0.08f));
        vbox.AddChild(header);

        string govStr = hasEmbassy || hasSpy ? Government.Get(_sim.GetGovernment(rival)).Name : "Unknown (Need Embassy)";
        string goldStr = hasEmbassy || hasSpy ? $"{_sim.GetTreasury(rival)}g" : "?";
        string milStr = hasEmbassy || hasSpy ? $"{_sim.Units.Count(u => u.Faction == rival)} units" : "?";

        var badges = new List<string> { DiplomacyManager.GetAttitudeLabel(attitude) };
        if (hasEmbassy) badges.Add("🏛️ Embassy");
        if (hasSpy) badges.Add("👁️ Spy");
        if (hasRop) badges.Add("🚶 RoP");
        if (hasMpp) badges.Add("🛡️ MPP");
        if (hasAlly) badges.Add("⚔️ Alliance");

        var infoLbl = new Label
        {
            Text = $"{string.Join("  |  ", badges)}\n" +
                   $"Government: {govStr}  |  Treasury: {goldStr}  |  Military: {milStr}",
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        infoLbl.AddThemeFontSizeOverride("font_size", 12);
        infoLbl.AddThemeColorOverride("font_color", new Color(0.22f, 0.22f, 0.22f));
        vbox.AddChild(infoLbl);

        // Tech & Resource Intelligence
        var weCanOffer = _sim.Diplomacy.GetTradableTechs(Faction.Player, rival);
        var theyHave = _sim.Diplomacy.GetTradableTechs(rival, Faction.Player);
        var theirResources = _sim.Diplomacy.GetTradableResources(rival);

        string techSummary = $"🔬 We can offer ({weCanOffer.Count}): {(weCanOffer.Count > 0 ? string.Join(", ", weCanOffer.Take(4).Select(t => t.Name)) : "None")}\n" +
                             $"💡 They possess ({theyHave.Count}): {(theyHave.Count > 0 ? string.Join(", ", theyHave.Take(4).Select(t => t.Name)) : "None")}\n" +
                             $"📦 Their Resources: {(theirResources.Count > 0 ? string.Join(", ", theirResources.Select(r => ResourceRegistry.Get(r)?.Name ?? r)) : "None connected")}";

        var intelLbl = new Label
        {
            Text = techSummary,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        intelLbl.AddThemeFontSizeOverride("font_size", 11);
        intelLbl.AddThemeColorOverride("font_color", new Color(0.25f, 0.30f, 0.35f));
        vbox.AddChild(intelLbl);

        vbox.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

        // Quick Action Buttons (2 rows)
        var actionRow1 = new HBoxContainer();
        actionRow1.AddThemeConstantOverride("separation", 6);

        var tradeBtn = new Button { Text = "🤝 Negotiate", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tradeBtn.Pressed += () => OpenDiplomacy(rival);
        actionRow1.AddChild(tradeBtn);

        if (!hasEmbassy && !isAtWar)
        {
            bool canAfford = _sim.Diplomacy.CanEstablishEmbassy(Faction.Player, rival);
            var embassyBtn = new Button
            {
                Text = $"🏛️ Embassy ({DiplomacyManager.EmbassyCost}g)",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Disabled = !canAfford
            };
            embassyBtn.Pressed += () =>
            {
                AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
                if (_sim.Diplomacy.EstablishEmbassy(Faction.Player, rival))
                {
                    RebuildUI();
                }
            };
            actionRow1.AddChild(embassyBtn);
        }
        else
        {
            var spyBtn = new Button { Text = "🕵️ Espionage", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            spyBtn.Pressed += () =>
            {
                AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
                var esp = new EspionageScreen(_sim);
                AddChild(esp);
            };
            actionRow1.AddChild(spyBtn);
        }
        vbox.AddChild(actionRow1);

        var actionRow2 = new HBoxContainer();
        actionRow2.AddThemeConstantOverride("separation", 6);

        if (!isAtWar)
        {
            var mapBtn = new Button { Text = "🗺️ Map Trade", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            mapBtn.Pressed += () => OpenDiplomacy(rival, new TradeDeal
            {
                FactionA = Faction.Player,
                FactionB = rival,
                WorldMapA = true,
                WorldMapB = true
            }, "Shall we exchange our World Maps?");
            actionRow2.AddChild(mapBtn);

            if (hasEmbassy)
            {
                var pactBtn = new Button { Text = "🛡️ Pact / RoP", SizeFlagsHorizontal = SizeFlags.ExpandFill };
                pactBtn.Pressed += () => OpenDiplomacy(rival, new TradeDeal
                {
                    FactionA = Faction.Player,
                    FactionB = rival,
                    RightOfPassage = true
                }, "Let us discuss formal treaties between our nations.");
                actionRow2.AddChild(pactBtn);
            }

            var warBtn = new Button { Text = "⚔️ Declare War", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            warBtn.AddThemeColorOverride("font_color", new Color(1.0f, 0.45f, 0.45f));
            warBtn.Pressed += () =>
            {
                AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
                _sim.Diplomacy.DeclareWar(Faction.Player, rival);
                RebuildUI();
            };
            actionRow2.AddChild(warBtn);
        }
        else
        {
            var peaceBtn = new Button { Text = "🕊️ Propose Peace Treaty", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            peaceBtn.AddThemeColorOverride("font_color", new Color(0.55f, 0.95f, 0.65f));
            peaceBtn.Pressed += () => OpenDiplomacy(rival, new TradeDeal
            {
                FactionA = Faction.Player,
                FactionB = rival,
                PeaceTreaty = true
            }, "We are willing to discuss terms for ending this war.");
            actionRow2.AddChild(peaceBtn);
        }

        vbox.AddChild(actionRow2);
        margin.AddChild(vbox);
        panel.AddChild(margin);
        return panel;
    }

    private PanelContainer BuildCivilizationsTable(List<Faction> activeRivals)
    {
        var panel = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.93f, 0.86f),
            BorderColor = new Color(0.6f, 0.5f, 0.35f),
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        });

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);

        var tableVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        tableVBox.AddThemeConstantOverride("separation", 6);

        var title = new Label { Text = "Known Civilizations (click to inspect)", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeColorOverride("font_color", new Color(0.18f, 0.15f, 0.10f));
        title.AddThemeFontSizeOverride("font_size", 16);
        tableVBox.AddChild(title);

        tableVBox.AddChild(CreateTableRow("Civilization", "Leader", "Attitude", "Embassy", "Status", "Cities", "Mil", true));

        int playerCities = _sim.Cities.Count(c => c.Faction == Faction.Player);
        int playerUnits = _sim.Units.Count(u => u.Faction == Faction.Player);
        tableVBox.AddChild(CreateTableRow(_sim.PlayerCiv.Name, _sim.PlayerCiv.LeaderName, "—", "Home", "YOU", playerCities.ToString(), playerUnits.ToString(), false));

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var listVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        listVBox.AddThemeConstantOverride("separation", 4);

        foreach (var aiFaction in activeRivals)
        {
            var civ = _sim.GetCivilization(aiFaction);
            int aiCities = _sim.Cities.Count(c => c.Faction == aiFaction);
            bool isWar = _sim.IsAtWar(Faction.Player, aiFaction);
            bool hasEmbassy = _sim.Diplomacy.HasEmbassy(Faction.Player, aiFaction);
            bool hasSpy = _sim.Diplomacy.HasSpy(Faction.Player, aiFaction);
            string milText = (hasEmbassy || hasSpy) ? _sim.Units.Count(u => u.Faction == aiFaction).ToString() : "?";
            string attText = DiplomacyManager.GetAttitudeLabel(_sim.Diplomacy.GetAttitude(Faction.Player, aiFaction));
            string status = isWar ? "AT WAR" : "Peace";

            var rowPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            bool isSelected = aiFaction == _selectedRival;
            rowPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = isSelected
                    ? new Color(0.82f, 0.78f, 0.62f)
                    : (isWar ? new Color(0.95f, 0.82f, 0.82f) : new Color(0.90f, 0.87f, 0.78f)),
                BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
                BorderColor = isSelected ? new Color(0.45f, 0.35f, 0.10f) : new Color(0.75f, 0.70f, 0.58f),
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
            });

            var row = CreateTableRow(civ.Name, civ.LeaderName, attText, hasEmbassy ? "🏛️ Yes" : "No", status, aiCities.ToString(), milText, false);

            var localFaction = aiFaction;
            var selectBtn = new Button { Text = isSelected ? "Viewing" : "Inspect", CustomMinimumSize = new Vector2(72, 26) };
            selectBtn.Pressed += () =>
            {
                AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
                _selectedRival = localFaction;
                RebuildUI();
            };
            row.AddChild(selectBtn);

            var diploBtn = new Button { Text = "Talk", CustomMinimumSize = new Vector2(60, 26) };
            diploBtn.Pressed += () => OpenDiplomacy(localFaction);
            row.AddChild(diploBtn);

            rowPanel.AddChild(row);
            listVBox.AddChild(rowPanel);
        }

        scroll.AddChild(listVBox);
        tableVBox.AddChild(scroll);
        margin.AddChild(tableVBox);
        panel.AddChild(margin);
        return panel;
    }

    private PanelContainer BuildActiveTreatiesPanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 150),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.93f, 0.86f),
            BorderColor = new Color(0.6f, 0.5f, 0.35f),
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        });

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);

        var vbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddThemeConstantOverride("separation", 4);

        var title = new Label { Text = "Active Treaties & Trade Agreements", HorizontalAlignment = HorizontalAlignment.Left };
        title.AddThemeColorOverride("font_color", new Color(0.18f, 0.15f, 0.10f));
        title.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(title);

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 4);

        var playerDeals = _sim.Diplomacy.ActiveDeals
            .Where(d => d.FactionA == Faction.Player || d.FactionB == Faction.Player)
            .ToList();

        if (playerDeals.Count == 0)
        {
            var emptyLbl = new Label { Text = "No active multi-turn agreements or treaties." };
            emptyLbl.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.45f));
            emptyLbl.AddThemeFontSizeOverride("font_size", 12);
            list.AddChild(emptyLbl);
        }
        else
        {
            foreach (var deal in playerDeals)
            {
                var partner = deal.FactionA == Faction.Player ? deal.FactionB : deal.FactionA;
                var civ = _sim.GetCivilization(partner);

                var terms = new List<string>();
                if (deal.PeaceTreaty) terms.Add("🕊️ Peace");
                if (deal.RightOfPassage) terms.Add("🚶 Right of Passage");
                if (deal.MutualProtectionPact) terms.Add("🛡️ Mutual Protection");
                if (deal.MilitaryAlliance) terms.Add("⚔️ Alliance");
                if (deal.GoldPerTurnA > 0) terms.Add($"-{deal.GoldPerTurnA} GPT");
                if (deal.GoldPerTurnB > 0) terms.Add($"+{deal.GoldPerTurnB} GPT");
                if (deal.ResourcesA.Count > 0) terms.Add($"Export: {string.Join(", ", deal.ResourcesA)}");
                if (deal.ResourcesB.Count > 0) terms.Add($"Import: {string.Join(", ", deal.ResourcesB)}");

                var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                var lbl = new Label
                {
                    Text = $"[{civ.Name}] {string.Join(" | ", terms)} ({deal.TurnsRemaining} turns left)",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };
                lbl.AddThemeColorOverride("font_color", new Color(0.12f, 0.12f, 0.12f));
                lbl.AddThemeFontSizeOverride("font_size", 12);
                row.AddChild(lbl);

                var capturedDeal = deal;
                var cancelBtn = new Button { Text = "Cancel", CustomMinimumSize = new Vector2(64, 22) };
                cancelBtn.Pressed += () =>
                {
                    AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
                    _sim.Diplomacy.CancelDeal(capturedDeal);
                    RebuildUI();
                };
                row.AddChild(cancelBtn);
                list.AddChild(row);
            }
        }

        scroll.AddChild(list);
        vbox.AddChild(scroll);
        margin.AddChild(vbox);
        panel.AddChild(margin);
        return panel;
    }

    private void OpenDiplomacy(Faction rival, TradeDeal? initialDeal = null, string? initialMsg = null)
    {
        AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
        var diag = new DiplomacyDialog(_sim, rival, initialDeal, initialMsg);
        diag.OnDealExecuted += RebuildUI;
        AddChild(diag);
    }

    private static HBoxContainer CreateTableRow(string civ, string leader, string attitude, string embassy, string status, string cities, string military, bool isHeader = false)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        var civLabel = new Label { Text = civ, CustomMinimumSize = new Vector2(120, 26), HorizontalAlignment = HorizontalAlignment.Left, ClipText = true };
        var leaderLabel = new Label { Text = leader, CustomMinimumSize = new Vector2(100, 26), HorizontalAlignment = HorizontalAlignment.Left, ClipText = true };
        var attLabel = new Label { Text = attitude, CustomMinimumSize = new Vector2(95, 26), HorizontalAlignment = HorizontalAlignment.Center };
        var embLabel = new Label { Text = embassy, CustomMinimumSize = new Vector2(65, 26), HorizontalAlignment = HorizontalAlignment.Center };
        var statusLabel = new Label { Text = status, CustomMinimumSize = new Vector2(70, 26), HorizontalAlignment = HorizontalAlignment.Center };
        var citiesLabel = new Label { Text = cities, CustomMinimumSize = new Vector2(45, 26), HorizontalAlignment = HorizontalAlignment.Center };
        var milLabel = new Label { Text = military, CustomMinimumSize = new Vector2(45, 26), HorizontalAlignment = HorizontalAlignment.Center };

        int fontSize = isHeader ? 12 : 13;
        var color = isHeader ? new Color(0.35f, 0.32f, 0.28f) : new Color(0.1f, 0.1f, 0.1f);

        foreach (var label in new[] { civLabel, leaderLabel, attLabel, embLabel, statusLabel, citiesLabel, milLabel })
        {
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
        }

        if (status == "AT WAR") statusLabel.AddThemeColorOverride("font_color", Colors.Red);
        if (status == "YOU") statusLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.45f, 0.1f));

        row.AddChild(civLabel);
        row.AddChild(leaderLabel);
        row.AddChild(attLabel);
        row.AddChild(embLabel);
        row.AddChild(statusLabel);
        row.AddChild(citiesLabel);
        row.AddChild(milLabel);
        return row;
    }
}
