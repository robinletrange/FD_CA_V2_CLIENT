using System.Text.Json;

namespace CLIENT.Services;

public class DoorSimulationService : BackgroundService
{
    private readonly WebSocketManager _manager;
    private readonly ILogger<DoorSimulationService> _logger;

    private readonly Random _random = new();

    public DoorSimulationService(WebSocketManager manager, ILogger<DoorSimulationService> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_random.Next(3000, 10000), stoppingToken);

            var isOpen = _random.Next(2) == 1;

            var message = new
            {
                type = "door_state",
                door = new
                {
                    id = "DOOR-001",
                    name = "Porte principale",
                    state = isOpen ? "open" : "closed",
                    timestamp = DateTime.UtcNow
                }
            };

            var json = JsonSerializer.Serialize(message);

            _logger.LogInformation("Porte {State}", isOpen ? "OUVERTE" : "FERMÉE");

            await _manager.BroadcastAsync(json, stoppingToken);
        }
    }
}