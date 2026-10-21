using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CivGame.Core;

namespace CivGame.Network;

public enum NetworkRole
{
    None,
    Host,
    Client
}

public class NetworkPacket
{
    public string Type { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string SenderCivId { get; set; } = "";
    public string Payload { get; set; } = "";
}

public class CivNetworkManager
{
    private static CivNetworkManager? _instance;
    public static CivNetworkManager Instance => _instance ??= new CivNetworkManager();

    public NetworkRole Role { get; private set; } = NetworkRole.None;
    public bool IsConnected => _client != null && _client.Connected;
    public string LocalPlayerName { get; set; } = "Player";
    public string LocalCivId { get; set; } = "rome";
    public string RemotePlayerName { get; set; } = "";
    public string RemoteCivId { get; set; } = "";
    public SaveDataDto? LastReceivedStartDto { get; set; }

    public event Action<string, string>? OnPeerConnected;
    public event Action<string>? OnStatusMessage;
    public event Action<string>? OnError;
    public event Action<SaveDataDto>? OnGameStartReceived;
    public event Action<SaveDataDto>? OnTurnStateReceived;
    public event Action<string, string>? OnChatMessageReceived;
    public event Action? OnDisconnected;

    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentQueue<Action> _mainThreadQueue = new();

    public void StartHost(int port, string playerName, string civId)
    {
        Disconnect();
        Role = NetworkRole.Host;
        LocalPlayerName = playerName;
        LocalCivId = civId;

        _cts = new CancellationTokenSource();
        Task.Run(() => RunHostLoop(port, _cts.Token));
    }

    public void ConnectToHost(string ip, int port, string playerName, string civId)
    {
        Disconnect();
        Role = NetworkRole.Client;
        LocalPlayerName = playerName;
        LocalCivId = civId;

        _cts = new CancellationTokenSource();
        Task.Run(() => RunClientLoop(ip, port, _cts.Token));
    }

    private async Task RunHostLoop(int port, CancellationToken token)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            QueueStatus($"[Server] Počúvam na porte {port}... Čakám na pripojenie klienta.");

            while (!token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                _client = client;
                _stream = client.GetStream();
                QueueStatus("[Server] Klient sa úspešne pripojil. Prebieha handshake...");

                // Send HELLO
                SendPacketDirect(new NetworkPacket
                {
                    Type = "HELLO",
                    SenderName = LocalPlayerName,
                    SenderCivId = LocalCivId
                });

                // Start read loop
                await ProcessStreamLoop(token);
                break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            QueueError($"Chyba host servera: {ex.Message}");
        }
    }

    private async Task RunClientLoop(string ip, int port, CancellationToken token)
    {
        try
        {
            QueueStatus($"[Client] Pripájam k {ip}:{port}...");
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port, token);
            _stream = _client.GetStream();
            QueueStatus("[Client] Spojenie nadviazané. Prebieha handshake...");

            // Send HELLO
            SendPacketDirect(new NetworkPacket
            {
                Type = "HELLO",
                SenderName = LocalPlayerName,
                SenderCivId = LocalCivId
            });

            // Start read loop
            await ProcessStreamLoop(token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            QueueError($"Chyba pripojenia ku klientovi: {ex.Message}");
        }
    }

    private async Task ProcessStreamLoop(CancellationToken token)
    {
        var lengthBuffer = new byte[4];

        while (!token.IsCancellationRequested && _stream != null && _client != null && _client.Connected)
        {
            // Read 4-byte length prefix
            int bytesRead = await ReadExactBytesAsync(_stream, lengthBuffer, 0, 4, token);
            if (bytesRead < 4) break;

            int packetLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer, 0));
            if (packetLength <= 0 || packetLength > 10 * 1024 * 1024) break; // 10MB limit

            var bodyBuffer = new byte[packetLength];
            bytesRead = await ReadExactBytesAsync(_stream, bodyBuffer, 0, packetLength, token);
            if (bytesRead < packetLength) break;

