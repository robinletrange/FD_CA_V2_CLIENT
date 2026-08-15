using System.Text.Json;

namespace CLIENT.Models;

public class Door
{
    public int Id { get; set; }


    public bool Enabled { get; set; }

    public int State { get; set; }
}

public class DoorDatabase
{
    public List<Door> Doors { get; set; } = new();
}

public interface IDoorRepository
{
    Task<List<Door>> GetAllAsync();
    Task<Door?> GetByIdAsync(int id);
    Task AddAsync(Door door);
    Task UpdateAsync(Door door);
    Task DeleteAsync(int id);
}

public class JsonDoorRepository : IDoorRepository
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonDoorRepository(string filePath)
    {
        _filePath = filePath;
    }

    private async Task<DoorDatabase> LoadAsync()
    {

        if (!File.Exists(_filePath))
            return new DoorDatabase();

        var json = await File.ReadAllTextAsync(_filePath);

        if (string.IsNullOrWhiteSpace(json))
            return new DoorDatabase();

        var database = JsonSerializer.Deserialize<DoorDatabase>(json, _options) ?? new DoorDatabase();

        return database;
    }

    private async Task SaveAsync(DoorDatabase database)
    {
        var json = JsonSerializer.Serialize(database, _options);

        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<List<Door>> GetAllAsync()
    {
        var database = await LoadAsync();

        return database.Doors;
    }

    public async Task<Door?> GetByIdAsync(int id)
    {
        var database = await LoadAsync();

        return database.Doors.FirstOrDefault(x => x.Id == id);
    }

    public async Task AddAsync(Door door)
    {
        var database = await LoadAsync();

        database.Doors.Add(door);

        await SaveAsync(database);
    }

    public async Task UpdateAsync(Door door)
    {
        var database = await LoadAsync();

        var index = database.Doors.FindIndex(x => x.Id == door.Id);

        if (index == -1)
            throw new KeyNotFoundException($"Door '{door.Id}' not found.");

        database.Doors[index] = door;

        await SaveAsync(database);
    }

    public async Task DeleteAsync(int id)
    {
        var database = await LoadAsync();

        var door = database.Doors.FirstOrDefault(x => x.Id == id);

        if (door == null)
            return;

        database.Doors.Remove(door);

        await SaveAsync(database);
    }
}