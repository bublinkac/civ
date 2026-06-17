using Godot;
using CivGame.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.UI.CityComponents;

public partial class CityProductionSelectionDialog : CanvasLayer
{
    private readonly City _city;
    private readonly GameSimulation _sim;
    private readonly int _baseProd;
    private readonly Action _onProjectChanged;
    private PanelContainer? _dialog;

    public CityProductionSelectionDialog(City city, GameSimulation sim, int baseProd, Action onProjectChanged)
    {
        _city = city;
        _sim = sim;
        _baseProd = baseProd;
        _onProjectChanged = onProjectChanged;

        // Render above city detail and other UI layers
        Layer = 100;

        // 1. Semi-transparent black modal overlay
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.45f);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        // 2. Main Parchment Dialog Panel
        var dialogStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.92f, 0.82f, 0.98f), // Rich parchment
            BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3,
            BorderColor = new Color(0.45f, 0.38f, 0.28f), // Bronze/Gold borders
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ShadowSize = 10,
            ShadowColor = new Color(0, 0, 0, 0.4f)
        };

        var dialog = new PanelContainer();
        dialog.AddThemeStyleboxOverride("panel", dialogStyle);
        dialog.CustomMinimumSize = new Vector2(480, 480);
        dialog.Size = new Vector2(480, 480);
        dialog.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);

        overlay.AddChild(dialog);
        _dialog = dialog;
        CallDeferred(nameof(ApplyDialogLayout));

        var dialogVBox = new VBoxContainer();
        dialogVBox.AddThemeConstantOverride("separation", 10);
        dialog.AddChild(dialogVBox);

        // Header margin
        var headerMargin = new MarginContainer();
        headerMargin.AddThemeConstantOverride("margin_top", 12);
        headerMargin.AddThemeConstantOverride("margin_left", 12);
        headerMargin.AddThemeConstantOverride("margin_right", 12);
        dialogVBox.AddChild(headerMargin);

        // Title HBox
        var titleHBox = new HBoxContainer();
        headerMargin.AddChild(titleHBox);

        var titleLabel = new Label { Text = "SELECT PRODUCTION" };
        titleLabel.AddThemeFontSizeOverride("font_size", 13);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.35f, 0.28f, 0.18f));
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleHBox.AddChild(titleLabel);

        // Close button 'X' at top right
        var closeBtn = new Button { Text = "×" };
        closeBtn.Flat = true;
        closeBtn.AddThemeFontSizeOverride("font_size", 16);
        closeBtn.AddThemeColorOverride("font_color", new Color(0.5f, 0.4f, 0.3f));
        closeBtn.Pressed += () => QueueFree();
        titleHBox.AddChild(closeBtn);

        dialogVBox.AddChild(new HSeparator());

        // Body area with columns: Left column for Units, Right column for Improvements & Wonders
        var bodyMargin = new MarginContainer();
        bodyMargin.AddThemeConstantOverride("margin_left", 12);
        bodyMargin.AddThemeConstantOverride("margin_right", 12);
        bodyMargin.AddThemeConstantOverride("margin_bottom", 12);
        bodyMargin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        dialogVBox.AddChild(bodyMargin);

        var columnsHBox = new HBoxContainer();
        columnsHBox.AddThemeConstantOverride("separation", 15);
        bodyMargin.AddChild(columnsHBox);

        // ══════════════════════════════════════════
        // COLUMN 1: UNITS
        // ══════════════════════════════════════════
        var unitsVBox = new VBoxContainer();
        unitsVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        columnsHBox.AddChild(unitsVBox);

        var unitsTitle = new Label { Text = "MILITARY & CIVILIAN UNITS" };
        unitsTitle.AddThemeFontSizeOverride("font_size", 10);
        unitsTitle.AddThemeColorOverride("font_color", new Color(0.12f, 0.45f, 0.75f));
        unitsTitle.HorizontalAlignment = HorizontalAlignment.Center;
        unitsVBox.AddChild(unitsTitle);
        unitsVBox.AddChild(new HSeparator());

        var unitsScroll = new ScrollContainer();
        unitsScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        unitsScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        unitsVBox.AddChild(unitsScroll);

        var unitsList = new VBoxContainer();
        unitsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        unitsList.AddThemeConstantOverride("separation", 6);
        unitsScroll.AddChild(unitsList);

        // ══════════════════════════════════════════
        // COLUMN 2: IMPROVEMENTS & WONDERS
        // ══════════════════════════════════════════
        var buildingsVBox = new VBoxContainer();
        buildingsVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        columnsHBox.AddChild(buildingsVBox);

        var buildingsTitle = new Label { Text = "IMPROVEMENTS & WONDERS" };
        buildingsTitle.AddThemeFontSizeOverride("font_size", 10);
        buildingsTitle.AddThemeColorOverride("font_color", new Color(0.65f, 0.45f, 0.05f));
        buildingsTitle.HorizontalAlignment = HorizontalAlignment.Center;
        buildingsVBox.AddChild(buildingsTitle);
        buildingsVBox.AddChild(new HSeparator());

        var buildingsScroll = new ScrollContainer();
        buildingsScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        buildingsScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        buildingsVBox.AddChild(buildingsScroll);

        var buildingsList = new VBoxContainer();
        buildingsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        buildingsList.AddThemeConstantOverride("separation", 6);
        buildingsScroll.AddChild(buildingsList);

        // Gather and populate available projects
        var available = GetAvailableProjects(_city, _sim);

        foreach (var proj in available)
        {
            var itemBtn = new Button();
            itemBtn.Flat = false;

            // Item Button Theme Styles: Civ3-parchment buttons
            var itemStyleNormal = new StyleBoxFlat
            {
                BgColor = new Color(0.92f, 0.88f, 0.78f, 0.8f),
                BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                BorderColor = new Color(0.6f, 0.52f, 0.4f, 0.4f),
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
            };
            var itemStyleHover = new StyleBoxFlat
            {
                BgColor = new Color(0.88f, 0.82f, 0.7f, 1f),
                BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                BorderColor = new Color(0.5f, 0.4f, 0.25f, 0.9f),
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
            };
            itemBtn.AddThemeStyleboxOverride("normal", itemStyleNormal);
            itemBtn.AddThemeStyleboxOverride("hover", itemStyleHover);
            itemBtn.AddThemeStyleboxOverride("pressed", itemStyleNormal);
            itemBtn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
            itemBtn.CustomMinimumSize = new Vector2(0, 36);
            itemBtn.MouseDefaultCursorShape = Control.CursorShape.PointingHand;

            var itemHBox = new HBoxContainer();
            itemHBox.AddThemeConstantOverride("separation", 6);
            itemHBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            itemHBox.MouseFilter = Control.MouseFilterEnum.Ignore;
            itemBtn.AddChild(itemHBox);

            // Small Icon
            var tex = CityProductionQueueComponent.GetProjectTexture(proj);
            if (tex != null)
            {
                var iconRect = new TextureRect
                {
                    Texture = tex,
                    CustomMinimumSize = new Vector2(28, 28),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                itemHBox.AddChild(iconRect);
            }
            else
            {
                var labelEmoji = new Label { Text = CityProductionQueueComponent.getProjectEmoji(proj) };
                labelEmoji.AddThemeFontSizeOverride("font_size", 14);
                labelEmoji.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
                labelEmoji.MouseFilter = Control.MouseFilterEnum.Ignore;
                itemHBox.AddChild(labelEmoji);
            }

            // Info VBox (Name, stats / Turns Remaining)
            var infoVBox = new VBoxContainer();
            infoVBox.AddThemeConstantOverride("separation", 0);
            infoVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            infoVBox.MouseFilter = Control.MouseFilterEnum.Ignore;
            itemHBox.AddChild(infoVBox);

            // Unit Stats (Civ3 Style)
            string unitStats = proj switch
            {
                ProductionProject.Explorer => " 0.0.2",
                ProductionProject.Settler => " 0.0.1",
                ProductionProject.Worker => " 0.0.1",
                ProductionProject.Warrior => " 1.1.1",
                ProductionProject.Archer => " 2(1).1.1",
                _ => ""
            };

            var nameLbl = new Label { Text = $"{proj}{unitStats}" };
            nameLbl.AddThemeFontSizeOverride("font_size", 10);
            nameLbl.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.12f));
            nameLbl.MouseFilter = Control.MouseFilterEnum.Ignore;
            infoVBox.AddChild(nameLbl);

            int pCost = _city.GetProjectCost(proj);
            int pTurns = _baseProd > 0 ? (int)Math.Ceiling((double)pCost / _baseProd) : 9999;
            string turnsText = pTurns == 9999 ? "Never" : $"{pTurns} turn{(pTurns == 1 ? "" : "s")}";

            var costLbl = new Label { Text = $"{pCost}🛡️ ({turnsText})" };
            costLbl.AddThemeFontSizeOverride("font_size", 8);
            costLbl.AddThemeColorOverride("font_color", new Color(0.4f, 0.35f, 0.25f));
            costLbl.MouseFilter = Control.MouseFilterEnum.Ignore;
            infoVBox.AddChild(costLbl);

            // Click Handler
            itemBtn.Pressed += () =>
            {
                bool isShiftHeld = Input.IsKeyPressed(Key.Shift);
                if (isShiftHeld)
                {
                    _city.ProductionQueue.Add(proj);
                    GD.Print($"[City Screen] Queued production project: {proj} for city {_city.Name}");
                }
                else
                {
                    _city.CurrentProject = proj;
                    _city.CurrentProductionProgress = 0;
                    _city.StoredProduction = 0;
                    GD.Print($"[City Screen] Selected new production project: {proj} for city {_city.Name}");
                }

                QueueFree(); // Close dialog
                _onProjectChanged?.Invoke(); // Refresh UI
            };

            // Categorize into Units or Buildings/Wonders lists
            bool isUnit = proj == ProductionProject.Explorer ||
                          proj == ProductionProject.Settler ||
                          proj == ProductionProject.Worker ||
                          proj == ProductionProject.Warrior ||
                          proj == ProductionProject.Archer;

            if (isUnit)
            {
                unitsList.AddChild(itemBtn);
            }
            else
            {
                buildingsList.AddChild(itemBtn);
            }
        }

        // Help footer
        var footerLbl = new Label { Text = "Tip: Hold 'Shift' to add to Queue." };
        footerLbl.AddThemeFontSizeOverride("font_size", 8);
        footerLbl.AddThemeColorOverride("font_color", new Color(0.45f, 0.4f, 0.3f));
        footerLbl.HorizontalAlignment = HorizontalAlignment.Center;
        dialogVBox.AddChild(footerLbl);
    }

    private void ApplyDialogLayout()
    {
        if (_dialog == null || !IsInstanceValid(_dialog)) return;

        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        if (viewportSize.X <= 0 || viewportSize.Y <= 0) return;

        const float rightMargin = 24f;
        const float topMargin = 70f;
        const float minMargin = 12f;

        // Ensure dialog fits small viewports
        float width = MathF.Min(480f, MathF.Max(320f, viewportSize.X - (minMargin * 2)));
        float height = MathF.Min(480f, MathF.Max(280f, viewportSize.Y - (minMargin * 2)));
        _dialog.Size = new Vector2(width, height);

        float x = viewportSize.X - _dialog.Size.X - rightMargin;
        float y = topMargin;

        // Clamp on-screen just in case
        x = MathF.Max(minMargin, MathF.Min(x, viewportSize.X - _dialog.Size.X - minMargin));
        y = MathF.Max(minMargin, MathF.Min(y, viewportSize.Y - _dialog.Size.Y - minMargin));

        _dialog.Position = new Vector2(x, y);
    }

    private List<ProductionProject> GetAvailableProjects(City city, GameSimulation sim)
    {
        var list = new List<ProductionProject>();

        // 1. Units are always available (or based on techs)
        list.Add(ProductionProject.Explorer);
        list.Add(ProductionProject.Settler);
        list.Add(ProductionProject.Worker);
        list.Add(ProductionProject.Warrior);

        if (sim.Research.IsResearched("bronze_working"))
        {
            list.Add(ProductionProject.Archer);
        }

        // 2. Add buildings/wonders from registry if unlocked and not yet built
        var allBuildings = new[]
        {
            (production: ProductionProject.Granary, buildingId: "granary"),
            (production: ProductionProject.Monument, buildingId: "monument"),
            (production: ProductionProject.Walls, buildingId: "walls"),
            (production: ProductionProject.Barracks, buildingId: "barracks"),
            (production: ProductionProject.Temple, buildingId: "temple"),
            (production: ProductionProject.Library, buildingId: "library"),
            (production: ProductionProject.Courthouse, buildingId: "courthouse"),
            (production: ProductionProject.Marketplace, buildingId: "marketplace"),
            (production: ProductionProject.Aqueduct, buildingId: "aqueduct"),
            (production: ProductionProject.Colosseum, buildingId: "colosseum"),
            (production: ProductionProject.Harbor, buildingId: "harbor"),
            (production: ProductionProject.Bank, buildingId: "bank"),
            (production: ProductionProject.Cathedral, buildingId: "cathedral"),
            (production: ProductionProject.University, buildingId: "university"),
            (production: ProductionProject.Hospital, buildingId: "hospital"),
            (production: ProductionProject.Factory, buildingId: "factory"),
            (production: ProductionProject.CoalPlant, buildingId: "coal_plant"),
            (production: ProductionProject.HydroPlant, buildingId: "hydro_plant"),
            (production: ProductionProject.SolarPlant, buildingId: "solar_plant"),
            (production: ProductionProject.NuclearPlant, buildingId: "nuclear_plant"),
            (production: ProductionProject.ManufacturingPlant, buildingId: "manufacturing_plant"),
            (production: ProductionProject.Airport, buildingId: "airport"),
            (production: ProductionProject.CivilDefense, buildingId: "civil_defense"),
            (production: ProductionProject.PoliceStation, buildingId: "police_station"),
            (production: ProductionProject.StockExchange, buildingId: "stock_exchange"),
            (production: ProductionProject.RecyclingCenter, buildingId: "recycling_center"),
            (production: ProductionProject.ResearchLab, buildingId: "research_lab"),
            (production: ProductionProject.SAMMissileBattery, buildingId: "sam_missile_battery"),
            (production: ProductionProject.OffshorePlatform, buildingId: "offshore_platform"),
            (production: ProductionProject.MassTransitSystem, buildingId: "mass_transit_system"),
            (production: ProductionProject.CommercialDock, buildingId: "commercial_dock"),
            (production: ProductionProject.CoastalFortress, buildingId: "coastal_fortress"),
            
            // Spaceship Parts
            (production: ProductionProject.SSCockpit, buildingId: "ss_cockpit"),
            (production: ProductionProject.SSDockingBay, buildingId: "ss_docking_bay"),
            (production: ProductionProject.SSEngine, buildingId: "ss_engine"),
            (production: ProductionProject.SSExteriorCasing, buildingId: "ss_exterior_casing"),
            (production: ProductionProject.SSFuelCells, buildingId: "ss_fuel_cells"),
            (production: ProductionProject.SSLifeSupportSystem, buildingId: "ss_life_support_system"),
            (production: ProductionProject.SSPlanetaryPartyLounge, buildingId: "ss_planetary_party_lounge"),
            (production: ProductionProject.SSStasisChamber, buildingId: "ss_stasis_chamber"),
            (production: ProductionProject.SSStorageSupply, buildingId: "ss_storage_supply"),
            (production: ProductionProject.SSThrusters, buildingId: "ss_thrusters"),
            (production: ProductionProject.Palace, buildingId: "palace"),

            // Wonders
            (production: ProductionProject.Pyramids, buildingId: "pyramids"),
            (production: ProductionProject.HangingGardens, buildingId: "hanging_gardens"),
            (production: ProductionProject.Colossus, buildingId: "colossus"),
            (production: ProductionProject.GreatWall, buildingId: "great_wall"),
            (production: ProductionProject.StatueOfZeus, buildingId: "statue_of_zeus"),
            (production: ProductionProject.Oracle, buildingId: "oracle"),
            (production: ProductionProject.KnightsHall, buildingId: "knights_hall"),
            (production: ProductionProject.SovereignBath, buildingId: "sovereign_bath"),
            (production: ProductionProject.LeonardoWorkshop, buildingId: "leonardo_workshop"),
            (production: ProductionProject.ShakespearesTheatre, buildingId: "shakespeares_theatre"),
            (production: ProductionProject.SunTzusWarAcademy, buildingId: "sun_tzu_war_academy"),
            (production: ProductionProject.CureForCancer, buildingId: "cure_for_cancer"),
            (production: ProductionProject.SistineChapel, buildingId: "sistine_chapel"),
            (production: ProductionProject.TajMahal, buildingId: "taj_mahal"),
            (production: ProductionProject.Astrolabe, buildingId: "astrolabe"),
            (production: ProductionProject.Hermitage, buildingId: "hermitage"),
            (production: ProductionProject.IronWorks, buildingId: "iron_works"),
            (production: ProductionProject.SmithsMansion, buildingId: "smith_mansion"),
            (production: ProductionProject.TrainStation, buildingId: "train_station"),
            (production: ProductionProject.UnitedNations, buildingId: "united_nations"),
            (production: ProductionProject.ApolloProgram, buildingId: "apollo_program"),
            (production: ProductionProject.ManhattanProject, buildingId: "manhattan_project"),
            (production: ProductionProject.Internet, buildingId: "internet"),
            (production: ProductionProject.LongevityVaccine, buildingId: "longevity_vaccine"),
            (production: ProductionProject.MarsColony, buildingId: "mars_colony"),
            (production: ProductionProject.WorldBank, buildingId: "world_bank"),
            (production: ProductionProject.SpaceStation, buildingId: "space_station"),

            // Small Wonders
            (production: ProductionProject.HeroicEpic, buildingId: "heroic_epic"),
            (production: ProductionProject.MilitaryAcademy, buildingId: "military_academy"),
            (production: ProductionProject.Pentagon, buildingId: "pentagon"),
            (production: ProductionProject.ForbiddenPalace, buildingId: "forbidden_palace"),
            (production: ProductionProject.WallStreet, buildingId: "wall_street"),
            (production: ProductionProject.IntelligenceAgency, buildingId: "intelligence_agency"),
            (production: ProductionProject.BattlefieldMedicine, buildingId: "battlefield_medicine"),
            (production: ProductionProject.SDIDefense, buildingId: "sdi_defense")
        };

        foreach (var item in allBuildings)
        {
            var building = BuildingRegistry.Get(item.buildingId);
            var wonder = WonderRegistry.Get(item.buildingId);

            if (building == null && wonder == null) continue;

            var targetBuilding = (Building?)building ?? wonder;

            // Check prerequisites
            if (targetBuilding!.RequiredTechId != null && !sim.Research.IsResearched(targetBuilding.RequiredTechId))
                continue;

            if (targetBuilding.RequiredResourceId != null && !sim.CityHasResourceAccess(city, targetBuilding.RequiredResourceId))
                continue;

            // For Wonders, check if claimed globally or by faction
            if (wonder != null && sim.IsWonderClaimedByFaction(city.Faction, item.buildingId))
                continue;

            // Check custom prerequisites for Small Wonders
            if (wonder != null && wonder.IsNationalWonder && !sim.AreSmallWonderPrerequisitesMet(city, item.buildingId))
                continue;

            // For spaceship parts, check if Apollo Program is built by this faction
            if (item.buildingId.StartsWith("ss_") && !sim.IsWonderClaimedByFaction(city.Faction, "apollo_program"))
                continue;

            // For regular buildings, check if city already has it
            if (building != null && city.Buildings.Any(b => b.Id == item.buildingId))
                continue;

            list.Add(item.production);
        }

        return list;
    }
}
