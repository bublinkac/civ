using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CivGame.Core;
using CivGame.UI;
using CoreTileData = CivGame.Core.TileData;

namespace CivGame.Render;

public partial class MapRenderer : TileMapLayer
{
    [Export] public int MapWidth { get; set; } = 40;
    [Export] public int MapHeight { get; set; } = 40;
    [Export] public int Seed { get; set; } = 1337;

    private GameSimulation? _sim;
    private FogRenderer? _fogRenderer;
    private UnitRenderer? _unitRenderer;
    private Camera2D? _camera;
    private float _cameraSpeed = 500f;

    private string? _selectedUnitId;
    private string? _selectedCityId;
    private GameHud? _hud;
    private HighlightRenderer? _highlightRenderer;
    private CityRenderer? _cityRenderer;
    private BorderRenderer? _borderRenderer;
    private CityDetailPanel? _cityDetail; 
    private AdvisorsMenu? _advisorsMenu;
    private TechTreePanel? _techTreePanel;
    private DomesticAdvisorPanel? _domesticAdvisorPanel;
    private bool IsFullScreenUiOpen => GodotObject.IsInstanceValid(_cityDetail) || 
                                       GodotObject.IsInstanceValid(_advisorsMenu) || 
                                       GodotObject.IsInstanceValid(_techTreePanel) || 
                                       GodotObject.IsInstanceValid(_domesticAdvisorPanel);


    private MainMenu? _mainMenu;

    public void OpenCityDetail(City city)
    {
        GD.Print($"[MapRenderer] Opening city detail for: {city.Name}");
        if (GodotObject.IsInstanceValid(_cityDetail))
        {
            _cityDetail.QueueFree();
        }
        if (_sim == null) return;

        _cityDetail = new CityDetailPanel(city, _sim);
        
        // Added to HUD (CanvasLayer) so it stays fixed on screen and is drawn on top of the map
        if (GodotObject.IsInstanceValid(_hud))
        {
            _hud.AddChild(_cityDetail);
        }
        else
        {
            AddChild(_cityDetail);
        }
    }

    public void OpenAdvisors()
    {
        if (GodotObject.IsInstanceValid(_advisorsMenu)) _advisorsMenu.QueueFree();
        
        _advisorsMenu = new AdvisorsMenu();
        _advisorsMenu.OnOpenScienceAdvisor += OpenTechTree;
        _advisorsMenu.OnOpenDomesticAdvisor += OpenDomesticAdvisor;
        
        if (GodotObject.IsInstanceValid(_hud)) _hud.AddChild(_advisorsMenu);
        else AddChild(_advisorsMenu);
    }

    public void OpenTechTree()
    {
        if (GodotObject.IsInstanceValid(_techTreePanel)) _techTreePanel.QueueFree();
        if (_sim == null) return;

        _techTreePanel = new TechTreePanel(_sim);
        
        if (GodotObject.IsInstanceValid(_hud)) _hud.AddChild(_techTreePanel);
        else AddChild(_techTreePanel);
    }

    public void OpenDomesticAdvisor()
    {
        if (GodotObject.IsInstanceValid(_domesticAdvisorPanel)) _domesticAdvisorPanel.QueueFree();
        if (_sim == null) return;

        _domesticAdvisorPanel = new DomesticAdvisorPanel(_sim);
        
        if (GodotObject.IsInstanceValid(_hud)) _hud.AddChild(_domesticAdvisorPanel);
        else AddChild(_domesticAdvisorPanel);
    }

    public override void _Ready()
    {
        // 1. Enable Y-Sorting for correct rendering overlay order
        YSortEnabled = true;

        // Show Main Menu first
        _mainMenu = new MainMenu();
        _mainMenu.OnStartGame += (width, height, seed) =>
        {
            _mainMenu.QueueFree();
            _mainMenu = null;
            StartGame(width, height, seed);
        };
        GetParent().CallDeferred(Node.MethodName.AddChild, _mainMenu);
    }

