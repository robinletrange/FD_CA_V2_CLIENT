using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace CLIENT.Controllers;

[ApiController]
[Route("ws")]
public class WebSocketController : ControllerBase
{
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(
        ILogger<WebSocketController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task Connect(CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync("WebSocket connection required.", cancellationToken);
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        _logger.LogInformation("Connexion WebSocket établie.");

        var buffer = new byte[4096];

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Fermeture de la connexion WebSocket.");

                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Fermeture demandée", cancellationToken);

                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    _logger.LogInformation("Message reçu : {Message}", message);

                    // Réponse temporaire
                    var response = Encoding.UTF8.GetBytes("{\"type\":\"status\",\"online\":true}");

                    await webSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Connexion WebSocket annulée.");
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "Connexion WebSocket interrompue.");
        }
    }
}