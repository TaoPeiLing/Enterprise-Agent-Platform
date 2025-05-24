using Enterprise.Agent.Contracts.Models;
using Enterprise.Agent.Core.Agents;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Agents;

/// <summary>
/// 通用聊天代理
/// </summary>
public class ChatAgent : ChatAgentBase
{
    public ChatAgent(
        string agentId,
        string name,
        IChatCompletionClient chatClient,
        ILogger<ChatAgent> logger,
        string? systemMessage = null)
        : base(agentId, name, chatClient, logger, systemMessage)
    {
        Description = "通用聊天代理，支持多轮对话和工具调用";
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing ChatAgent {AgentId} with model {ModelName}", AgentId, ModelName);
        
        // 可以在这里添加初始化逻辑，比如加载工具、验证模型等
        await base.OnInitializeAsync(cancellationToken);
        
        Logger.LogInformation("ChatAgent {AgentId} initialized successfully", AgentId);
    }
}

/// <summary>
/// 代码助手Agent
/// </summary>
public class CodeAssistantAgent : ChatAgentBase
{
    public CodeAssistantAgent(
        string agentId,
        string name,
        IChatCompletionClient chatClient,
        ILogger<CodeAssistantAgent> logger)
        : base(agentId, name, chatClient, logger, GetSystemMessage())
    {
        Description = "专业的代码助手，擅长编程、调试和代码审查";
    }

    private static string GetSystemMessage()
    {
        return """
        你是一个专业的代码助手，具有以下能力：
        
        1. 编程语言专家：精通C#、Python、JavaScript、Java等多种编程语言
        2. 代码分析：能够分析代码结构、发现潜在问题和优化点
        3. 调试专家：帮助定位和解决代码bug
        4. 最佳实践：提供符合行业标准的代码建议
        5. 架构设计：协助设计软件架构和系统设计
        
        请始终：
        - 提供清晰、可读的代码示例
        - 解释代码的工作原理
        - 遵循编程最佳实践
        - 考虑性能、安全性和可维护性
        - 使用中文回答，但代码注释可以使用英文
        """;
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing CodeAssistantAgent {AgentId} with model {ModelName}", AgentId, ModelName);
        
        // 可以添加代码相关的工具
        // AddTool(new CodeAnalysisTool());
        // AddTool(new CodeGenerationTool());
        
        await base.OnInitializeAsync(cancellationToken);
        
        Logger.LogInformation("CodeAssistantAgent {AgentId} initialized successfully", AgentId);
    }
}

/// <summary>
/// 文档助手Agent
/// </summary>
public class DocumentAssistantAgent : ChatAgentBase
{
    public DocumentAssistantAgent(
        string agentId,
        string name,
        IChatCompletionClient chatClient,
        ILogger<DocumentAssistantAgent> logger)
        : base(agentId, name, chatClient, logger, GetSystemMessage())
    {
        Description = "专业的文档助手，擅长文档编写、总结和分析";
    }

    private static string GetSystemMessage()
    {
        return """
        你是一个专业的文档助手，具有以下能力：
        
        1. 文档编写：能够创建清晰、结构化的技术文档
        2. 内容总结：快速提取文档要点和关键信息
        3. 格式优化：改善文档的可读性和专业性
        4. 多语言支持：支持中英文文档处理
        5. 标准化：遵循文档编写的行业标准
        
        请始终：
        - 保持文档结构清晰
        - 使用恰当的标题层级
        - 提供准确的信息
        - 考虑读者的理解水平
        - 使用专业但易懂的语言
        """;
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing DocumentAssistantAgent {AgentId} with model {ModelName}", AgentId, ModelName);
        
        // 可以添加文档相关的工具
        // AddTool(new DocumentAnalysisTool());
        // AddTool(new MarkdownFormatterTool());
        
        await base.OnInitializeAsync(cancellationToken);
        
        Logger.LogInformation("DocumentAssistantAgent {AgentId} initialized successfully", AgentId);
    }
}

/// <summary>
/// 数据分析Agent
/// </summary>
public class DataAnalystAgent : ChatAgentBase
{
    public DataAnalystAgent(
        string agentId,
        string name,
        IChatCompletionClient chatClient,
        ILogger<DataAnalystAgent> logger)
        : base(agentId, name, chatClient, logger, GetSystemMessage())
    {
        Description = "专业的数据分析师，擅长数据处理、分析和可视化";
    }

    private static string GetSystemMessage()
    {
        return """
        你是一个专业的数据分析师，具有以下能力：
        
        1. 数据处理：清洗、转换和准备数据
        2. 统计分析：执行描述性和推断性统计分析
        3. 数据可视化：创建图表和可视化报告
        4. 模式识别：发现数据中的趋势和模式
        5. 业务洞察：将数据分析转化为业务建议
        
        请始终：
        - 提供准确的数据分析
        - 解释分析方法和结果
        - 考虑数据的质量和局限性
        - 提供可操作的建议
        - 使用清晰的图表和说明
        """;
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing DataAnalystAgent {AgentId} with model {ModelName}", AgentId, ModelName);
        
        // 可以添加数据分析相关的工具
        // AddTool(new DataProcessingTool());
        // AddTool(new StatisticsCalculatorTool());
        // AddTool(new ChartGeneratorTool());
        
        await base.OnInitializeAsync(cancellationToken);
        
        Logger.LogInformation("DataAnalystAgent {AgentId} initialized successfully", AgentId);
    }
}