using System.Text.Json;

namespace CLIENT.Services;

public class DoorSimulationService : BackgroundService
{
    private readonly IDoorRepository _repository;
    private readonly WebSocketManager _manager;
    private readonly ILogger<DoorSimulationService> _logger;
    private readonly HttpClient _httpClient;

    private readonly Random _random = new();

    // À remplacer par l'adresse de TON API d'autorisation
    private const string AccessApiUrl = "http://192.168.1.201:8223";

    public DoorSimulationService(IDoorRepository repository, WebSocketManager manager, ILogger<DoorSimulationService> logger, HttpClient httpClient)
    {
        _repository = repository;
        _manager = manager;
        _logger = logger;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_random.Next(1000, 5000), stoppingToken);

            int doorId = 14000000;

            Dictionary<int, Door> doors = (await _repository.GetAllAsync()).ToDictionary(d => d.Id);

            if (!doors.TryGetValue(doorId, out Door? door) || door == null)
                continue;

            // Porte ouverte → fermeture obligatoire
            if (door.State == 1)
            {
                door.State = 0;

                await _repository.UpdateAsync(door);

                var message = new
                {
                    type = "door",
                    data = door
                };

                var json = JsonSerializer.Serialize(message);

                _logger.LogInformation($"Porte {door.Id} FERMÉE");

                await _manager.BroadcastAsync(json, stoppingToken);

                continue;
            }

            // Porte fermée → demande d'autorisation
            string identifierValue = "CAFD6A06";

            string url = $"{AccessApiUrl}/doors/access" + $"?identifierValue={Uri.EscapeDataString(identifierValue)}" + $"&doorId={doorId}";

            try
            {
                _logger.LogInformation($"Demande d'autorisation pour porte {doorId}");

                using HttpResponseMessage response = await _httpClient.GetAsync(url, stoppingToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"API autorisation : HTTP {(int)response.StatusCode}");
                    continue;
                }

                string responseContent = await response.Content.ReadAsStringAsync(stoppingToken);

                var accessResponse = JsonSerializer.Deserialize<AccessResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (accessResponse?.Data?.Authorized != true)
                {
                    _logger.LogWarning($"Accès REFUSÉ : badge {identifierValue}, porte {doorId}");
                    continue;
                }

                _logger.LogInformation($"Accès AUTORISÉ : badge {identifierValue}, porte {doorId}");

                // Autorisé → ouverture
                door.State = 1;

                await _repository.UpdateAsync(door);

                var message = new
                {
                    type = "door",
                    data = door
                };

                var json = JsonSerializer.Serialize(message);

                _logger.LogInformation($"Porte {door.Id} OUVERTE");

                await _manager.BroadcastAsync(json, stoppingToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erreur lors de la communication avec l'API d'autorisation");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Réponse invalide de l'API d'autorisation");
            }
        }
    }

    private class AccessResponse
    {
        public AccessData? Data { get; set; }
    }

    private class AccessData
    {
        public bool Authorized { get; set; }
    }
}