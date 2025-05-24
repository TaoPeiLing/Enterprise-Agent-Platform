using System.ComponentModel.DataAnnotations;

namespace Enterprise.Agent.Contracts.Models;

/// <summary>
/// 模型提供商接口
/// </summary>
public interface IModelProvider
{
    /// <summary>
    /// 提供商名称
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// 支持的模型列表
    /// </summary>
    IEnumerable<ModelInfo> SupportedModels { get; }
    
    /// <summary>
    /// 创建聊天完成客户端
    /// </summary>
    /// <param name="modelName">模型名称</param>
    /// <param name="configuration">配置参数</param>
    /// <returns>聊天完成客户端</returns>
    IChatCompletionClient CreateChatClient(string modelName, ModelConfiguration configuration);
    
    /// <summary>
    /// 检查模型是否可用
    /// </summary>
    /// <param name="modelName">模型名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可用</returns>
    Task<bool> IsModelAvailableAsync(string modelName, CancellationToken cancellationToken = default);
}

/// <summary>
/// 聊天完成客户端接口
/// </summary>
public interface IChatCompletionClient
{
    /// <summary>
    /// 模型名称
    /// </summary>
    string ModelName { get; }
    
    /// <summary>
    /// 创建聊天完成
    /// </summary>
    /// <param name="messages">消息列表</param>
    /// <param name="options">选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>聊天完成响应</returns>
    Task<ChatCompletionResponse> CreateChatCompletionAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 创建流式聊天完成
    /// </summary>
    /// <param name="messages">消息列表</param>
    /// <param name="options">选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流式聊天完成响应</returns>
    IAsyncEnumerable<ChatCompletionStreamResponse> CreateChatCompletionStreamAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 模型信息
/// </summary>
public record ModelInfo
{
    [Required]
    public string Name { get; init; } = string.Empty;
    
    public string? DisplayName { get; init; }
    
    public string? Description { get; init; }
    
    public ModelCapabilities Capabilities { get; init; } = new();
    
    public ModelLimits Limits { get; init; } = new();
    
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// 模型能力
/// </summary>
public record ModelCapabilities
{
    public bool SupportsToolCalling { get; init; } = false;
    
    public bool SupportsStreaming { get; init; } = true;
    
    public bool SupportsSystemMessage { get; init; } = true;
    
    public bool SupportsMultimodal { get; init; } = false;
    
    public bool SupportsJsonMode { get; init; } = false;
    
    public IEnumerable<string> SupportedLanguages { get; init; } = new[] { "zh", "en" };
}

/// <summary>
/// 模型限制
/// </summary>
public record ModelLimits
{
    public int MaxTokens { get; init; } = 4096;
    
    public int MaxInputTokens { get; init; } = 3072;
    
    public int MaxOutputTokens { get; init; } = 1024;
    
    public int MaxMessages { get; init; } = 100;
    
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);
}

/// <summary>
/// 模型配置
/// </summary>
public record ModelConfiguration
{
    public string? ApiKey { get; init; }
    
    public string? BaseUrl { get; init; }
    
    public string? ApiVersion { get; init; }
    
    public TimeSpan? Timeout { get; init; }
    
    public int? MaxRetries { get; init; } = 3;
    
    public IDictionary<string, object> AdditionalProperties { get; init; } = new Dictionary<string, object>();
}