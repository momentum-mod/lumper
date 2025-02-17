namespace Lumper.UI.Services;

using System;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public sealed class GameSyncService : ReactiveObject, IDisposable
{
    public static GameSyncService Instance { get; } = new();

    public enum SyncStatus
    {
        Connected,
        Disconnected,
        Connecting,
        Disconnecting,
    }

    [Reactive]
    public SyncStatus Status { get; private set; } = SyncStatus.Disconnected;

    [ObservableAsProperty]
    public bool Connected { get; set; }

    [Reactive]
    public string? PlayerPosition { get; private set; }

    [Reactive]
    public string? TargetEntities { get; private set; }

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private ClientWebSocket? _client;

    private const ushort Port = 42124;

    // Very large (1MB) message size so we never have to handle multi-frame messages. Same constant in C++.
    private const uint MaxMessageLength = 1024 * 1024;

    private GameSyncService() =>
        this.WhenAnyValue(x => x.Status).Select(x => x == SyncStatus.Connected).ToPropertyEx(this, x => x.Connected);

    public void TeleportToOrigin(string origin) =>
        _ = PostMessage(new Message { Type = MessageType.CTS_TeleportToLocation, Content = origin });

    public void ToggleConnection()
    {
        if (Status == SyncStatus.Disconnected)
            _ = Connect();
        else if (Status == SyncStatus.Connected)
            _ = Disconnect();
    }

    private async Task Connect()
    {
        if (_client is not null)
            throw new InvalidOperationException("Tried to connect while already connected.");

        _client = new ClientWebSocket();

        _logger.Info("Connecting to Game Sync...");
        Status = SyncStatus.Connecting;

        try
        {
            await _client.ConnectAsync(new Uri($"ws://localhost:{Port}"), CancellationToken.None);
        }
        catch
        {
            await Disconnect();
            _logger.Error("Game Sync connection failed. Do you have a sync server running? (mom_lumper_sync_enable 1)");
            return;
        }

        _logger.Info("Game Sync connected.");

        _ = Observable.Start(ReceiveIncomingMessages, RxApp.TaskpoolScheduler);

        Status = SyncStatus.Connected;
    }

    private async Task Disconnect()
    {
        if (_client is null)
            return;

        Status = SyncStatus.Disconnecting;

        if (_client.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
            _logger.Info("Game Sync disconnected.");
        }
        else if (_client?.State == WebSocketState.Connecting)
        {
            _client.Abort();
            _logger.Info("Game Sync connect attempt cancelled.");
        }

        _client?.Dispose();
        _client = null;
        Status = SyncStatus.Disconnected;
    }

    private async Task ReceiveIncomingMessages()
    {
        if (_client is null)
            return;

        Memory<byte> buffer = new byte[MaxMessageLength];
        while (_client.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            ValueWebSocketReceiveResult result = await _client.ReceiveAsync(buffer, CancellationToken.None);
            if (!result.EndOfMessage)
            {
                _logger.Error($"Received partial message ({result.Count} bytes), which is not supported. Discarding.");
                continue;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                HandleMessage(buffer.Span[..result.Count]);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await Disconnect();
                break;
            }
            else
            {
                _logger.Error("Received message of unknown type, discarding.");
            }
        }
    }

    private void HandleMessage(ReadOnlySpan<byte> messageData)
    {
        string messageString = Encoding.UTF8.GetString(messageData);

        Message? message = JsonConvert.DeserializeObject<Message>(messageString);
        if (message is null)
        {
            _logger.Error($"Received invalid message {messageString}, discarding.");
            return;
        }

        _logger.Debug($"Received message: {messageString}");

        switch (message.Type)
        {
            case MessageType.STC_CurrentPosition:
                PlayerPosition = message.Content;
                break;
            case MessageType.STC_TargetedEntitiesList:
                TargetEntities = message.Content;
                break;
            default:
                _logger.Error($"Message type {message.Type} not handled by client, discarding.");
                break;
        }
    }

    private async Task PostMessage(Message message)
    {
        if (_client?.State != WebSocketState.Open)
            return;

        string str = JsonConvert.SerializeObject(message);

        _logger.Debug($"Sending message: {str}");

        await _client.SendAsync(Encoding.UTF8.GetBytes(str), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    // Corresponds to LumperSyncMessageType in C++
    // CTS: Client (Us) -> Server (Game), STC: Server -> Client
    private enum MessageType
    {
        INVALID = 0,
        STC_CurrentPosition = 1,
        STC_TargetedEntitiesList = 2,
        CTS_AddStripperConfig = 3,
        CTS_TeleportToLocation = 4,
    }

    private sealed record Message
    {
        [JsonProperty("type")]
        public required MessageType Type { get; set; }

        [JsonProperty("content")]
        public required string Content { get; set; }
    }

    public void Dispose() => _client?.Dispose();
}
