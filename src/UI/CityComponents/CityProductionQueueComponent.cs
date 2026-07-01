using Godot;
using CivGame.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TileData = CivGame.Core.TileData;

namespace CivGame.UI.CityComponents;

public partial class CityProductionQueueComponent : PanelContainer
{
    private Action? _onProjectChanged;
    private int _baseProd;

    public CityProductionQueueComponent(City city, GameSimulation sim, Action? onProjectChanged = null)
    {
        _onProjectChanged = onProjectChanged;

        // Parchment styled box
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.92f, 0.82f, 0.95f), // Parchment
            BorderColor = new Color(0.5f, 0.4f, 0.3f),
            CornerRadiusBottomRight = 10
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(490, 280);
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var hsplit = new HBoxContainer();
        hsplit.AddThemeConstantOverride("separation", 15);
        margin.AddChild(hsplit);

        // ==========================================
        // LEFT COLUMN: Yield Bars (Food, Prod, Comm, Granary)
        // ==========================================
        var leftCol = new VBoxContainer();
        leftCol.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftCol.AddThemeConstantOverride("separation", 8);
        hsplit.AddChild(leftCol);

        // Gather base yields (similar to GameSimulation.CollectCityYields)
        int baseFood = 0, baseProd = 0, baseComm = 0;

        // City Center
        var centerTile = sim.Map.GetTile(city.X, city.Y);
        if (centerTile != null)
        {
            // Civ3: city center guarantees minimum 1 food, 1 shield, 1 commerce
            baseFood += Math.Max(1, centerTile.TotalYield.Food);
            baseProd += Math.Max(1, centerTile.TotalYield.Production);
            baseComm += Math.Max(1, centerTile.TotalYield.Commerce);
        }

        // Surrounding worked tiles (match simulation logic: manual + auto-fill to population)
        var workingTiles = new List<TileData>();

        if (city.WorkedTiles.Count > 0)
        {
            foreach (var tilePos in city.WorkedTiles)
            {
                var t = sim.Map.GetTile(tilePos.X, tilePos.Y);
                if (t != null && t.OwnerCityId == city.Id)
                {
                    workingTiles.Add(t);
                }
            }
        }

