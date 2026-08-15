//
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Text.Json;

global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;

global using DotNetEnv;

global using CLIENT.Models;


//
Env.Load();


//
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


//
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("API");

app.MapControllers();

app.Run();
