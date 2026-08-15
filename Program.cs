global using System.ComponentModel.DataAnnotations.Schema;
global using System.Security.Cryptography;
global using System.Text.Json;

global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;

global using DotNetEnv;

global using CLIENT.Models;

// Charger le .env s'il existe
Env.Load();


// ─────────────────────────────────────────────
// Numéro de série
// ─────────────────────────────────────────────

var serialDirectory = Path.Combine(
    Directory.GetCurrentDirectory(),
    "Data"
);

Directory.CreateDirectory(serialDirectory);

var serialPath = Path.Combine(
    serialDirectory,
    "serial.txt"
);

string serialNumber;

if (File.Exists(serialPath))
{
    serialNumber = File.ReadAllText(serialPath).Trim();
}
else
{
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    var bytes = RandomNumberGenerator.GetBytes(8);

    serialNumber = string.Concat(
        bytes.Select(b => chars[b % chars.Length])
    );

    File.WriteAllText(serialPath, serialNumber);
}

// Disponible globalement dans l'application
AppInfo.SerialNumber = serialNumber;

// Variable d'environnement du processus
Environment.SetEnvironmentVariable(
    "CLIENT_SERIAL_NUMBER",
    serialNumber
);


// ─────────────────────────────────────────────
// ASP.NET
// ─────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("API", policy =>
    {
        policy
            .WithOrigins("http://localhost:5165")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IDoorRepository>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();

    var path = Path.Combine(env.ContentRootPath, "Data", "doors.json");

    return new JsonDoorRepository(path);
});

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("API");

app.MapControllers();

app.Run();