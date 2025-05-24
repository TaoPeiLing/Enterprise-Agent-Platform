using Enterprise.Agent.Agents;
using Enterprise.Agent.Contracts.Agents;
using Enterprise.Agent.Contracts.Models;
using Enterprise.Agent.Models.Domestic.Qwen;
using Enterprise.Agent.Models.Ollama;
using Enterprise.Agent.Services;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/enterprise-agent-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Enterprise Agent Platform API",
        Version = "v1",
        Description = "企业级Agent平台API，支持国产模型和Ollama"
    });
});

// 添加内存缓存
builder.Services.AddMemoryCache();

// 添加HTTP客户端
builder.Services.AddHttpClient();

// 注册模型提供商
builder.Services.AddSingleton<IModelProvider>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<OllamaModelProvider>>();
    var httpClient = serviceProvider.GetRequiredService<HttpClient>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    var ollamaBaseUrl = configuration["ModelProviders:Ollama:BaseUrl"] ?? "http://localhost:11434";
    return new OllamaModelProvider(logger, httpClient, ollamaBaseUrl);
});

builder.Services.AddSingleton<IModelProvider, QwenModelProvider>();

// 注册Agent工厂
builder.Services.AddSingleton<IAgentFactory, AgentFactory>();

// 注册服务
builder.Services.AddSingleton<IAgentManagementService, AgentManagementService>();
builder.Services.AddSingleton<IChatService, ChatService>();

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise Agent Platform API v1");
        c.RoutePrefix = string.Empty; // 设置Swagger UI为根路径
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// 添加健康检查端点
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow });

// 添加模型提供商信息端点
app.MapGet("/api/providers", (IEnumerable<IModelProvider> providers) =>
{
    return providers.Select(p => new
    {
        Name = p.ProviderName,
        Models = p.SupportedModels.Select(m => new
        {
            m.Name,
            m.DisplayName,
            m.Description,
            m.Capabilities,
            m.Limits
        })
    });
});

try
{
    Log.Information("Starting Enterprise Agent Platform API");
    app.Run();
}
catch (HostAbortedException)
{
    // 正常关闭，不记录错误
}
catch (InvalidOperationException ex)
{
    Log.Fatal(ex, "Invalid operation during application startup");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}