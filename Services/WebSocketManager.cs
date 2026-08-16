using System.Net.WebSockets;
using System.Text;

namespace CLIENT.Services;

public class WebSocketManager
{
    private readonly List<WebSocket> _connections = new();

    private readonly object _lock = new();

    public void Add(WebSocket socket)
    {
        lock (_lock)
        {
            _connections.Add(socket);
        }
    }

    public void Remove(WebSocket socket)
    {
        lock (_lock)
        {
            _connections.Remove(socket);
        }
    }

    public async Task BroadcastAsync(string message, CancellationToken cancellationToken)
    {
        var data = Encoding.UTF8.GetBytes(message);

        List<WebSocket> connections;

        lock (_lock)
        {
            connections = _connections.ToList();
        }

        foreach (var socket in connections)
        {
            if (socket.State != WebSocketState.Open)
                continue;

            await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cancellationToken);
        }
    }
}