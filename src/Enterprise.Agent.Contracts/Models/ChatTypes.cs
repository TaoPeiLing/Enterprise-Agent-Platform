using System.ComponentModel.DataAnnotations;

namespace Enterprise.Agent.Contracts.Models;

/// <summary>
/// 聊天消息
/// </summary>
public record ChatMessage
{
    [Required]
    public string Role { get; init; } = string.Empty;
    
    [Required]
    public string Content { get; init; } = string.Empty;
    
    public string? Name { get; init; }
    
    public IEnumerable<ToolCall>? ToolCalls { get; init; }
    
    public string? ToolCallId { get; init; }
    
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// 工具调用
/// </summary>
public record ToolCall
{
    [Required]
    public string Id { get; init; } = string.Empty;
    
    [Required]
    public string Type { get; init; } = "function";
    
    [Required]
    public FunctionCall Function { get; init; } = new();
}

/// <summary>
/// 函数调用
/// </summary>
public record FunctionCall
{
    [Required]
    public string Name { get; init; } = string.Empty;
    
    [Required]
    public string Arguments { get; init; } = string.Empty;
}

/// <summary>
/// 聊天完成选项
/// </summary>
public record ChatCompletionOptions
{
    public double? Temperature { get; init; } = 0.7;
    
    public double? TopP { get; init; } = 1.0;
    
    public int? MaxTokens { get; init; }
    
    public bool Stream { get; init; } = false;
    
    public IEnumerable<string>? Stop { get; init; }
    
    public IEnumerable<ToolDefinition>? Tools { get; init; }
    
    public object? ToolChoice { get; init; }
    
    public bool? JsonMode { get; init; }
    
    public IDictionary<string, object> AdditionalProperties { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// 工具定义
/// </summary>
public record ToolDefinition
{
    [Required]
    public string Type { get; init; } = "function";
    
    [Required]
    public FunctionDefinition Function { get; init; } = new();
}

/// <summary>
/// 函数定义
/// </summary>
public record FunctionDefinition
{
    [Required]
    public string Name { get; init; } = string.Empty;
    
    public string? Description { get; init; }
    
    public object? Parameters { get; init; }
}

/// <summary>
/// 聊天完成响应
/// </summary>
public record ChatCompletionResponse
{
    [Required]
    public string Id { get; init; } = string.Empty;
    
    [Required]
    public string Object { get; init; } = "chat.completion";
    
    public long Created { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [Required]
    public string Model { get; init; } = string.Empty;
    
    [Required]
    public IEnumerable<ChatChoice> Choices { get; init; } = Array.Empty<ChatChoice>();
    
    public UsageInfo? Usage { get; init; }
    
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// 流式聊天完成响应
/// </summary>
public record ChatCompletionStreamResponse
{
    [Required]
    public string Id { get; init; } = string.Empty;
    
    [Required]
    public string Object { get; init; } = "chat.completion.chunk";
    
    public long Created { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [Required]
    public string Model { get; init; } = string.Empty;
    
    [Required]
    public IEnumerable<ChatStreamChoice> Choices { get; init; } = Array.Empty<ChatStreamChoice>();
    
    public UsageInfo? Usage { get; init; }
}

/// <summary>
/// 聊天选择
/// </summary>
public record ChatChoice
{
    public int Index { get; init; } = 0;
    
    [Required]
    public ChatMessage Message { get; init; } = new();
    
    public string? FinishReason { get; init; }
}

/// <summary>
/// 流式聊天选择
/// </summary>
public record ChatStreamChoice
{
    public int Index { get; init; } = 0;
    
    public ChatMessageDelta? Delta { get; init; }
    
    public string? FinishReason { get; init; }
}

/// <summary>
/// 聊天消息增量
/// </summary>
public record ChatMessageDelta
{
    public string? Role { get; init; }
    
    public string? Content { get; init; }
    
    public IEnumerable<ToolCall>? ToolCalls { get; init; }
}

/// <summary>
/// 使用信息
/// </summary>
public record UsageInfo
{
    public int PromptTokens { get; init; } = 0;
    
    public int CompletionTokens { get; init; } = 0;
    
    public int TotalTokens { get; init; } = 0;
}