        if (workingTiles.Count < city.Population)
        {
            var potentialTiles = new List<TileData>();
            int radius = 1;
            var used = new HashSet<(int X, int Y)>(city.WorkedTiles.Select(w => (w.X, w.Y)));

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int tx = city.X + dx;
                    int ty = city.Y + dy;
                    if (!sim.Map.IsInBounds(tx, ty)) continue;

                    var tile = sim.Map.GetTile(tx, ty);
                    if (tile != null && tile.OwnerCityId == city.Id && !used.Contains((tx, ty)))
                    {
                        potentialTiles.Add(tile);
                    }
                }
            }

            potentialTiles.Sort((a, b) =>
                (b.TotalYield.Food + b.TotalYield.Production + b.TotalYield.Commerce)
                .CompareTo(a.TotalYield.Food + a.TotalYield.Production + a.TotalYield.Commerce)
            );

            int needed = city.Population - workingTiles.Count;
            for (int i = 0; i < Math.Min(needed, potentialTiles.Count); i++)
            {
                workingTiles.Add(potentialTiles[i]);
            }
        }

        foreach (var tile in workingTiles)
        {
            baseFood += tile.TotalYield.Food;
            baseProd += tile.TotalYield.Production;
            baseComm += tile.TotalYield.Commerce;
        }

        // Apply Wonder/Small Wonder multipliers
        if (city.Buildings.Any(b => b.Id == "forbidden_palace")) baseComm = (int)Math.Round(baseComm * 1.5f);
        if (city.Buildings.Any(b => b.Id == "iron_works")) baseProd *= 2;

        _baseProd = baseProd;

        int foodConsumption = city.Population * 2;
        int netFood = baseFood - foodConsumption;

        // --- 1. PRODUCTION BAR ---
        var prodBox = new VBoxContainer();
        prodBox.AddThemeConstantOverride("separation", 1);
        leftCol.AddChild(prodBox);

        var prodTitle = new Label { Text = $"PRODUCTION  {baseProd} per turn" };
        prodTitle.AddThemeFontSizeOverride("font_size", 11);
        prodTitle.AddThemeColorOverride("font_color", new Color(0.12f, 0.45f, 0.75f)); // Blue for production
        prodBox.AddChild(prodTitle);

        var prodIcons = new Label { Text = string.Concat(Enumerable.Repeat("🛡️", Math.Min(15, baseProd))) + (baseProd > 15 ? $" (+{baseProd - 15})" : "") };
        prodIcons.AddThemeFontSizeOverride("font_size", 12);
        prodBox.AddChild(prodIcons);

        // --- 2. FOOD BAR ---
        var foodBox = new VBoxContainer();
        foodBox.AddThemeConstantOverride("separation", 1);
        leftCol.AddChild(foodBox);

        string growthStatus = netFood > 0 ? $"+{netFood}/turn" : netFood < 0 ? $"{netFood}/turn" : "Zero Growth";
        var foodTitle = new Label { Text = $"FOOD  {baseFood} per turn ({growthStatus})" };
        foodTitle.AddThemeFontSizeOverride("font_size", 11);
        foodTitle.AddThemeColorOverride("font_color", new Color(0.65f, 0.45f, 0.05f)); // Golden brown for food
        foodBox.AddChild(foodTitle);

        // Draw green wheat for net food, red apples/icons for eaten food, or simple splits
        string foodText = string.Concat(Enumerable.Repeat("🌾", Math.Min(12, baseFood)));
        if (foodConsumption > 0)
        {
            int consumed = Math.Min(10, foodConsumption);
            foodText = string.Concat(Enumerable.Repeat("🟢", Math.Max(0, baseFood - foodConsumption))) + " " + string.Concat(Enumerable.Repeat("🔴", consumed));
        }
        var foodIcons = new Label { Text = foodText };
        foodIcons.AddThemeFontSizeOverride("font_size", 10);
        foodBox.AddChild(foodIcons);

        // --- 3. COMMERCE BAR ---
        var commBox = new VBoxContainer();
        commBox.AddThemeConstantOverride("separation", 1);
        leftCol.AddChild(commBox);

        var commTitle = new Label { Text = $"COMMERCE  {baseComm} per turn" };
        commTitle.AddThemeFontSizeOverride("font_size", 11);
        commTitle.AddThemeColorOverride("font_color", new Color(0.15f, 0.45f, 0.15f)); // Green for commerce
        commBox.AddChild(commTitle);

        // Split gold and science according to tax rate
        int goldPart = (baseComm * sim.PlayerTaxRate) / 100;
        int sciPart = baseComm - goldPart;

        var commSplit = new HBoxContainer();
        commBox.AddChild(commSplit);

        var goldPartLabel = new Label { Text = $"💰 {goldPart} Gold ({sim.PlayerTaxRate}%)  " };
        goldPartLabel.AddThemeFontSizeOverride("font_size", 10);
        goldPartLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.45f, 0.05f));
        commSplit.AddChild(goldPartLabel);

        var sciPartLabel = new Label { Text = $"🧪 {sciPart} Science ({100 - sim.PlayerTaxRate}%)" };
        sciPartLabel.AddThemeFontSizeOverride("font_size", 10);
        sciPartLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.45f, 0.55f));
        commSplit.AddChild(sciPartLabel);

        // --- 3b. CORRUPTION & WASTE ---
        if (city.LastTurnCorruption > 0 || city.LastTurnWaste > 0)
        {
            var cwBox = new HBoxContainer();
            cwBox.AddThemeConstantOverride("separation", 10);
            leftCol.AddChild(cwBox);

            if (city.LastTurnWaste > 0)
            {
                var wasteLabel = new Label { Text = $"Waste: -{city.LastTurnWaste}" };
                wasteLabel.AddThemeFontSizeOverride("font_size", 10);
                wasteLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.15f, 0.15f));
                cwBox.AddChild(wasteLabel);
            }
            if (city.LastTurnCorruption > 0)
            {
                var corrLabel = new Label { Text = $"Corruption: -{city.LastTurnCorruption}" };
                corrLabel.AddThemeFontSizeOverride("font_size", 10);
                corrLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.15f, 0.15f));
                cwBox.AddChild(corrLabel);
            }
        }

        // --- 4. GRANARY PREVIEW ---
        var granBox = new VBoxContainer();
        granBox.AddThemeConstantOverride("separation", 2);
        leftCol.AddChild(granBox);

        var granTitle = new Label { Text = $"GRANARY  ({city.StoredFood}/{city.FoodNeededForGrowth})" };
        granTitle.AddThemeFontSizeOverride("font_size", 10);
        granTitle.AddThemeColorOverride("font_color", new Color(0.3f, 0.25f, 0.15f));
        granBox.AddChild(granTitle);

        // Draw a row of boxes or wheat representing food stored
        var granHBox = new HBoxContainer();
        granBox.AddChild(granHBox);

        for (int i = 0; i < city.FoodNeededForGrowth; i += 2)
        {
            string symbol = (i < city.StoredFood) ? "🌾" : "░";
            var item = new Label { Text = symbol };
            item.AddThemeFontSizeOverride("font_size", 11);
            granHBox.AddChild(item);
        }

        // ==========================================
        // RIGHT COLUMN: Active Project Display
        // ==========================================
        var rightCol = new VBoxContainer();
        rightCol.CustomMinimumSize = new Vector2(170, 0);
        rightCol.Alignment = BoxContainer.AlignmentMode.Center;
        rightCol.AddThemeConstantOverride("separation", 4);
        hsplit.AddChild(rightCol);

        var projFrameStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.9f, 0.85f, 0.75f, 0.8f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = new Color(0.45f, 0.38f, 0.28f, 0.5f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        };
        var frame = new PanelContainer();
        frame.AddThemeStyleboxOverride("panel", projFrameStyle);
        frame.MouseDefaultCursorShape = CursorShape.PointingHand;
        rightCol.AddChild(frame);

        var frameVBox = new VBoxContainer();
        frameVBox.AddThemeConstantOverride("separation", 4);
        frameVBox.Alignment = BoxContainer.AlignmentMode.Center;
        frameVBox.MouseFilter = Control.MouseFilterEnum.Ignore;
        frame.AddChild(frameVBox);

        var activeTitle = new Label { Text = "BUILDING PROJECT" };
        activeTitle.AddThemeFontSizeOverride("font_size", 10);
        activeTitle.AddThemeColorOverride("font_color", new Color(0.4f, 0.35f, 0.25f));
        activeTitle.HorizontalAlignment = HorizontalAlignment.Center;
        activeTitle.MouseFilter = Control.MouseFilterEnum.Ignore;
        frameVBox.AddChild(activeTitle);

        if (city.CurrentProject == ProductionProject.None)
        {
            var idleLabel = new Label { Text = "IDLE" };
            idleLabel.AddThemeFontSizeOverride("font_size", 16);
            idleLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.5f));
            idleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            idleLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            frameVBox.AddChild(idleLabel);

            var idleDesc = new Label { Text = "Click to Choose" };
            idleDesc.AddThemeFontSizeOverride("font_size", 9);
            idleDesc.HorizontalAlignment = HorizontalAlignment.Center;
            idleDesc.MouseFilter = Control.MouseFilterEnum.Ignore;
            frameVBox.AddChild(idleDesc);
        }
        else
        {
            var projName = new Label { Text = city.CurrentProject.ToString().ToUpper() };
            projName.AddThemeFontSizeOverride("font_size", 13);
            projName.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.12f));
            projName.HorizontalAlignment = HorizontalAlignment.Center;
            projName.MouseFilter = Control.MouseFilterEnum.Ignore;
            frameVBox.AddChild(projName);

            // Project Icon/Graphic: Load high-resolution texture or fallback to emoji
            var tex = GetProjectTexture(city.CurrentProject);
            if (tex != null)
            {
                var textureRect = new TextureRect
                {
                    Texture = tex,
                    CustomMinimumSize = new Vector2(64, 64),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                frameVBox.AddChild(textureRect);
            }
            else
            {
                var graphic = new Label { Text = getProjectEmoji(city.CurrentProject) };
                graphic.AddThemeFontSizeOverride("font_size", 34);
                graphic.HorizontalAlignment = HorizontalAlignment.Center;
                graphic.MouseFilter = Control.MouseFilterEnum.Ignore;
                frameVBox.AddChild(graphic);
            }

            int cost = city.GetProjectCost(city.CurrentProject);
            int progress = city.CurrentProductionProgress;
            int turnsLeft = baseProd > 0 ? (int)Math.Ceiling((double)(cost - progress) / baseProd) : 9999;

            var timeLabel = new Label { Text = turnsLeft == 9999 ? "Never Completes" : $"Complete in {turnsLeft} turn{(turnsLeft == 1 ? "" : "s")}" };
            timeLabel.AddThemeFontSizeOverride("font_size", 10);
            timeLabel.AddThemeColorOverride("font_color", new Color(0.12f, 0.4f, 0.15f));
            timeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            timeLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            frameVBox.AddChild(timeLabel);

            // 3D-styled Empty Shield Slots Grid (Civ3 Style)
            // Determine scale: 1 slot = 'shieldStep' shields
            int shieldStep = 1;
            if (cost > 100)
            {
                shieldStep = 10;
            }
            else if (cost > 50)
            {
                shieldStep = 5;
            }
            
            int totalSlots = (int)Math.Ceiling((double)cost / shieldStep);
            int filledSlots = (int)Math.Floor((double)progress / shieldStep);
            
            var gridContainer = new GridContainer();
            gridContainer.Columns = 10;
            gridContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            gridContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            gridContainer.AddThemeConstantOverride("h_separation", 2);
            gridContainer.AddThemeConstantOverride("v_separation", 2);
            
            for (int i = 0; i < totalSlots; i++)
            {
                var slot = new PanelContainer();
                slot.CustomMinimumSize = new Vector2(14, 14);
                
                // Recessed empty tile style
                var emptyStyle = new StyleBoxFlat
                {
                    BgColor = new Color(0.82f, 0.77f, 0.67f, 0.9f), // parchment stone recessed
                    BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                    BorderColor = new Color(0.5f, 0.45f, 0.35f, 0.7f), // dark borders
                    CornerRadiusTopLeft = 1, CornerRadiusTopRight = 1, CornerRadiusBottomLeft = 1, CornerRadiusBottomRight = 1
                };
                
                slot.AddThemeStyleboxOverride("panel", emptyStyle);
                
                if (i < filledSlots)
                {
                    var shieldLabel = new Label { Text = "🛡️" };
                    shieldLabel.AddThemeFontSizeOverride("font_size", 9);
                    shieldLabel.HorizontalAlignment = HorizontalAlignment.Center;
                    shieldLabel.VerticalAlignment = VerticalAlignment.Center;
                    shieldLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
                    slot.AddChild(shieldLabel);
                }
                
                gridContainer.AddChild(slot);
            }
            
            frameVBox.AddChild(gridContainer);

            var progressLabel = new Label { Text = $"{progress}/{cost} Shields" };
            progressLabel.AddThemeFontSizeOverride("font_size", 9);
            progressLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.36f, 0.22f));
            progressLabel.HorizontalAlignment = HorizontalAlignment.Center;
            progressLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            frameVBox.AddChild(progressLabel);
        }

        // Handle clicking on the card
        frame.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                var dialog = new CityProductionSelectionDialog(city, sim, _baseProd, () => _onProjectChanged?.Invoke());
                GetTree().Root.AddChild(dialog);
            }
        };

        // ==========================================
        // QUEUE PANEL (Civ3 style "QUEUE (hold 'Shift' to add)")
        // ==========================================
        var queueSpacer = new Control { CustomMinimumSize = new Vector2(0, 4) };
        rightCol.AddChild(queueSpacer);

        var queueStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.92f, 0.89f, 0.79f, 0.95f), // Parchment background
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = new Color(0.5f, 0.42f, 0.3f, 0.6f),
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        var queuePanel = new PanelContainer();
        queuePanel.AddThemeStyleboxOverride("panel", queueStyle);
        queuePanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        rightCol.AddChild(queuePanel);

        var queueVBox = new VBoxContainer();
        queueVBox.AddThemeConstantOverride("separation", 2);
        queuePanel.AddChild(queueVBox);

        var queueTitle = new Label { Text = "QUEUE (hold 'Shift' to add)" };
        queueTitle.AddThemeFontSizeOverride("font_size", 9);
        queueTitle.AddThemeColorOverride("font_color", new Color(0.35f, 0.28f, 0.18f));
        queueTitle.HorizontalAlignment = HorizontalAlignment.Center;
        queueVBox.AddChild(queueTitle);
        
        queueVBox.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        queueVBox.AddChild(scroll);

        var listVBox = new VBoxContainer();
        listVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        listVBox.AddThemeConstantOverride("separation", 3);
        scroll.AddChild(listVBox);

        if (city.ProductionQueue.Count == 0)
        {
            var emptyLabel = new Label { Text = "2. Empty Slot" };
            emptyLabel.AddThemeFontSizeOverride("font_size", 10);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.5f, 0.8f)); // Soft blue like screenshot
            listVBox.AddChild(emptyLabel);
        }
        else
        {
            for (int i = 0; i < city.ProductionQueue.Count; i++)
            {
                var queuedProj = city.ProductionQueue[i];
                int indexInQueue = i + 2; // e.g. 2. Settler, 3. Worker
                
                var itemHBox = new HBoxContainer();
                itemHBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                listVBox.AddChild(itemHBox);

                // Small icon (zmenšený)
                var queuedTex = GetProjectTexture(queuedProj);
                if (queuedTex != null)
                {
                    var miniIcon = new TextureRect
                    {
                        Texture = queuedTex,
                        CustomMinimumSize = new Vector2(20, 20),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
                    };
                    itemHBox.AddChild(miniIcon);
                }
                else
                {
                    var miniEmoji = new Label { Text = getProjectEmoji(queuedProj) };
                    miniEmoji.AddThemeFontSizeOverride("font_size", 11);
                    miniEmoji.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
                    itemHBox.AddChild(miniEmoji);
                }

                // Name and index
                string projName = queuedProj.ToString();
                var nameLabel = new Label { Text = $"{indexInQueue}. {projName}" };
                nameLabel.AddThemeFontSizeOverride("font_size", 10);
                nameLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.12f));
                nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                itemHBox.AddChild(nameLabel);

                // Calculate turns remaining in queue
                int qCost = city.GetProjectCost(queuedProj);
                int qTurns = baseProd > 0 ? (int)Math.Ceiling((double)qCost / baseProd) : 9999;
                var turnsText = qTurns == 9999 ? "Never" : $"{qTurns} turn{(qTurns == 1 ? "" : "s")}";
                
                var turnsLabel = new Label { Text = turnsText };
                turnsLabel.AddThemeFontSizeOverride("font_size", 9);
                turnsLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.3f));
                itemHBox.AddChild(turnsLabel);

                // A tiny delete button to remove from queue
                var delButton = new Button { Text = "×" };
                delButton.AddThemeFontSizeOverride("font_size", 9);
                delButton.AddThemeColorOverride("font_color", new Color(0.7f, 0.2f, 0.2f));
                delButton.Flat = true;
                delButton.FocusMode = FocusModeEnum.None;
                
                int itemIdx = i; // local copy for closure
                delButton.Pressed += () =>
                {
                    city.ProductionQueue.RemoveAt(itemIdx);
                    _onProjectChanged?.Invoke(); // Refresh layout!
                };
                itemHBox.AddChild(delButton);
            }
        }
    }

    internal static string getProjectEmoji(ProductionProject proj)
    {
        string name = proj.ToString().ToLower();
        if (name.Contains("settler")) return "🧑‍🤝‍🧑";
        if (name.Contains("worker")) return "⚒️";
        if (name.Contains("warrior")) return "⚔️";
        if (name.Contains("archer")) return "🏹";
        if (name.Contains("explorer")) return "🧭";
        if (name.Contains("granary")) return "🌾";
        if (name.Contains("barracks")) return "⚔️";
        if (name.Contains("temple")) return "🏛️";
        if (name.Contains("library")) return "📖";
        if (name.Contains("colosseum")) return "🏟️";
        if (name.Contains("aqueduct")) return "🚰";
        if (name.Contains("cathedral")) return "⛪";
        if (name.Contains("university")) return "🎓";
        if (name.Contains("palace")) return "🏰";
        if (name.Contains("epic")) return "🛡️";
        if (name.Contains("academy")) return "🎖️";
        if (name.Contains("pentagon")) return "🛑";
        return "🏢";
    }

    internal static Texture2D? GetProjectTexture(ProductionProject proj)
    {
        string? baseName = proj switch
        {
            ProductionProject.Explorer => "explorer",
            ProductionProject.Settler => "settler",
            ProductionProject.Worker => "worker",
            ProductionProject.Warrior => "warrior",
            ProductionProject.Archer => "archer",
            _ => null
        };

        if (baseName == null) return null;

        // Try to load original webp first, then fallback to png
        string origPath = $"res://assets/{baseName}_orig.webp";
        if (ResourceLoader.Exists(origPath))
        {
            try
            {
                var tex = GD.Load<Texture2D>(origPath);
                if (tex != null) return tex;
            }
            catch {}
        }

        string pngPath = $"res://assets/{baseName}.png";
        if (ResourceLoader.Exists(pngPath))
        {
            try
            {
                var tex = GD.Load<Texture2D>(pngPath);
                if (tex != null) return tex;
            }
            catch {}
        }

        return null;
    }
}