    private void StartGame(int width, int height, int seed)
    {
        MapWidth = width;
        MapHeight = height;
        Seed = seed;

        // 2. Initialize Map Data and Game Simulation
        var generator = new MapGenerator(Seed);
        var map = generator.Generate(MapWidth, MapHeight);
        _sim = new GameSimulation(map);
        _sim.SetupInitialUnits();

        // 3. Programmatically configure TileSet for Isometric layout
        SetupTileSet();

        // 4. Render GameMap to TileMapLayer cells
        RenderMap();

        // 5. Spawn peer rendering layers (Fog and Units)
        SetupPeerRenderers();

        // 6. Setup Controllable Camera and focus on starting unit
        SetupCamera();
    }

    private void SetupPeerRenderers()
    {
        if (_sim == null) return;

        // Instantiate Fog of War renderer
        _fogRenderer = new FogRenderer();
        GetParent().CallDeferred(Node.MethodName.AddChild, _fogRenderer);

        // Instantiate Unit renderer
        _unitRenderer = new UnitRenderer();
        _unitRenderer.Initialize(this);
        GetParent().CallDeferred(Node.MethodName.AddChild, _unitRenderer);

        // Instantiate Highlight renderer
        _highlightRenderer = new HighlightRenderer();
        GetParent().CallDeferred(Node.MethodName.AddChild, _highlightRenderer);

        // Instantiate City renderer
        _cityRenderer = new CityRenderer();
        _cityRenderer.Initialize(this);
        GetParent().CallDeferred(Node.MethodName.AddChild, _cityRenderer);

        // Instantiate Border renderer
        _borderRenderer = new BorderRenderer();
        GetParent().CallDeferred(Node.MethodName.AddChild, _borderRenderer);

        // Instantiate HUD
        _hud = new GameHud();
        _hud.OnActionTriggered += HandleHudAction;
        _hud.OnMinimapTileSelected += HandleMinimapTileSelected;
        GetParent().CallDeferred(Node.MethodName.AddChild, _hud);

        // Defer initial updates until both renderers are ready in the scene tree
        Callable.From(() =>
        {
            _fogRenderer.UpdateFog(_sim);
            if (_sim.Units.Count > 0)
            {
                _selectedUnitId = _sim.Units[0].Id;
            }
            _unitRenderer.UpdateUnits(_sim, _selectedUnitId);
            _cityRenderer.UpdateCities(_sim);
            _borderRenderer.UpdateBorders(_sim);
            Unit? initial = _sim.Units.Find(u => u.Id == _selectedUnitId);
            _highlightRenderer.UpdateHighlight(_sim, initial);
            _hud.Refresh(_sim, _selectedUnitId, _selectedCityId);
        }).CallDeferred();
    }

    private void SetupTileSet()
    {
        var tileSet = new TileSet
        {
            TileShape = TileSet.TileShapeEnum.Isometric,
            TileLayout = TileSet.TileLayoutEnum.DiamondDown,
            TileSize = new Vector2I(256, 128)
        };

        // Create and register textures based on TerrainRegistry
        int id = 0;
        foreach (var terrain in TerrainRegistry.All.Values)
        {
            Texture2D texture = GetOrCreateTerrainTexture(terrain);

            var source = new TileSetAtlasSource
            {
                Texture = texture,
                TextureRegionSize = new Vector2I(256, 128)
            };
            source.CreateTile(new Vector2I(0, 0));

            tileSet.AddSource(source, id);
            id++;
        }

        this.TileSet = tileSet;
    }

    private Texture2D GetOrCreateTerrainTexture(Terrain terrain)
    {
        string dirPath = "res://assets";
        string fileName = terrain.Id.ToString().ToLower() + ".png";
        string filePath = $"{dirPath}/{fileName}";

        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(dirPath);
        }

        Image img = GenerateTerrainImage(terrain);

        try
        {
            img.SavePng(filePath);
        }
        catch (Exception ex)
        {
            GD.Print($"[MapRenderer] Warning: Could not save PNG to {filePath}: {ex.Message}");
        }

