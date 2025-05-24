using Enterprise.Agent.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>
/// 聊天控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 发送聊天消息
    /// </summary>
    /// <param name="request">聊天请求</param>
    /// <returns>聊天响应</returns>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (request == null)
            return BadRequest("Request cannot be null");

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _chatService.ChatAsync(
                request.AgentId,
                request.Message,
                request.ConversationId);

            return Ok(new ChatResponse
            {
                AgentId = request.AgentId,
                ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                Message = response,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid chat request");
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation in chat with agent {AgentId}", request.AgentId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout in chat with agent {AgentId}", request.AgentId);
            return StatusCode(500, new { Error = "Request timeout" });
        }
    }

    /// <summary>
    /// 流式聊天
    /// </summary>
    /// <param name="request">聊天请求</param>
    /// <returns>流式聊天响应</returns>
    [HttpPost("stream")]
    public async Task<ActionResult> ChatStream([FromBody] ChatRequest request)
    {
        if (request == null)
            return BadRequest("Request cannot be null");

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

            await foreach (var chunk in _chatService.ChatStreamAsync(
                request.AgentId,
                request.Message,
                conversationId)){
                var data = $"data: {System.Text.Json.JsonSerializer.Serialize(new
                {
                    AgentId = request.AgentId,
                    ConversationId = conversationId,
                    Chunk = chunk,
                    Timestamp = DateTimeOffset.UtcNow
                })}

";

                await Response.WriteAsync(data, Encoding.UTF8);
                await Response.Body.FlushAsync();
            }

            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();

            return new EmptyResult();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid stream chat request");
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation in stream chat with agent {AgentId}", request.AgentId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout in stream chat with agent {AgentId}", request.AgentId);
            return StatusCode(500, new { Error = "Request timeout" });
        }
    }

    /// <summary>
    /// 获取聊天历史
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <returns>聊天历史</returns>
    [HttpGet("history/{conversationId}")]
    public async Task<ActionResult<IEnumerable<ChatHistoryItem>>> GetChatHistory(string conversationId)
    {
        try
        {
            var history = await _chatService.GetChatHistoryAsync(conversationId);
            return Ok(history);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid conversation ID");
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation getting chat history for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// 清除聊天历史
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <returns>清除结果</returns>
    [HttpDelete("history/{conversationId}")]
    public async Task<ActionResult> ClearChatHistory(string conversationId)
    {
        try
        {
            var success = await _chatService.ClearChatHistoryAsync(conversationId);
            if (!success)
            {
                return NotFound(new { Error = $"Conversation with ID '{conversationId}' not found" });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation clearing chat history for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }
}

/// <summary>
/// 聊天请求
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// Agent ID
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// 用户消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 会话ID（可选）
    /// </summary>
    public string? ConversationId { get; set; }
}

/// <summary>
/// 聊天响应
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// Agent ID
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// 会话ID
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Agent回复
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}