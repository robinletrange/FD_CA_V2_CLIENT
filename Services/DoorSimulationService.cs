using System.Text.Json;

namespace CLIENT.Services;

public class DoorSimulationService : BackgroundService
{
    private readonly IDoorRepository _repository;
    private readonly WebSocketManager _manager;
    private readonly ILogger<DoorSimulationService> _logger;

    private readonly Random _random = new();

    public DoorSimulationService(IDoorRepository repository, WebSocketManager manager, ILogger<DoorSimulationService> logger)
    {
        _repository = repository;
        _manager = manager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_random.Next(1000, 5000), stoppingToken);

            int doorId = _random.Next(2) + 1;

            Dictionary<int, Door> doors = (await _repository.GetAllAsync()).ToDictionary(d => d.Id);

            if (doors.TryGetValue(doorId, out Door? door) && door != null)
            {
                int newState;

                if (door.State == 1)
                {
                    // Si elle est ouverte → fermeture obligatoire
                    newState = 0;
                }
                else
                {
                    // Si elle est fermée → ouverture aléatoire
                    newState = _random.Next(2);
                }

                door.State = newState;

                await _repository.UpdateAsync(door);

                var message = new
                {
                    type = "door",
                    data = door
                };

                var json = JsonSerializer.Serialize(message);

                _logger.LogInformation($"Porte {door.Id} {(door.State == 1 ? "OUVERTE" : "FERMÉE")}");

                await _manager.BroadcastAsync(json, stoppingToken);
            }
        }
    }
}