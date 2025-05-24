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

        if (!_modelProviders.TryGetValue(providerName, out var provider))
        {
            throw new ArgumentException($"Model provider '{providerName}' is not available. " +
                $"Available providers: {string.Join(", ", _modelProviders.Keys)}");
        }

        // 从配置中提取模型配置
        var modelConfig = new ModelConfiguration
        {
            ApiKey = configuration.Properties.TryGetValue("ApiKey", out var apiKey) ? apiKey?.ToString() : null,
            BaseUrl = configuration.Properties.TryGetValue("BaseUrl", out var baseUrl) ? baseUrl?.ToString() : null,
            Timeout = configuration.Properties.TryGetValue("Timeout", out var timeout) &&
                     TimeSpan.TryParse(timeout?.ToString(), out var timeoutValue) ? timeoutValue : null,
            MaxRetries = configuration.Properties.TryGetValue("MaxRetries", out var maxRetries) &&
                        int.TryParse(maxRetries?.ToString(), out var retriesValue) ? retriesValue : 3,
            AdditionalProperties = configuration.Properties.Where(p =>
                !new[] { "ApiKey", "BaseUrl", "Timeout", "MaxRetries" }.Contains(p.Key))
                .ToDictionary(p => p.Key, p => p.Value)
        };

        return provider.CreateChatClient(modelName, modelConfig);
    }

    private string DetectProviderFromModelName(string modelName)
    {
        // 根据模型名称模式自动检测提供商
        if (modelName.Contains(':') &&
            (modelName.StartsWith("qwen", StringComparison.OrdinalIgnoreCase) ||
             modelName.StartsWith("llama", StringComparison.OrdinalIgnoreCase) ||
             modelName.StartsWith("deepseek", StringComparison.OrdinalIgnoreCase) ||
             modelName.StartsWith("chatglm", StringComparison.OrdinalIgnoreCase)))
        {
            return "Ollama";
        }

        if (modelName.StartsWith("qwen-", StringComparison.OrdinalIgnoreCase))
        {
            return "Qwen";
        }

        // 默认使用第一个可用的提供商
        var firstProvider = _modelProviders.Values.FirstOrDefault();
        if (firstProvider == null)
            throw new InvalidOperationException("No model providers are available");

        return firstProvider.ProviderName;
    }

    private ChatAgent CreateChatAgent(string agentId, AgentConfiguration configuration, IChatCompletionClient chatClient)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<ChatAgent>>();
        return new ChatAgent(agentId, configuration.Name, chatClient, logger, configuration.SystemMessage);
    }

    private CodeAssistantAgent CreateCodeAssistantAgent(string agentId, AgentConfiguration configuration, IChatCompletionClient chatClient)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<CodeAssistantAgent>>();
        return new CodeAssistantAgent(agentId, configuration.Name, chatClient, logger);
    }

    private DocumentAssistantAgent CreateDocumentAssistantAgent(string agentId, AgentConfiguration configuration, IChatCompletionClient chatClient)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<DocumentAssistantAgent>>();
        return new DocumentAssistantAgent(agentId, configuration.Name, chatClient, logger);
    }

    private DataAnalystAgent CreateDataAnalystAgent(string agentId, AgentConfiguration configuration, IChatCompletionClient chatClient)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<DataAnalystAgent>>();
        return new DataAnalystAgent(agentId, configuration.Name, chatClient, logger);
    }
}

/// <summary>
/// Agent工厂扩展方法
/// </summary>
public static class AgentFactoryExtensions
{
    /// <summary>
    /// 创建聊天Agent的便捷方法
    /// </summary>
    public static IAgent CreateChatAgent(
        this IAgentFactory factory,
        string name,
        string modelName,
        string? systemMessage = null,
        Dictionary<string, object>? properties = null)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));
        var configuration = new AgentConfiguration
        {
            Name = name,
            AgentType = "chat",
            ModelName = modelName,
            SystemMessage = systemMessage,
            Properties = properties ?? new Dictionary<string, object>()
        };

        return factory.CreateAgent(configuration);
    }

    /// <summary>
    /// 创建代码助手Agent的便捷方法
    /// </summary>
    public static IAgent CreateCodeAssistantAgent(
        this IAgentFactory factory,
        string name,
        string modelName,
        Dictionary<string, object>? properties = null)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));
        var configuration = new AgentConfiguration
        {
            Name = name,
            AgentType = "code-assistant",
            ModelName = modelName,
            Properties = properties ?? new Dictionary<string, object>()
        };

        return factory.CreateAgent(configuration);
    }

    /// <summary>
    /// 创建文档助手Agent的便捷方法
    /// </summary>
    public static IAgent CreateDocumentAssistantAgent(
        this IAgentFactory factory,
        string name,
        string modelName,
        Dictionary<string, object>? properties = null)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));
        var configuration = new AgentConfiguration
        {
            Name = name,
            AgentType = "document-assistant",
            ModelName = modelName,
            Properties = properties ?? new Dictionary<string, object>()
        };

        return factory.CreateAgent(configuration);
    }

    /// <summary>
    /// 创建数据分析Agent的便捷方法
    /// </summary>
    public static IAgent CreateDataAnalystAgent(
        this IAgentFactory factory,
        string name,
        string modelName,
        Dictionary<string, object>? properties = null)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));
        var configuration = new AgentConfiguration
        {
            Name = name,
            AgentType = "data-analyst",
            ModelName = modelName,
            Properties = properties ?? new Dictionary<string, object>()
        };

        return factory.CreateAgent(configuration);
    }
}