            string json = Encoding.UTF8.GetString(bodyBuffer);
            var packet = JsonSerializer.Deserialize<NetworkPacket>(json);
            if (packet != null)
            {
                HandleIncomingPacket(packet);
            }
        }

        QueueDisconnected();
    }

    private static async Task<int> ReadExactBytesAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken token)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), token);
            if (read == 0) return totalRead; // End of stream
            totalRead += read;
        }
        return totalRead;
    }

    private void HandleIncomingPacket(NetworkPacket packet)
    {
        switch (packet.Type)
        {
            case "HELLO":
                RemotePlayerName = packet.SenderName;
                RemoteCivId = packet.SenderCivId;
                _mainThreadQueue.Enqueue(() =>
                {
                    OnPeerConnected?.Invoke(packet.SenderName, packet.SenderCivId);
                    OnStatusMessage?.Invoke($"✅ Protihráč pripojený: {packet.SenderName} ({packet.SenderCivId})");
                });
                break;

            case "GAME_START":
                var startDto = JsonSaveSystem.DeserializeDto(packet.Payload);
                if (startDto != null)
                {
                    LastReceivedStartDto = startDto;
                    _mainThreadQueue.Enqueue(() => OnGameStartReceived?.Invoke(startDto));
                }
                break;

            case "TURN_STATE":
                var turnDto = JsonSaveSystem.DeserializeDto(packet.Payload);
                if (turnDto != null)
                {
                    _mainThreadQueue.Enqueue(() => OnTurnStateReceived?.Invoke(turnDto));
                }
                break;

            case "CHAT":
                _mainThreadQueue.Enqueue(() => OnChatMessageReceived?.Invoke(packet.SenderName, packet.Payload));
                break;
        }
    }

    public void SendGameStart(SaveDataDto state)
    {
        string json = JsonSaveSystem.SerializeDto(state);
        SendPacketDirect(new NetworkPacket
        {
            Type = "GAME_START",
            SenderName = LocalPlayerName,
            SenderCivId = LocalCivId,
            Payload = json
        });
    }

    public void SendTurnState(SaveDataDto state)
    {
        string json = JsonSaveSystem.SerializeDto(state);
        SendPacketDirect(new NetworkPacket
        {
            Type = "TURN_STATE",
            SenderName = LocalPlayerName,
            SenderCivId = LocalCivId,
            Payload = json
        });
    }

    public void SendChatMessage(string message)
    {
        SendPacketDirect(new NetworkPacket
        {
            Type = "CHAT",
            SenderName = LocalPlayerName,
            SenderCivId = LocalCivId,
            Payload = message
        });
    }

    private void SendPacketDirect(NetworkPacket packet)
    {
        try
        {
            if (_stream == null || _client == null || !_client.Connected) return;

            string json = JsonSerializer.Serialize(packet);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(json);
            byte[] lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bodyBytes.Length));

            lock (_stream)
            {
                _stream.Write(lengthBytes, 0, lengthBytes.Length);
                _stream.Write(bodyBytes, 0, bodyBytes.Length);
                _stream.Flush();
            }
        }
        catch (Exception ex)
        {
            QueueError($"Chyba odosielania paketu: {ex.Message}");
        }
    }

    public void PollMessages()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Network Poll Error] {ex.Message}");
            }
        }
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        try { _listener?.Stop(); } catch { }

        _stream = null;
        _client = null;
        _listener = null;
        Role = NetworkRole.None;
    }

    private void QueueStatus(string message) => _mainThreadQueue.Enqueue(() => OnStatusMessage?.Invoke(message));
    private void QueueError(string message) => _mainThreadQueue.Enqueue(() => OnError?.Invoke(message));
    private void QueueDisconnected() => _mainThreadQueue.Enqueue(() => OnDisconnected?.Invoke());
}