        return ImageTexture.CreateFromImage(img);
    }

    private Image GenerateTerrainImage(Terrain terrain)
    {
        int width = 256;
        int height = 128;
        Image img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0)); // transparent background

        // Simplified procedural generation using terrain ID
        Color topColor = Colors.Green;
        Color borderColor = Colors.DarkGreen;

        switch (terrain.Id)
        {
            case "grassland":
                topColor = new Color(0.2f, 0.65f, 0.2f);
                borderColor = new Color(0.1f, 0.4f, 0.1f);
                break;
            case "plains":
                topColor = new Color(0.7f, 0.65f, 0.25f);
                borderColor = new Color(0.45f, 0.4f, 0.15f);
                break;
            case "ocean":
                topColor = new Color(0.1f, 0.35f, 0.75f);
                borderColor = new Color(0.05f, 0.2f, 0.5f);
                break;
            case "desert":
                topColor = new Color(0.85f, 0.7f, 0.4f);
                borderColor = new Color(0.55f, 0.45f, 0.25f);
                break;
            case "mountain":
                return DrawMountainImage(width, height);
            default:
                topColor = Colors.Gray;
                borderColor = Colors.DarkGray;
                break;
        }

        // ... rest of the rendering loop ...
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dx = Math.Abs(x - 128.0) / 128.0;
                double dy = Math.Abs(y - 64.0) / 64.0;
                double dist = dx + dy;

                if (dist <= 1.0)
                {
                    if (dist > 0.95)
                    {
                        img.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        double lx = (x - 128.0) / 128.0;
                        double ly = (y - 64.0) / 64.0;
                        double light = (-lx - ly) / 2.0;
                        Color finalColor = light > 0
                            ? topColor.Lerp(Colors.White, (float)(light * 0.12))
                            : topColor.Lerp(Colors.Black, (float)(-light * 0.12));

                        if (terrain.Id == "ocean")
                        {
                            double ripple = Math.Sin((x + y * 2.5) * 0.15);
                            if (ripple > 0.8)
                            {
                                finalColor = finalColor.Lerp(Colors.White, 0.2f);
                            }
                        }
                        else if (terrain.Id == "desert")
                        {
                            double dune = Math.Sin((x - y * 1.5) * 0.12);
                            if (dune > 0.75)
                            {
                                finalColor = finalColor.Lerp(Colors.Black, 0.08f);
                            }
                        }

                        img.SetPixel(x, y, finalColor);
                    }
                }
            }
        }

        return img;
    }

    private Image DrawMountainImage(int width, int height)
    {
        Image img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        Color grassBase = new Color(0.2f, 0.65f, 0.2f);
        Color mountainColor = new Color(0.5f, 0.5f, 0.5f);
        Color shadowColor = new Color(0.35f, 0.35f, 0.35f);
        Color snowColor = Colors.White;
        Color outlineColor = new Color(0.1f, 0.4f, 0.1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dx = Math.Abs(x - 128.0) / 128.0;
                double dy = Math.Abs(y - 64.0) / 64.0;
                double dist = dx + dy;

                if (dist <= 1.0)
                {
                    if (dist > 0.95)
                    {
                        img.SetPixel(x, y, outlineColor);
                    }
                    else
                    {
                        img.SetPixel(x, y, grassBase);
                    }
                }

                if (y >= 20 && y <= 92)
                {
                    double factor = (y - 20) / 72.0;
                    int halfWidth = (int)(factor * 60.0);
                    
                    int leftX = 128 - halfWidth;
                    int rightX = 128 + halfWidth;

                    if (x >= leftX && x <= rightX)
                    {
                        if (x == leftX || x == rightX || y == 20 || y == 92)
                        {
                            img.SetPixel(x, y, new Color(0.15f, 0.15f, 0.15f));
                        }
                        else if (y < 42)
                        {
                            img.SetPixel(x, y, snowColor);
                        }
                        else
                        {
                            if (x < 128)
                            {
                                img.SetPixel(x, y, mountainColor);
                            }
                            else
                            {
                                img.SetPixel(x, y, shadowColor);
                            }
                        }
                    }
                }
            }
        }

        return img;
    }

        // Need a stable mapping for TileSet IDs
        private Dictionary<string, int> _terrainToId = new()
        {
            {"grassland", 0}, {"plains", 1}, {"desert", 2}, {"tundra", 3}, {"hills", 4}, 
            {"mountain", 5}, {"forest", 6}, {"jungle", 7}, {"marsh", 8}, {"floodplains", 9}, 
            {"ocean", 10}, {"sea", 11}, {"coast", 12}
        };

    private void RenderMap()
    {
        if (_sim == null) return;

        Clear();

        for (int x = 0; x < _sim.Map.Width; x++)
        {
            for (int y = 0; y < _sim.Map.Height; y++)
            {
                CoreTileData? tile = _sim.Map.GetTile(x, y);
                if (tile != null)
                {
                    int id = _terrainToId.ContainsKey(tile.Terrain.Id) ? _terrainToId[tile.Terrain.Id] : 0;
                    SetCell(new Vector2I(x, y), sourceId: id, atlasCoords: new Vector2I(0, 0));
                }
            }
        }
    }

    private void SetupCamera()
    {
        if (_sim == null || _sim.Units.Count == 0) return;

        // Auto center on the starting explorer
        var initialUnit = _sim.Units[0];
        Vector2 startPos = MapToLocal(new Vector2I(initialUnit.X, initialUnit.Y));

        _camera = new Camera2D
        {
            Position = startPos,
            Zoom = new Vector2(0.8f, 0.8f)
        };
        AddChild(_camera);
    }

    private bool CheckIsFullScreenUiOpen() => GodotObject.IsInstanceValid(_cityDetail) || 
                                              GodotObject.IsInstanceValid(_advisorsMenu) || 
                                              GodotObject.IsInstanceValid(_techTreePanel) || 
                                              GodotObject.IsInstanceValid(_domesticAdvisorPanel);

    public override void _Process(double delta)
    {
        if (_camera == null || CheckIsFullScreenUiOpen()) return;

        Vector2 movement = Vector2.Zero;

        // Keyboard scrolling
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) movement.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) movement.Y += 1;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) movement.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) movement.X += 1;

        // Mouse edge scrolling
        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        float margin = 20f;

        // Ensure the mouse is actually inside the window (focused)
        if (mousePos.X >= 0 && mousePos.Y >= 0 && mousePos.X <= viewportSize.X && mousePos.Y <= viewportSize.Y)
        {
            // Do not edge-scroll down if mouse is hovering over the bottom HUD dock (bottom 160px)
            bool overBottomHud = mousePos.Y > viewportSize.Y - 160;
            // Do not edge-scroll up if mouse is hovering over the top bar (top-left 350px wide, 60px high)
            bool overTopBar = mousePos.Y < 60 && mousePos.X < 350;

            if (mousePos.X < margin) movement.X -= 1;
            if (mousePos.X > viewportSize.X - margin) movement.X += 1;
            if (mousePos.Y < margin && !overTopBar) movement.Y -= 1;
            if (mousePos.Y > viewportSize.Y - margin && !overBottomHud) movement.Y += 1;
        }

        if (movement != Vector2.Zero)
        {
            float speedAdjustment = 1.0f / _camera.Zoom.X;
            _camera.Position += movement.Normalized() * _cameraSpeed * speedAdjustment * (float)delta;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_camera == null || _sim == null || _unitRenderer == null || _fogRenderer == null || CheckIsFullScreenUiOpen()) return;

        // Block gameplay actions if the game has ended
        if (_sim.EndState != GameEndState.None) return;

        if (@event is InputEventMouseButton mouseButton)
        {
            // Zoom controls
            if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
            {
                float newZoom = Math.Min(_camera.Zoom.X + 0.1f, 3.0f);
                _camera.Zoom = new Vector2(newZoom, newZoom);
                return;
            }
            if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
            {
                float newZoom = Math.Max(_camera.Zoom.X - 0.1f, 0.2f);
                _camera.Zoom = new Vector2(newZoom, newZoom);
                return;
            }

            // Left Click: Selection
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
            {
                Vector2I clickedCell = LocalToMap(GetLocalMousePosition());
                
                if (!_sim.Map.IsInBounds(clickedCell.X, clickedCell.Y) || _sim.VisibilityGrid[clickedCell.X, clickedCell.Y] == FogState.Unexplored)
                {
                    GD.Print("[Selection] Tile is unexplored or out of bounds!");
                    return;
                }
                
                // Check for city first (cities take precedence)
                City? clickedCity = null;
                foreach (var city in _sim.Cities)
                {
                    if (city.X == clickedCell.X && city.Y == clickedCell.Y)
                    {
                        clickedCity = city;
                        break;
                    }
                }

                if (clickedCity != null)
                {
                    _selectedCityId = clickedCity.Id;
                    _selectedUnitId = null; // Clear unit selection

                    // --- NEW: Detect Double Click for City Detail ---
                    if (mouseButton.DoubleClick)
                    {
                        GD.Print($"[Selection] Double click detected on city: {clickedCity.Name}");
                        // Use CallDeferred to ensure the panel is added safely outside the input event processing
                        Callable.From(() => OpenCityDetail(clickedCity)).CallDeferred();
                    }
                    else
                    {
                        GD.Print($"[Selection] City: {clickedCity.Name} | Position: ({clickedCity.X}, {clickedCity.Y}) | Yields: F:{clickedCity.StoredFood} P:{clickedCity.StoredProduction} C:{clickedCity.StoredCommerce}");
                    }
                    // ------------------------------------------------
                }
                else
                {
                    // Check for unit
                    Unit? clickedUnit = null;
                    foreach (var unit in _sim.Units)
                    {
                        if (unit.X == clickedCell.X && unit.Y == clickedCell.Y)
                        {
                            clickedUnit = unit;
                            break;
                        }
                    }

                    if (clickedUnit != null)
                    {
                        _selectedUnitId = clickedUnit.Id;
                        _selectedCityId = null; // Clear city selection
                        GD.Print($"[Selection] Unit: {clickedUnit.Type} | Position: ({clickedUnit.X}, {clickedUnit.Y}) | MP: {clickedUnit.RemainingMovement}/{clickedUnit.MaxMovement}");
                    }
                    else
                    {
                        _selectedUnitId = null;
                        _selectedCityId = null;
                        GD.Print("[Selection] Cleared selection.");
                    }
                }

                _unitRenderer.UpdateUnits(_sim, _selectedUnitId);
                Unit? selUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
                _highlightRenderer?.UpdateHighlight(_sim, selUnit);
                _hud?.Refresh(_sim, _selectedUnitId, _selectedCityId);
            }

            // Right Click: Move Unit / Attack
            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed && !string.IsNullOrEmpty(_selectedUnitId))
            {
                Vector2I targetCell = LocalToMap(GetLocalMousePosition());
                Unit? selectedUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);

                if (selectedUnit != null)
                {
                    if (selectedUnit.Faction != Faction.Player)
                    {
                        GD.Print("[Control] Cannot command units belonging to other factions!");
                        return;
                    }

                    MoveValidationResult validation = _sim.ValidateUnitMove(selectedUnit, targetCell.X, targetCell.Y);
                    if (validation.IsValid)
                    {
                        bool moved = _sim.MoveUnit(selectedUnit, targetCell.X, targetCell.Y);
                        if (moved)
                        {
                            // If selected unit died in battle
                            if (!_sim.Units.Contains(selectedUnit))
                            {
                                _selectedUnitId = null;
                                selectedUnit = null;
                            }

                            _unitRenderer.UpdateUnits(_sim, _selectedUnitId);
                            _fogRenderer.UpdateFog(_sim);
                            _highlightRenderer?.UpdateHighlight(_sim, selectedUnit);
                            _hud?.Refresh(_sim, _selectedUnitId, _selectedCityId);
                        }
                    }
                    else
                    {
                        GD.Print($"[Move/Attack] Invalid action to ({targetCell.X}, {targetCell.Y}) | Reason: {validation.GetMessage()}");
                    }
                }
            }
        }

        // End Turn handling (Spacebar)
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Space)
            {
                _sim.EndTurn();
                GD.Print($"[Turn Manager] Turn {_sim.TurnNumber} started.");
                
                // Clear selection if selected unit died during barbarian turn!
                if (!string.IsNullOrEmpty(_selectedUnitId) && !_sim.Units.Any(u => u.Id == _selectedUnitId))
                {
                    _selectedUnitId = null;
                }

                _unitRenderer.UpdateUnits(_sim, _selectedUnitId);
                _fogRenderer.UpdateFog(_sim);
                _borderRenderer?.UpdateBorders(_sim);
                _cityRenderer?.UpdateCities(_sim); // City stats or labels might update
                Unit? turnUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
                _highlightRenderer?.UpdateHighlight(_sim, turnUnit);
                _hud?.Refresh(_sim, _selectedUnitId, _selectedCityId);
            }
            else if (keyEvent.Keycode == Key.B && !string.IsNullOrEmpty(_selectedUnitId))
            {
                Unit? selectedUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
                if (selectedUnit != null && selectedUnit.Type == UnitType.Settler)
                {
                    if (selectedUnit.Faction != Faction.Player)
                    {
                        GD.Print("[Control] Cannot settle a city with other factions' settlers!");
                        return;
                    }

                    if (_sim.CanBuildCity(selectedUnit))
                    {
                        var city = _sim.BuildCity(selectedUnit);
                        if (city != null)
                        {
                            GD.Print($"[City Manager] {city.Name} has been founded at ({city.X}, {city.Y})!");
                            _selectedUnitId = null; // Settler is consumed!
                            _unitRenderer.UpdateUnits(_sim, null);
                            _cityRenderer?.UpdateCities(_sim);
                            _borderRenderer?.UpdateBorders(_sim);
                            _fogRenderer.UpdateFog(_sim);
                            _highlightRenderer?.UpdateHighlight(_sim, null);
                            _selectedCityId = null;
                            _hud?.Refresh(_sim, null, null);
                        }
                    }
                    else
                    {
                        GD.Print("[City Manager] Cannot build city here. Must be on land and not on top of another city.");
                    }
                }
            }
            else if ((keyEvent.Keycode == Key.F || keyEvent.Keycode == Key.M || keyEvent.Keycode == Key.L) && !string.IsNullOrEmpty(_selectedUnitId))
            {
                Unit? selectedUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
                if (selectedUnit != null && selectedUnit.Type == UnitType.Worker)
                {
                    if (selectedUnit.Faction != Faction.Player)
                    {
                        GD.Print("[Control] Cannot command other factions' workers!");
                        return;
                    }

                    if (selectedUnit.IsWorkerBuilding())
                    {
                        GD.Print("[Worker] Worker is already busy constructing an improvement!");
                        return;
                    }

                    if (!selectedUnit.HasMovementRemaining())
                    {
                        GD.Print("[Worker] Worker has no movement points remaining to start construction!");
                        return;
                    }

                    TileImprovement? imp = keyEvent.Keycode switch
                    {
                        Key.F => new Farm(),
                        Key.M => new Mine(),
                        Key.L => new Plantation(),
                        _ => null
                    };

                    if (imp != null)
                    {
                        var tile = _sim.Map.GetTile(selectedUnit.X, selectedUnit.Y);
                        if (tile != null)
                        {
                            if (tile.Improvement != null)
                            {
                                GD.Print($"[Worker] There is already a {tile.Improvement.Name} built on this tile!");
                                return;
                            }

                            if (!imp.CanBeBuiltOn(tile.Terrain))
                            {
                                GD.Print($"[Worker] {imp.Name} cannot be built on {tile.Terrain} terrain!");
                                return;
                            }

                            // Start construction!
                            selectedUnit.StartImprovement(imp);
                            GD.Print($"[Worker] Started building {imp.Name} at ({selectedUnit.X}, {selectedUnit.Y}). Will complete in {imp.ConstructionTurns} turns.");
                            _unitRenderer.UpdateUnits(_sim, _selectedUnitId);
                            _hud?.Refresh(_sim, _selectedUnitId, _selectedCityId);
                        }
                    }
                }
            }
            else if (keyEvent.Keycode == Key.P && !string.IsNullOrEmpty(_selectedCityId))
            {
                City? selectedCity = _sim.Cities.Find(c => c.Id == _selectedCityId);
                if (selectedCity != null)
                {
                        _sim.CycleCityProduction(selectedCity);
                        GD.Print($"[Production Queue] {selectedCity.Name} changed project to: {selectedCity.CurrentProject} (Cost: {selectedCity.GetProjectCost(selectedCity.CurrentProject)} Prod)");
                        _hud?.Refresh(_sim, _selectedUnitId, _selectedCityId);
                    }
                }
            else if (keyEvent.Keycode == Key.C && !string.IsNullOrEmpty(_selectedCityId))
            {
                    City? selectedCity = _sim.Cities.Find(c => c.Id == _selectedCityId);
                if (selectedCity != null)
                    {
                    OpenCityDetail(selectedCity);
                    }
                }
            else if (keyEvent.Keycode == Key.T)
            {
                _sim.Research.CycleResearch();
                string projectInfo = _sim.Research.CurrentResearch?.Name ?? "None (Idle)";
                GD.Print($"[Research Queue] Active research project changed to: {projectInfo}");
        _hud?.Refresh(_sim, _selectedUnitId, _selectedCityId);
    }
        }
    }

    private void HandleHudAction(string actionType)
    {
        if (_sim == null) return;

        // Block HUD actions if the game has ended (except save/load)
        if (_sim.EndState != GameEndState.None && actionType != "save_game" && actionType != "load_game") return;

        switch (actionType)
        {
            case "save_game":
                {
                    ISaveSystem saveSystem = new JsonSaveSystem();
                    saveSystem.Save("save_slot_1", _sim);
                    GD.Print("[Save System] Game saved successfully!");
                }
                break;

            case "load_game":
                {
                    ISaveSystem saveSystem = new JsonSaveSystem();
                    if (saveSystem.Load("save_slot_1", _sim))
                    {
                        GD.Print("[Save System] Game loaded successfully!");
                        
                        // Recalculate selected unit/city reference safety
                        if (!string.IsNullOrEmpty(_selectedUnitId) && !_sim.Units.Any(u => u.Id == _selectedUnitId))
                        {
                            _selectedUnitId = null;
                        }
                        if (_sim.Units.Count > 0 && string.IsNullOrEmpty(_selectedUnitId))
                        {
                            _selectedUnitId = _sim.Units[0].Id;
                        }
                        _selectedCityId = null;
                        
                        UpdateUiAfterAction();
                    }
                    else
                    {
                        GD.Print("[Save System] Load failed: Save file does not exist!");
                    }
                }
                break;

            case "end_turn":
                _sim.EndTurn();
                GD.Print($"[Turn Manager] Turn {_sim.TurnNumber} started.");
                if (!string.IsNullOrEmpty(_selectedUnitId) && !_sim.Units.Any(u => u.Id == _selectedUnitId))
                {
                    _selectedUnitId = null;
                }
                UpdateUiAfterAction();
                break;

            case "settle":
                if (!string.IsNullOrEmpty(_selectedUnitId))
                {
                    Unit? selectedUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
                    if (selectedUnit != null && selectedUnit.Type == UnitType.Settler && selectedUnit.Faction == Faction.Player)
                    {
                        if (_sim.CanBuildCity(selectedUnit))
                        {
                            var city = _sim.BuildCity(selectedUnit);
                            if (city != null)
                            {
                                GD.Print($"[City Manager] {city.Name} has been founded at ({city.X}, {city.Y})!");
                                _selectedUnitId = null;
                                _selectedCityId = null;
                                UpdateUiAfterAction();
                            }
                        }
                    }
                }
                break;

            case "farm":
            case "mine":
            case "plantation":
            case "road":
                if (!string.IsNullOrEmpty(_selectedUnitId))
                {
                    Unit? selectedUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
                    if (selectedUnit != null && selectedUnit.Type == UnitType.Worker && selectedUnit.Faction == Faction.Player)
                    {
                        if (selectedUnit.IsWorkerBuilding() || !selectedUnit.HasMovementRemaining()) return;
                        TileImprovement? imp = actionType switch
                        {
                            "farm" => new Farm(),
                            "mine" => new Mine(),
                            "plantation" => new Plantation(),
                            "road" => new RoadBuild(),
                            _ => null
                        };
                        if (imp != null)
                        {
                            var tile = _sim.Map.GetTile(selectedUnit.X, selectedUnit.Y);
                            if (tile != null && tile.Improvement == null && imp.CanBeBuiltOn(tile.Terrain))
                            {
                                if (imp is RoadBuild && tile.HasRoad) return; // Already has road
                                selectedUnit.StartImprovement(imp);
                                GD.Print($"[Worker] Started building {imp.Name} at ({selectedUnit.X}, {selectedUnit.Y})!");
                                UpdateUiAfterAction();
                            }
                        }
                    }
                }
                break;

            case "fortify":
                if (!string.IsNullOrEmpty(_selectedUnitId))
                {
                    Unit? selectedUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
                    if (selectedUnit != null && selectedUnit.Faction == Faction.Player)
                    {
                        selectedUnit.IsFortified = true;
                        selectedUnit.RemainingMovement = 0;
                        GD.Print($"[Unit] {selectedUnit.Type} fortified at ({selectedUnit.X}, {selectedUnit.Y}). (+25% Defense Bonus)");
                        UpdateUiAfterAction();
                    }
                }
                break;

            case "sleep":
                if (!string.IsNullOrEmpty(_selectedUnitId))
                {
                    Unit? selectedUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
                    if (selectedUnit != null && selectedUnit.Faction == Faction.Player)
                    {
                        selectedUnit.IsSleeping = true;
                        selectedUnit.RemainingMovement = 0;
                        GD.Print($"[Unit] {selectedUnit.Type} went to sleep at ({selectedUnit.X}, {selectedUnit.Y}).");
                        UpdateUiAfterAction();
                    }
                }
                break;

            case "wake":
                if (!string.IsNullOrEmpty(_selectedUnitId))
                {
                    Unit? selectedUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
                    if (selectedUnit != null && selectedUnit.Faction == Faction.Player)
                    {
                        selectedUnit.IsFortified = false;
                        selectedUnit.IsSleeping = false;
                        GD.Print($"[Unit] {selectedUnit.Type} woke up at ({selectedUnit.X}, {selectedUnit.Y}).");
                        UpdateUiAfterAction();
                    }
                }
                break;

            case "cycle_production":
                if (!string.IsNullOrEmpty(_selectedCityId))
                {
                    City? selectedCity = _sim.Cities.Find(c => c.Id == _selectedCityId);
                    if (selectedCity != null && selectedCity.Faction == Faction.Player)
                    {
                        _sim.CycleCityProduction(selectedCity);
                        GD.Print($"[Production Queue] {selectedCity.Name} changed project to: {selectedCity.CurrentProject} (Cost: {selectedCity.GetProjectCost(selectedCity.CurrentProject)} Prod)");
                        UpdateUiAfterAction();
                    }
                }
                break;

            case "cycle_research":
                _sim.Research.CycleResearch();
                string proj = _sim.Research.CurrentResearch?.Name ?? "None (Idle)";
                GD.Print($"[Research Queue] Active research project: {proj}");
                UpdateUiAfterAction();
                break;

            case "open_advisors":
                OpenAdvisors();
                break;

            case "open_tech_tree":
                OpenTechTree();
                break;
        }
    }

    private void UpdateUiAfterAction()
    {
        if (_sim == null || _unitRenderer == null || _fogRenderer == null) return;
        _unitRenderer.UpdateUnits(_sim, _selectedUnitId);
        _fogRenderer.UpdateFog(_sim);
        _borderRenderer?.UpdateBorders(_sim);
        _cityRenderer?.UpdateCities(_sim);
        Unit? selUnit = _sim.Units.Find(u => u.Id == _selectedUnitId);
        _highlightRenderer?.UpdateHighlight(_sim, selUnit);
        _hud?.Refresh(_sim, _selectedUnitId, _selectedCityId);
    }

    private void HandleMinimapTileSelected(int x, int y)
    {
        if (_camera == null || _sim == null) return;
        Vector2 targetPos = MapToLocal(new Vector2I(x, y));
        _camera.Position = targetPos;
    }
}

