using Enterprise.Agent.Contracts.Agents;
using Enterprise.Agent.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Agents;

/// <summary>
/// Agent工厂实现
/// </summary>
public class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentFactory> _logger;
    private readonly Dictionary<string, IModelProvider> _modelProviders;

    public AgentFactory(
        IServiceProvider serviceProvider,
        ILogger<AgentFactory> logger,
        IEnumerable<IModelProvider> modelProviders)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelProviders = modelProviders?.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase)
            ?? throw new ArgumentNullException(nameof(modelProviders));
    }

    public IEnumerable<string> SupportedAgentTypes => new[]
    {
        "chat",
        "code-assistant",
        "document-assistant",
        "data-analyst"
    };

    public IAgent CreateAgent(AgentConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(configuration.Name))
            throw new ArgumentException("Agent name cannot be null or empty", nameof(configuration));

        if (string.IsNullOrWhiteSpace(configuration.AgentType))
            throw new ArgumentException("Agent type cannot be null or empty", nameof(configuration));

        _logger.LogDebug("Creating agent {AgentName} of type {AgentType}", configuration.Name, configuration.AgentType);

        try
        {
            var chatClient = CreateChatClient(configuration);
            var agentId = Guid.NewGuid().ToString();

            return configuration.AgentType.ToUpperInvariant() switch
            {
                "CHAT" => CreateChatAgent(agentId, configuration, chatClient),
                "CODE-ASSISTANT" => CreateCodeAssistantAgent(agentId, configuration, chatClient),
                "DOCUMENT-ASSISTANT" => CreateDocumentAssistantAgent(agentId, configuration, chatClient),
                "DATA-ANALYST" => CreateDataAnalystAgent(agentId, configuration, chatClient),
                _ => throw new NotSupportedException($"Agent type '{configuration.AgentType}' is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create agent {AgentName} of type {AgentType}",
                configuration.Name, configuration.AgentType);
            throw;
        }
    }

    private IChatCompletionClient CreateChatClient(AgentConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.ModelName))
            throw new ArgumentException("Model name is required for chat agents", nameof(configuration));

        // 解析模型名称
        string providerName, modelName;

        // 检查是否明确指定了提供商（格式：provider:model）
        if (configuration.ModelName.StartsWith("ollama:", StringComparison.OrdinalIgnoreCase))
        {
            providerName = "Ollama";
            modelName = configuration.ModelName.Substring(7); // 移除 "ollama:" 前缀
        }
        else if (configuration.ModelName.StartsWith("qwen:", StringComparison.OrdinalIgnoreCase))
        {
            providerName = "Qwen";
            modelName = configuration.ModelName.Substring(5); // 移除 "qwen:" 前缀
        }
        else
        {
            // 尝试根据模型名称自动识别提供商
            providerName = DetectProviderFromModelName(configuration.ModelName);
            modelName = configuration.ModelName;
        }