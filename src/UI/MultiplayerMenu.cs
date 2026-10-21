using System;
using System.Linq;
using Godot;
using CivGame.Core;
using CivGame.Audio;
using CivGame.Network;

namespace CivGame.UI;

public partial class MultiplayerMenu : PanelContainer
{
    public event Action<GameSetupOptions>? OnStartHotseatGame;
    public event Action<GameSetupOptions>? OnStartNetworkGame;
    public event Action? OnBackToTitle;

    // Hotseat Controls
    private LineEdit _p1NameInput;
    private OptionButton _p1CivInput;
    private LineEdit _p2NameInput;
    private OptionButton _p2CivInput;

    // Map & World Options
    private int _selectedSizeIndex = 2; // Standard
    private Button[] _sizeButtons;
    private OptionButton _waterInput;
    private OptionButton _ageInput;
    private OptionButton _climateInput;
    private OptionButton _tempInput;
    private OptionButton _diffInput;
    private SpinBox _seedInput;
    private int _seed = 1337;

    // LAN / Network Controls
    private LineEdit _netHostNameInput;
    private OptionButton _netHostCivInput;
    private SpinBox _netHostPortInput;
    private Button _netStartHostBtn;
    private Button _netLaunchHostGameBtn;
    private Label _netHostStatusLabel;

    private LineEdit _netJoinNameInput;
    private OptionButton _netJoinCivInput;
    private LineEdit _netJoinIpInput;
    private SpinBox _netJoinPortInput;
    private Button _netConnectBtn;
    private Label _netJoinStatusLabel;

    private static readonly (string Name, int Width, int Height)[] MapSizes =
    {
        ("Tiny", 40, 40),
        ("Small", 60, 60),
        ("Standard", 80, 80),
        ("Large", 100, 100),
        ("Huge", 130, 130)
    };

