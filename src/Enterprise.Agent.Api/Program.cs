using Enterprise.Agent.Agents;
using Enterprise.Agent.Core.Services;
using Enterprise.Agent.Services; // Ensured this is present

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register ILanguageModelService placeholder if not already registered
if (!builder.Services.Any(x => x.ServiceType == typeof(ILanguageModelService)))
{
    builder.Services.AddSingleton<ILanguageModelService, PlaceholderLanguageModelService>(); 
}

// Register AgentFactory
builder.Services.AddSingleton<AgentFactory>();

// Register IAgentManagementService placeholder if not already registered
// Note: AgentManagementService itself would need to be defined in Enterprise.Agent.Services
// For now, this assumes such a class exists or will be added.
if (!builder.Services.Any(x => x.ServiceType == typeof(IAgentManagementService)))
{
    // If AgentManagementService is not available, this line would cause a compile error.
    // Assuming it's defined in Enterprise.Agent.Services and implements IAgentManagementService.
    builder.Services.AddScoped<IAgentManagementService, AgentManagementService>();
}

// Register the new services for Tender Assistant feature
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddScoped<ITenderProjectService, TenderProjectService>();
builder.Services.AddScoped<ITenderWorkflowService, TenderWorkflowService>();
builder.Services.AddScoped<IUserInteractionService, UserInteractionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Minimal API examples
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            "SampleSummary" // Placeholder summary
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/chat", async (HttpContext context, AgentFactory agentFactory) =>
{
    await context.Response.WriteAsync("Chat endpoint placeholder.");
})
.WithName("Chat")
.WithOpenApi();

app.Run();

// Placeholder class for ILanguageModelService (should be in its own file ideally)
public class PlaceholderLanguageModelService : ILanguageModelService
{
    public Task<string> GenerateResponseAsync(string prompt)
    {
        return Task.FromResult($"Placeholder response for: {prompt}");
    }
}

// Placeholder for WeatherForecast record if not defined elsewhere in the project
internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);
