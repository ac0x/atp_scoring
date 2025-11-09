using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backend.Hubs;
using Backend.SmartDirectorListener.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.SmartDirectorListener;

public class SmartDirectorWorker : BackgroundService
{
    private readonly ILogger<SmartDirectorWorker> _logger;
    private readonly SdDecoder _decoder;
    private readonly IHubContext<LiveHub> _hubContext;
    private readonly SmartDirectorOptions _options;
    private readonly ConcurrentDictionary<Guid, Task> _clientTasks = new();

    public SmartDirectorWorker(
        ILogger<SmartDirectorWorker> logger,
        SdDecoder decoder,
        IHubContext<LiveHub> hubContext,
        IOptions<SmartDirectorOptions> options)
    {
        _logger = logger;
        _decoder = decoder;
        _hubContext = hubContext;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SmartDirector worker starting (bind={Bind}, port={Port})", _options.BindAddress, _options.Port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var listener = CreateListener();
                try
                {
                    listener.Start();
                    _logger.LogInformation("SmartDirector listener ready on {Address}:{Port}", _options.BindAddress, _options.Port);
                    await AcceptLoopAsync(listener, stoppingToken);
                }
                finally
                {
                    listener.Stop();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SmartDirector listener faulted, retrying in {DelaySeconds}s", _options.ReconnectDelaySeconds);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        await Task.WhenAll(_clientTasks.Values);
        _logger.LogInformation("SmartDirector worker stopped");
    }

    private TcpListener CreateListener()
    {
        if (!IPAddress.TryParse(_options.BindAddress, out var ipAddress))
        {
            ipAddress = IPAddress.Any;
        }

        return new TcpListener(ipAddress, _options.Port);
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AcceptTcpClientAsync failed");
                continue;
            }

            if (client == null)
            {
                continue;
            }

            var id = Guid.NewGuid();
            var task = HandleClientAsync(id, client, cancellationToken);
            _clientTasks[id] = task;
            _ = task.ContinueWith(_ =>
            {
                _clientTasks.TryRemove(id, out _);
                client.Dispose();
            }, CancellationToken.None);
        }
    }

    private async Task HandleClientAsync(Guid id, TcpClient client, CancellationToken cancellationToken)
    {
        var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogInformation("SmartDirector client connected: {Endpoint}", endpoint);

        using var stream = client.GetStream();
        var buffer = new byte[_options.BufferSize];
        using var memoryStream = new MemoryStream();
        var lastReceived = DateTime.UtcNow;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!stream.DataAvailable)
                {
                    if (memoryStream.Length > 0 && (DateTime.UtcNow - lastReceived).TotalMilliseconds > _options.FrameFlushThresholdMs)
                    {
                        await ProcessChunkAsync(memoryStream.ToArray(), cancellationToken);
                        memoryStream.SetLength(0);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(_options.PollDelayMs), cancellationToken);
                    continue;
                }

                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                lastReceived = DateTime.UtcNow;
                memoryStream.Write(buffer, 0, read);
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SmartDirector client {Endpoint} faulted", endpoint);
        }
        finally
        {
            if (memoryStream.Length > 0)
            {
                await ProcessChunkAsync(memoryStream.ToArray(), cancellationToken);
            }

            _logger.LogInformation("SmartDirector client disconnected: {Endpoint}", endpoint);
        }
    }

    private async Task ProcessChunkAsync(byte[] data, CancellationToken cancellationToken)
    {
        string text;
        try
        {
            text = Encoding.UTF8.GetString(data);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to decode SmartDirector chunk as UTF-8");
            return;
        }

        var emitted = 0;
        foreach (var snapshot in _decoder.DecodePackets(text))
        {
            var message = ToLiveSnapshotMessage(snapshot);
            try
            {
                await _hubContext.Clients.All.SendAsync("Snapshot", message, cancellationToken);
                emitted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to emit SmartDirector frame");
            }
        }

        if (emitted > 0)
        {
            _logger.LogDebug("SmartDirector emitted {Count} frame(s)", emitted);
        }
    }

    private static LiveSnapshotMessage ToLiveSnapshotMessage(DecodedSnapshot snapshot)
    {
        var points = string.IsNullOrWhiteSpace(snapshot.PointsDisplay)
            ? string.Empty
            : snapshot.PointsDisplay;

        return new LiveSnapshotMessage
        {
            TeamA = snapshot.TeamA,
            TeamB = snapshot.TeamB,
            Sets = snapshot.Sets,
            Points = points,
            Server = snapshot.Server,
            Clock = snapshot.MatchClock
        };
    }
}