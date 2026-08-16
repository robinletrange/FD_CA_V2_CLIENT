using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace CLIENT.Controllers;

[ApiController]
[Route("ws")]
public class WebSocketController : ControllerBase
{
    private readonly Services.WebSocketManager _manager;

    public WebSocketController(Services.WebSocketManager manager)
    {
        _manager = manager;
    }

    [HttpGet]
    public async Task Connect(CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        _manager.Add(socket);

        try
        {
            var buffer = new byte[4096];

            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        finally
        {
            _manager.Remove(socket);
        }
    }
}