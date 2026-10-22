using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Antlr4.Runtime;
using Kotlin_plus_plus;

var builder = WebApplication.CreateBuilder(args);

// 1. Настраиваем CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                    "https://mp-team-hello-world.github.io", 
                    "http://localhost:5000", 
                    "http://127.0.0.1:5000",
                    "http://localhost:5500",
                    "http://127.0.0.1:5500"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});

var app = builder.Build();

// Включаем CORS
app.UseCors();

// 2. Включаем раздачу статики из папки wwwroot
app.UseDefaultFiles(); // Автоматически ищет index.html по адресу /
app.UseStaticFiles();  // Отдает index.html, style.css, script.js, tree-view.js

// 3. Эндпоинт POST для трансляции
app.MapPost("/translate", (TranslateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Code))
    {
        return Results.BadRequest(new { error = "Входной код пуст" });
    }

    try
    {
        var (targetCode, tree) = Translator.TranslateWithTree(
            request.Code, 
            request.Source ?? "kotlin",
            request.Target ?? "cpp"
        );
        
        return Results.Ok(new { cppCode = targetCode, tree });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

public record TranslateRequest(string Code, string? Source = "kotlin", string? Target = "cpp");