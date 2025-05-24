using Enterprise.Agent.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Models.Domestic.Qwen;

/// <summary>
/// 阿里巴巴通义千问模型提供商
/// </summary>
public class QwenModelProvider : IModelProvider
{
    private readonly ILogger<QwenModelProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly List<ModelInfo> _supportedModels = new();

    public QwenModelProvider(ILogger<QwenModelProvider> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        InitializeSupportedModels();
    }

    public string ProviderName => "Qwen";

    public IEnumerable<ModelInfo> SupportedModels => _supportedModels.AsReadOnly();

    public IChatCompletionClient CreateChatClient(string modelName, ModelConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));

        var modelInfo = _supportedModels.FirstOrDefault(m => m.Name == modelName);
        if (modelInfo == null)
        {
            throw new ArgumentException($"Model '{modelName}' is not supported by {ProviderName}", nameof(modelName));
        }

        return new QwenChatCompletionClient(_httpClient, modelInfo, configuration, _logger);
    }

    public Task<bool> IsModelAvailableAsync(string modelName, CancellationToken cancellationToken = default)
    {
        // 通义千问API的可用性检查
        try
        {
            var modelInfo = _supportedModels.FirstOrDefault(m => m.Name == modelName);
            return Task.FromResult(modelInfo != null);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid model name for {ModelName}", modelName);
            return Task.FromResult(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error checking model availability for {ModelName}", modelName);
            return Task.FromResult(false);
        }
    }

    private void InitializeSupportedModels()
    {
        _supportedModels.AddRange(new[]
        {
            new ModelInfo
            {
                Name = "qwen-turbo",
                DisplayName = "通义千问-Turbo",
                Description = "通义千问超大规模语言模型，适用于各种自然语言理解和生成任务",
                Capabilities = new ModelCapabilities
                {
                    SupportsToolCalling = true,
                    SupportsStreaming = true,
                    SupportsSystemMessage = true,
                    SupportsMultimodal = false,
                    SupportsJsonMode = true,
                    SupportedLanguages = new[] { "zh", "en" }
                },
                Limits = new ModelLimits
                {
                    MaxTokens = 8192,
                    MaxInputTokens = 6144,
                    MaxOutputTokens = 2048,
                    MaxMessages = 100,
                    RequestTimeout = TimeSpan.FromMinutes(2)
                },
                Metadata = new Dictionary<string, object>
                {
                    ["endpoint"] = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
                    ["pricing_input"] = 0.002, // 每千token价格
                    ["pricing_output"] = 0.006
                }
            },
            new ModelInfo
            {
                Name = "qwen-plus",
                DisplayName = "通义千问-Plus",
                Description = "通义千问增强版，具有更强的理解和生成能力",
                Capabilities = new ModelCapabilities
                {
                    SupportsToolCalling = true,
                    SupportsStreaming = true,
                    SupportsSystemMessage = true,
                    SupportsMultimodal = false,
                    SupportsJsonMode = true,
                    SupportedLanguages = new[] { "zh", "en" }
                },
                Limits = new ModelLimits
                {
                    MaxTokens = 32768,
                    MaxInputTokens = 30720,
                    MaxOutputTokens = 2048,
                    MaxMessages = 100,
                    RequestTimeout = TimeSpan.FromMinutes(3)
                },
                Metadata = new Dictionary<string, object>
                {
                    ["endpoint"] = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
                    ["pricing_input"] = 0.004,
                    ["pricing_output"] = 0.012
                }
            },
            new ModelInfo
            {
                Name = "qwen-max",
                DisplayName = "通义千问-Max",
                Description = "通义千问最强版本，具有最佳的理解和生成能力",
                Capabilities = new ModelCapabilities
                {
                    SupportsToolCalling = true,
                    SupportsStreaming = true,
                    SupportsSystemMessage = true,
                    SupportsMultimodal = false,
                    SupportsJsonMode = true,
                    SupportedLanguages = new[] { "zh", "en" }
                },
                Limits = new ModelLimits
                {
                    MaxTokens = 32768,
                    MaxInputTokens = 30720,
                    MaxOutputTokens = 2048,
                    MaxMessages = 100,
                    RequestTimeout = TimeSpan.FromMinutes(5)
                },
                Metadata = new Dictionary<string, object>
                {
                    ["endpoint"] = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
                    ["pricing_input"] = 0.02,
                    ["pricing_output"] = 0.06
                }
            },
            new ModelInfo
            {
                Name = "qwen-max-longcontext",
                DisplayName = "通义千问-Max长文本",
                Description = "支持超长文本的通义千问模型，适合处理长文档",
                Capabilities = new ModelCapabilities
                {
                    SupportsToolCalling = true,
                    SupportsStreaming = true,
                    SupportsSystemMessage = true,
                    SupportsMultimodal = false,
                    SupportsJsonMode = true,
                    SupportedLanguages = new[] { "zh", "en" }
                },
                Limits = new ModelLimits
                {
                    MaxTokens = 1000000, // 100万token上下文
                    MaxInputTokens = 998000,
                    MaxOutputTokens = 2000,
                    MaxMessages = 100,
                    RequestTimeout = TimeSpan.FromMinutes(10)
                },
                Metadata = new Dictionary<string, object>
                {
                    ["endpoint"] = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
                    ["pricing_input"] = 0.02,
                    ["pricing_output"] = 0.06
                }
            }
        });

        _logger.LogInformation("Initialized {Count} supported models for {ProviderName}",
            _supportedModels.Count, ProviderName);
    }
}