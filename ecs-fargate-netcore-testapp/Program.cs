using System.Diagnostics;
using NewRelic.Api.Agent;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async () =>
{
    // Get DockerId from ECS_CONTAINER_METADATA_URI_V4
    var dockerId = await GetDockerIdFromMetadataAsync();
    if (!string.IsNullOrEmpty(dockerId))
    {
        // New Relic API to add a custom attribute
        IAgent agent = NewRelic.Api.Agent.NewRelic.GetAgent();
        var transaction = agent.CurrentTransaction;
        transaction.AddCustomAttribute("containerId", dockerId);
    }

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

async Task<string> GetDockerIdFromMetadataAsync()
{
    var process = new Process()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = "-c \"curl $ECS_CONTAINER_METADATA_URI_V4 | jq -r .DockerId\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }
    };
    process.Start();
    var output = await process.StandardOutput.ReadToEndAsync();
    process.WaitForExit();
    return output.Trim();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
