using Enterprise.Agent.Contracts.Messages;
using System.ComponentModel.DataAnnotations;

namespace Enterprise.Agent.Contracts.Agents;

/// <summary>
/// 代理基础接口
/// </summary>
public interface IAgent
{
    /// <summary>
    /// 代理唯一标识符
    /// </summary>
    string AgentId { get; }
    
    /// <summary>
    /// 代理名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 代理描述
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// 代理类型
    /// </summary>
    string AgentType { get; }
    
    /// <summary>
    /// 代理状态
    /// </summary>
    AgentStatus Status { get; }
    
    /// <summary>
    /// 处理消息
    /// </summary>
    /// <param name="message">输入消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应消息</returns>
    Task<IAgentMessage> HandleMessageAsync(IAgentMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 流式处理消息
    /// </summary>
    /// <param name="message">输入消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应消息流</returns>
    IAsyncEnumerable<IAgentMessage> HandleMessageStreamAsync(IAgentMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 初始化代理
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 停止代理
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 聊天代理接口
/// </summary>
public interface IChatAgent : IAgent
{
    /// <summary>
    /// 系统消息
    /// </summary>
    string? SystemMessage { get; }
    
    /// <summary>
    /// 模型名称
    /// </summary>
    string ModelName { get; }
    
    /// <summary>
    /// 支持的工具
    /// </summary>
    IEnumerable<IAgentTool> Tools { get; }
    
    /// <summary>
    /// 聊天
    /// </summary>
    /// <param name="message">用户消息</param>
    /// <param name="conversationId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>助手回复</returns>
    Task<string> ChatAsync(string message, string? conversationId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 流式聊天
    /// </summary>
    /// <param name="message">用户消息</param>
    /// <param name="conversationId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>助手回复流</returns>
    IAsyncEnumerable<string> ChatStreamAsync(string message, string? conversationId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代理工具接口
/// </summary>
public interface IAgentTool
{
    /// <summary>
    /// 工具名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 工具描述
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// 参数架构
    /// </summary>
    object? ParameterSchema { get; }
    
    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="arguments">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<object> ExecuteAsync(object arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代理状态
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// 未初始化
    /// </summary>
    NotInitialized,
    
    /// <summary>
    /// 初始化中
    /// </summary>
    Initializing,
    
    /// <summary>
    /// 运行中
    /// </summary>
    Running,
    
    /// <summary>
    /// 空闲
    /// </summary>
    Idle,
    
    /// <summary>
    /// 忙碌
    /// </summary>
    Busy,
    
    /// <summary>
    /// 错误
    /// </summary>
    Error,
    
    /// <summary>
    /// 已停止
    /// </summary>
    Stopped
}

/// <summary>
/// 代理配置
/// </summary>
public record AgentConfiguration
{
    [Required]
    public string Name { get; init; } = string.Empty;
    
    public string? Description { get; init; }
    
    [Required]
    public string AgentType { get; init; } = string.Empty;
    
    public string? SystemMessage { get; init; }
    
    public string? ModelName { get; init; }
    
    public IEnumerable<string> ToolNames { get; init; } = Array.Empty<string>();
    
    public IDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// 代理工厂接口
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// 创建代理
    /// </summary>
    /// <param name="configuration">代理配置</param>
    /// <returns>代理实例</returns>
    IAgent CreateAgent(AgentConfiguration configuration);
    
    /// <summary>
    /// 支持的代理类型
    /// </summary>
    IEnumerable<string> SupportedAgentTypes { get; }
}