    public MultiplayerMenu()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.08f, 0.98f),
            BorderWidthLeft = 4,
            BorderWidthTop = 4,
            BorderWidthRight = 4,
            BorderWidthBottom = 4,
            BorderColor = new Color(0.92f, 0.75f, 0.3f) // Antique Gold
        };
        AddThemeStyleboxOverride("panel", bgStyle);

        var center = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        AddChild(center);

        var card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(720, 640)
        };
        card.AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.09f, 0.09f, 0.12f, 0.96f),
            new Color(0.85f, 0.65f, 0.22f), 2, 8));
        center.AddChild(card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        card.AddChild(margin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 14);
        margin.AddChild(mainVBox);

        // Header Title
        var title = new Label
        {
            Text = "⚔️  CIVILIZATION III: MULTIPLAYER  ⚔️",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.75f, 0.3f));
        mainVBox.AddChild(title);

        var tabContainer = new TabContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        mainVBox.AddChild(tabContainer);

        // TAB 1: HOTSEAT
        var hotseatTab = BuildHotseatTab();
        hotseatTab.Name = "👥 Hotseat (1 PC)";
        tabContainer.AddChild(hotseatTab);

        // TAB 2: LAN / DIRECT IP
        var networkTab = BuildNetworkTab();
        networkTab.Name = "🌐 LAN / Direct IP";
        tabContainer.AddChild(networkTab);

        // Bottom Bar (Back button)
        var bottomHBox = new HBoxContainer();
        bottomHBox.Alignment = BoxContainer.AlignmentMode.Center;
        mainVBox.AddChild(bottomHBox);

        var backBtn = new Button
        {
            Text = "◀  SPÄŤ DO HLAVNÉHO MENU",
            CustomMinimumSize = new Vector2(260, 42)
        };
        backBtn.AddThemeFontSizeOverride("font_size", 14);
        backBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(
            new Color(0.22f, 0.22f, 0.26f), new Color(0.5f, 0.5f, 0.5f, 0.6f), 2, 6));
        backBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(
            new Color(0.3f, 0.3f, 0.36f), Colors.White, 2, 6));
        backBtn.Pressed += () =>
        {
            AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
            CivNetworkManager.Instance.Disconnect();
            QueueFree();
            OnBackToTitle?.Invoke();
        };
        bottomHBox.AddChild(backBtn);
    }

    private Control BuildHotseatTab()
    {
        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(vbox);

        var desc = new Label
        {
            Text = "Hrajte ťahovú hru pre 2 hráčov na jednom počítači. Pri striedaní ťahov sa zobrazí obrazovka odovzdania ťahu.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.8f));
        vbox.AddChild(desc);

        // Players Grid
        var playersHBox = new HBoxContainer();
        playersHBox.AddThemeConstantOverride("separation", 24);
        playersHBox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(playersHBox);

        // Player 1 Box
        var p1Box = new VBoxContainer();
        p1Box.AddThemeConstantOverride("separation", 6);
        playersHBox.AddChild(p1Box);

        var p1Title = new Label { Text = "🔵 Hráč 1 (Hostiteľ)" };
        p1Title.AddThemeFontSizeOverride("font_size", 14);
        p1Title.AddThemeColorOverride("font_color", new Color(0.3f, 0.7f, 1.0f));
        p1Box.AddChild(p1Title);

        _p1NameInput = new LineEdit { Text = "César", CustomMinimumSize = new Vector2(200, 32) };
        p1Box.AddChild(_p1NameInput);

        _p1CivInput = new OptionButton { CustomMinimumSize = new Vector2(200, 32) };
        PopulateCivDropdown(_p1CivInput, "rome");
        p1Box.AddChild(_p1CivInput);

        // Player 2 Box
        var p2Box = new VBoxContainer();
        p2Box.AddThemeConstantOverride("separation", 6);
        playersHBox.AddChild(p2Box);

        var p2Title = new Label { Text = "🔴 Hráč 2 (Rival)" };
        p2Title.AddThemeFontSizeOverride("font_size", 14);
        p2Title.AddThemeColorOverride("font_color", new Color(1.0f, 0.4f, 0.4f));
        p2Box.AddChild(p2Title);

        _p2NameInput = new LineEdit { Text = "Chammurapi", CustomMinimumSize = new Vector2(200, 32) };
        p2Box.AddChild(_p2NameInput);

        _p2CivInput = new OptionButton { CustomMinimumSize = new Vector2(200, 32) };
        PopulateCivDropdown(_p2CivInput, "babylon");
        p2Box.AddChild(_p2CivInput);

        vbox.AddChild(new HSeparator());

        // Map Sizes
        var sizeTitle = new Label { Text = "Veľkosť mapy:", HorizontalAlignment = HorizontalAlignment.Center };
        sizeTitle.AddThemeFontSizeOverride("font_size", 13);
        sizeTitle.AddThemeColorOverride("font_color", new Color(0.92f, 0.75f, 0.3f));
        vbox.AddChild(sizeTitle);

        var sizesHBox = new HBoxContainer();
        sizesHBox.AddThemeConstantOverride("separation", 8);
        sizesHBox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(sizesHBox);

        _sizeButtons = new Button[MapSizes.Length];
        for (int i = 0; i < MapSizes.Length; i++)
        {
            int idx = i;
            var btn = new Button
            {
                Text = $"{MapSizes[i].Name}\n({MapSizes[i].Width}x{MapSizes[i].Height})",
                CustomMinimumSize = new Vector2(90, 42)
            };
            btn.AddThemeFontSizeOverride("font_size", 12);
            btn.Pressed += () => SelectSize(idx);
            sizesHBox.AddChild(btn);
            _sizeButtons[i] = btn;
        }
        SelectSize(_selectedSizeIndex);

        // World Parameters Grid
        var geoGrid = new GridContainer { Columns = 4 };
        geoGrid.AddThemeConstantOverride("h_separation", 10);
        geoGrid.AddThemeConstantOverride("v_separation", 8);
        vbox.AddChild(geoGrid);

        geoGrid.AddChild(new Label { Text = "Voda:" });
        _waterInput = new OptionButton();
        _waterInput.AddItem("60%"); _waterInput.AddItem("70%"); _waterInput.AddItem("80%"); _waterInput.AddItem("🎲 Náhodne");
        _waterInput.Selected = 1;
        geoGrid.AddChild(_waterInput);

        geoGrid.AddChild(new Label { Text = "Podnebie:" });
        _climateInput = new OptionButton();
        _climateInput.AddItem("Suché"); _climateInput.AddItem("Normálne"); _climateInput.AddItem("Vlhké"); _climateInput.AddItem("🎲 Náhodne");
        _climateInput.Selected = 1;
        geoGrid.AddChild(_climateInput);

        geoGrid.AddChild(new Label { Text = "Teplota:" });
        _tempInput = new OptionButton();
        _tempInput.AddItem("Teplá"); _tempInput.AddItem("Mierna"); _tempInput.AddItem("Chladná"); _tempInput.AddItem("🎲 Náhodne");
        _tempInput.Selected = 1;
        geoGrid.AddChild(_tempInput);

        geoGrid.AddChild(new Label { Text = "Vek zeme:" });
        _ageInput = new OptionButton();
        _ageInput.AddItem("3 Mld."); _ageInput.AddItem("4 Mld."); _ageInput.AddItem("5 Mld."); _ageInput.AddItem("🎲 Náhodne");
        _ageInput.Selected = 1;
        geoGrid.AddChild(_ageInput);

        // Difficulty & Seed
        var miscRow = new HBoxContainer();
        miscRow.AddThemeConstantOverride("separation", 16);
        miscRow.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(miscRow);

        miscRow.AddChild(new Label { Text = "Obtiažnosť:" });
        _diffInput = new OptionButton();
        foreach (var lvl in DifficultySettings.AllLevels)
        {
            _diffInput.AddItem(DifficultySettings.Get(lvl).DisplayName);
        }
        _diffInput.Selected = 2; // Regent
        miscRow.AddChild(_diffInput);

        miscRow.AddChild(new Label { Text = "Seed:" });
        _seedInput = new SpinBox { MinValue = 1, MaxValue = 99999, Value = _seed };
        miscRow.AddChild(_seedInput);

        var randBtn = new Button { Text = "🎲" };
        randBtn.Pressed += () => { _seed = new Random().Next(1, 99999); _seedInput.Value = _seed; };
        miscRow.AddChild(randBtn);

        // Start Hotseat Button
        var startHotseatBtn = new Button
        {
            Text = "⚔️  ZAČAŤ HOTSEAT HRU  ⚔️",
            CustomMinimumSize = new Vector2(320, 50),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        startHotseatBtn.AddThemeFontSizeOverride("font_size", 16);
        startHotseatBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(
            new Color(0.12f, 0.55f, 0.22f), new Color(0.95f, 0.72f, 0.12f), 2, 6));
        startHotseatBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(
            new Color(0.16f, 0.65f, 0.28f), Colors.White, 2, 6));
        startHotseatBtn.Pressed += OnStartHotseatClicked;
        vbox.AddChild(startHotseatBtn);

        return scroll;
    }

    private Control BuildNetworkTab()
    {
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 14);

        var netDesc = new Label
        {
            Text = "Hrajte cez lokálnu sieť (LAN) alebo priamu IP adresu. Jeden hráč založí server (Host) a druhý sa pripojí (Join).",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        netDesc.AddThemeFontSizeOverride("font_size", 12);
        netDesc.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.8f));
        vbox.AddChild(netDesc);

        var splitHBox = new HBoxContainer();
        splitHBox.AddThemeConstantOverride("separation", 20);
        splitHBox.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(splitHBox);

        // HOST COLUMN
        var hostPanel = new PanelContainer();
        hostPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hostPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.12f, 0.12f, 0.16f), new Color(0.3f, 0.6f, 1.0f), 2, 6));
        splitHBox.AddChild(hostPanel);

        var hostMargin = new MarginContainer();
        hostMargin.AddThemeConstantOverride("margin_left", 12);
        hostMargin.AddThemeConstantOverride("margin_right", 12);
        hostMargin.AddThemeConstantOverride("margin_top", 12);
        hostMargin.AddThemeConstantOverride("margin_bottom", 12);
        hostPanel.AddChild(hostMargin);

        var hostVBox = new VBoxContainer();
        hostVBox.AddThemeConstantOverride("separation", 8);
        hostMargin.AddChild(hostVBox);

        var hostHeader = new Label { Text = "📡 ZALOŽIŤ SERVER (HOST)" };
        hostHeader.AddThemeFontSizeOverride("font_size", 15);
        hostHeader.AddThemeColorOverride("font_color", new Color(0.3f, 0.7f, 1.0f));
        hostVBox.AddChild(hostHeader);

        hostVBox.AddChild(new Label { Text = "Meno Hostiteľa:" });
        _netHostNameInput = new LineEdit { Text = "Hostiteľ (Hráč 1)" };
        hostVBox.AddChild(_netHostNameInput);

        hostVBox.AddChild(new Label { Text = "Civilizácia:" });
        _netHostCivInput = new OptionButton();
        PopulateCivDropdown(_netHostCivInput, "rome");
        hostVBox.AddChild(_netHostCivInput);

        var portHBox = new HBoxContainer();
        portHBox.AddChild(new Label { Text = "Port:" });
        _netHostPortInput = new SpinBox { MinValue = 1024, MaxValue = 65535, Value = 7777 };
        portHBox.AddChild(_netHostPortInput);
        hostVBox.AddChild(portHBox);

        _netStartHostBtn = new Button { Text = "▶ Spustiť Server" };
        _netStartHostBtn.Pressed += OnStartHostClicked;
        hostVBox.AddChild(_netStartHostBtn);

        _netHostStatusLabel = new Label
        {
            Text = "Server nebeží.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _netHostStatusLabel.AddThemeFontSizeOverride("font_size", 11);
        _netHostStatusLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        hostVBox.AddChild(_netHostStatusLabel);

        _netLaunchHostGameBtn = new Button
        {
            Text = "🚀 SPUSTIŤ SIEŤOVÚ HRU 🚀",
            Disabled = true,
            CustomMinimumSize = new Vector2(0, 42)
        };
        _netLaunchHostGameBtn.Pressed += OnLaunchNetworkGameClicked;
        hostVBox.AddChild(_netLaunchHostGameBtn);

        // JOIN COLUMN
        var joinPanel = new PanelContainer();
        joinPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        joinPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.12f, 0.12f, 0.16f), new Color(1.0f, 0.4f, 0.4f), 2, 6));
        splitHBox.AddChild(joinPanel);

        var joinMargin = new MarginContainer();
        joinMargin.AddThemeConstantOverride("margin_left", 12);
        joinMargin.AddThemeConstantOverride("margin_right", 12);
        joinMargin.AddThemeConstantOverride("margin_top", 12);
        joinMargin.AddThemeConstantOverride("margin_bottom", 12);
        joinPanel.AddChild(joinMargin);

        var joinVBox = new VBoxContainer();
        joinVBox.AddThemeConstantOverride("separation", 8);
        joinMargin.AddChild(joinVBox);

        var joinHeader = new Label { Text = "🔗 PRIPOJIŤ SA (CLIENT)" };
        joinHeader.AddThemeFontSizeOverride("font_size", 15);
        joinHeader.AddThemeColorOverride("font_color", new Color(1.0f, 0.4f, 0.4f));
        joinVBox.AddChild(joinHeader);

        joinVBox.AddChild(new Label { Text = "Meno Klienta:" });
        _netJoinNameInput = new LineEdit { Text = "Hosť (Hráč 2)" };
        joinVBox.AddChild(_netJoinNameInput);

        joinVBox.AddChild(new Label { Text = "Civilizácia:" });
        _netJoinCivInput = new OptionButton();
        PopulateCivDropdown(_netJoinCivInput, "babylon");
        joinVBox.AddChild(_netJoinCivInput);

        var ipHBox = new HBoxContainer();
        ipHBox.AddChild(new Label { Text = "IP:" });
        _netJoinIpInput = new LineEdit { Text = "127.0.0.1", CustomMinimumSize = new Vector2(120, 0) };
        ipHBox.AddChild(_netJoinIpInput);
        joinVBox.AddChild(ipHBox);

        var joinPortHBox = new HBoxContainer();
        joinPortHBox.AddChild(new Label { Text = "Port:" });
        _netJoinPortInput = new SpinBox { MinValue = 1024, MaxValue = 65535, Value = 7777 };
        joinPortHBox.AddChild(_netJoinPortInput);
        joinVBox.AddChild(joinPortHBox);

        _netConnectBtn = new Button { Text = "▶ Pripojiť k Serveru" };
        _netConnectBtn.Pressed += OnConnectToHostClicked;
        joinVBox.AddChild(_netConnectBtn);

        _netJoinStatusLabel = new Label
        {
            Text = "Nepripojený.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _netJoinStatusLabel.AddThemeFontSizeOverride("font_size", 11);
        _netJoinStatusLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        joinVBox.AddChild(_netJoinStatusLabel);

        // Listen to Network Events
        CivNetworkManager.Instance.OnStatusMessage += msg =>
        {
            if (CivNetworkManager.Instance.Role == NetworkRole.Host)
                _netHostStatusLabel.Text = msg;
            else if (CivNetworkManager.Instance.Role == NetworkRole.Client)
                _netJoinStatusLabel.Text = msg;
        };

        CivNetworkManager.Instance.OnPeerConnected += (name, civ) =>
        {
            if (CivNetworkManager.Instance.Role == NetworkRole.Host)
            {
                _netLaunchHostGameBtn.Disabled = false;
                _netHostStatusLabel.Text = $"✅ Klient pripojený: {name} ({civ})! Pripravený na štart.";
            }
            else
            {
                _netJoinStatusLabel.Text = $"✅ Úspešne pripojený k hostiteľovi {name} ({civ})! Čaká sa na spustenie.";
            }
        };

        CivNetworkManager.Instance.OnError += err =>
        {
            if (CivNetworkManager.Instance.Role == NetworkRole.Host)
                _netHostStatusLabel.Text = $"❌ {err}";
            else
                _netJoinStatusLabel.Text = $"❌ {err}";
        };

        CivNetworkManager.Instance.OnGameStartReceived += (dto) =>
        {
            // Client received game start!
            var options = new GameSetupOptions
            {
                Mode = GameMode.NetworkClient,
                Player1Name = dto.Player1Name,
                Player2Name = dto.Player2Name,
                PlayerCivId = dto.PlayerCivId,
                AiCivId = dto.AiCivId
            };
            QueueFree();
            OnStartNetworkGame?.Invoke(options);
        };

        return vbox;
    }

    public override void _Process(double delta)
    {
        CivNetworkManager.Instance.PollMessages();
    }

    private void SelectSize(int index)
    {
        _selectedSizeIndex = index;
        for (int i = 0; i < _sizeButtons.Length; i++)
        {
            bool sel = i == index;
            _sizeButtons[i].AddThemeStyleboxOverride("normal", CreatePanelStyle(
                sel ? new Color(0.18f, 0.42f, 0.65f) : new Color(0.15f, 0.15f, 0.2f),
                sel ? new Color(0.95f, 0.72f, 0.12f) : new Color(0.4f, 0.4f, 0.4f, 0.5f),
                sel ? 2 : 1, 5));
        }
    }

    private void PopulateCivDropdown(OptionButton optionBtn, string preselectId)
    {
        optionBtn.AddItem("🎲 Náhodná Civilizácia");
        optionBtn.SetItemMetadata(0, "random");

        int idx = 1;
        int selected = 0;
        foreach (var civ in CivilizationRegistry.All.Values)
        {
            optionBtn.AddItem($"{civ.Name} ({civ.LeaderName})");
            optionBtn.SetItemMetadata(idx, civ.Id);
            if (civ.Id == preselectId) selected = idx;
            idx++;
        }
        optionBtn.Selected = selected;
    }

    private void OnStartHotseatClicked()
    {
        AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");

        var opt = MapSizes[_selectedSizeIndex];
        string p1Civ = (string)_p1CivInput.GetItemMetadata(_p1CivInput.Selected);
        string p2Civ = (string)_p2CivInput.GetItemMetadata(_p2CivInput.Selected);

        var options = new GameSetupOptions
        {
            Mode = GameMode.Hotseat,
            Width = opt.Width,
            Height = opt.Height,
            Seed = (int)_seedInput.Value,
            Player1Name = string.IsNullOrWhiteSpace(_p1NameInput.Text) ? "Hráč 1" : _p1NameInput.Text.Trim(),
            Player2Name = string.IsNullOrWhiteSpace(_p2NameInput.Text) ? "Hráč 2" : _p2NameInput.Text.Trim(),
            PlayerCivId = p1Civ,
            AiCivId = p2Civ,
            Difficulty = _diffInput.Selected >= 0 ? DifficultySettings.AllLevels[_diffInput.Selected] : DifficultyLevel.Regent
        };

        QueueFree();
        OnStartHotseatGame?.Invoke(options);
    }

    private void OnStartHostClicked()
    {
        AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
        int port = (int)_netHostPortInput.Value;
        string name = string.IsNullOrWhiteSpace(_netHostNameInput.Text) ? "Host" : _netHostNameInput.Text.Trim();
        string civId = (string)_netHostCivInput.GetItemMetadata(_netHostCivInput.Selected);

        CivNetworkManager.Instance.StartHost(port, name, civId);
        _netStartHostBtn.Disabled = true;
    }

    private void OnConnectToHostClicked()
    {
        AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
        string ip = _netJoinIpInput.Text.Trim();
        int port = (int)_netJoinPortInput.Value;
        string name = string.IsNullOrWhiteSpace(_netJoinNameInput.Text) ? "Client" : _netJoinNameInput.Text.Trim();
        string civId = (string)_netJoinCivInput.GetItemMetadata(_netJoinCivInput.Selected);

        CivNetworkManager.Instance.ConnectToHost(ip, port, name, civId);
        _netConnectBtn.Disabled = true;
    }

    private void OnLaunchNetworkGameClicked()
    {
        AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");

        var opt = MapSizes[_selectedSizeIndex];
        string hostCiv = (string)_netHostCivInput.GetItemMetadata(_netHostCivInput.Selected);
        string clientCiv = CivNetworkManager.Instance.RemoteCivId;
        if (string.IsNullOrEmpty(clientCiv) || clientCiv == "random") clientCiv = "babylon";

        var options = new GameSetupOptions
        {
            Mode = GameMode.NetworkHost,
            Width = opt.Width,
            Height = opt.Height,
            Seed = (int)_seedInput.Value,
            Player1Name = _netHostNameInput.Text.Trim(),
            Player2Name = CivNetworkManager.Instance.RemotePlayerName,
            PlayerCivId = hostCiv,
            AiCivId = clientCiv,
            Difficulty = DifficultyLevel.Regent
        };

        QueueFree();
        OnStartNetworkGame?.Invoke(options);
    }

    private static StyleBoxFlat CreatePanelStyle(Color bg, Color border, float borderWidth = 2, float cornerRadius = 6)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = (int)borderWidth,
            BorderWidthTop = (int)borderWidth,
            BorderWidthRight = (int)borderWidth,
            BorderWidthBottom = (int)borderWidth,
            CornerRadiusTopLeft = (int)cornerRadius,
            CornerRadiusTopRight = (int)cornerRadius,
            CornerRadiusBottomLeft = (int)cornerRadius,
            CornerRadiusBottomRight = (int)cornerRadius
        };
    }
}
