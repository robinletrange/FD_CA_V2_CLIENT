using System.Text;
using System.Text.Json;

namespace CLIENT.Services;

public class DoorSimulationService : BackgroundService
{
    private readonly IDoorRepository _repository;
    private readonly WebSocketManager _manager;
    private readonly ILogger<DoorSimulationService> _logger;
    private readonly HttpClient _httpClient;

    private readonly Random _random = new();

    // API d'autorisation
    private const string AccessApiUrl = "http://192.168.1.201:8223";

    // Liste des UID RFID
    private readonly List<string> _credentials = new();

    private readonly string _credentialsFilePath;

    public DoorSimulationService(IDoorRepository repository, WebSocketManager manager, ILogger<DoorSimulationService> logger, HttpClient httpClient, IHostEnvironment environment)
    {
        _repository = repository;
        _manager = manager;
        _logger = logger;
        _httpClient = httpClient;

        _credentialsFilePath = Path.Combine(environment.ContentRootPath, "Data", "credentials.csv");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // =========================================================
        // CHARGEMENT DES CREDENTIALS
        // =========================================================

        try
        {
            if (!File.Exists(_credentialsFilePath))
            {
                _logger.LogError($"Fichier des credentials introuvable : {_credentialsFilePath}");

                return;
            }

            foreach (string line in File.ReadLines(_credentialsFilePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(',');

                // On veut le deuxième élément
                if (parts.Length < 2)
                    continue;

                string credential = parts[1].Trim();

                if (!string.IsNullOrWhiteSpace(credential))
                {
                    _credentials.Add(credential);
                }
            }

            _logger.LogInformation($"{_credentials.Count} credentials RFID chargés depuis {_credentialsFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement du fichier des credentials");

            return;
        }

        // Aucun credential disponible
        if (_credentials.Count == 0)
        {
            _logger.LogError("Aucun credential RFID valide trouvé dans le fichier CSV");

            return;
        }

        // =========================================================
        // SIMULATION
        // =========================================================

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_random.Next(1000, 5000), stoppingToken);

            int doorId = 14000000 + _random.Next(2);

            Dictionary<int, Door> doors = (await _repository.GetAllAsync()).ToDictionary(d => d.Id);

            if (!doors.TryGetValue(doorId, out Door? door) || door == null)
            {
                continue;
            }

            // =========================================================
            // PORTE OUVERTE → FERMETURE
            // =========================================================

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

            // =========================================================
            // CHOIX D'UN CREDENTIAL RFID ALÉATOIRE
            // =========================================================

            string credentialValue = _credentials[_random.Next(_credentials.Count)];

            _logger.LogInformation($"Badge simulé : {credentialValue}");

            // =========================================================
            // DEMANDE D'AUTORISATION
            // =========================================================

            string url = $"{AccessApiUrl}/doors/access?identifierValue={Uri.EscapeDataString(credentialValue)}&doorId={doorId}";

            try
            {
                _logger.LogInformation($"Demande d'autorisation pour porte {doorId} " + $"avec badge {credentialValue}");

                using HttpResponseMessage response = await _httpClient.GetAsync(url, stoppingToken);

                string responseContent = await response.Content.ReadAsStringAsync(stoppingToken);

                _logger.LogInformation($"API autorisation : HTTP {(int)response.StatusCode}");

                var accessResponse = JsonSerializer.Deserialize<AccessResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (accessResponse?.Data?.Authorized != true)
                {
                    _logger.LogWarning($"Accès REFUSÉ : badge {credentialValue}, porte {doorId} " + $"(HTTP {(int)response.StatusCode})");

                    // =====================================================
                    // ACCÈS REFUSÉ
                    // =====================================================

                    var logData2 = new
                    {
                        data = new
                        {
                            type = "BADGE",
                            value = credentialValue,
                            door = doorId == 14000000 ? "LMNO_1556_01" : "LMNO_1556_02",
                            message = "Acces refuse",
                            allowed = "FALSE"
                        },
                        level = "ERROR"
                    };

                    string logJson2 = JsonSerializer.Serialize(logData2);

                    using var content2 = new StringContent(logJson2, Encoding.UTF8, "application/json");

                    using HttpResponseMessage logResponse2 = await _httpClient.PostAsync($"{AccessApiUrl}/logs/PLC", content2, stoppingToken);

                    if (!logResponse2.IsSuccessStatusCode)
                    {
                        string error2 = await logResponse2.Content.ReadAsStringAsync(stoppingToken);

                        _logger.LogWarning($"Erreur envoi log refus : HTTP {(int)logResponse2.StatusCode} - {error2}");
                    }
                    else
                    {
                        _logger.LogInformation($"Log REFUS envoyé pour le badge {credentialValue}");
                    }

                    continue;
                }

                // =====================================================
                // ACCÈS AUTORISÉ
                // =====================================================

                _logger.LogInformation($"Accès AUTORISÉ : badge {credentialValue}, " + $"porte {doorId}");

                // =========================================================
                // LOG HTTP POST
                // =========================================================

                var logData = new
                {
                    data = new
                    {
                        type = "BADGE",
                        value = credentialValue,
                        door = doorId == 14000000 ? "LMNO_1556_01" : "LMNO_1556_02",
                        message = "Acces OK",
                        allowed = "TRUE"
                    },
                    level = "SUCCESS"
                };

                string logJson = JsonSerializer.Serialize(logData);

                using var content = new StringContent(logJson, Encoding.UTF8, "application/json");

                using HttpResponseMessage logResponse = await _httpClient.PostAsync($"{AccessApiUrl}/logs/PLC", content, stoppingToken);

                if (!logResponse.IsSuccessStatusCode)
                {
                    string error = await logResponse.Content.ReadAsStringAsync(stoppingToken);

                    _logger.LogWarning($"Erreur envoi log : HTTP {(int)logResponse.StatusCode} - {error}");
                }
                else
                {
                    _logger.LogInformation($"Log AUTORISÉ envoyé pour le badge {credentialValue}");
                }

                // =========================================================
                // OUVERTURE DE LA PORTE
                // =========================================================

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