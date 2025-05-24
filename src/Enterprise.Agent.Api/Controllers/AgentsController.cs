using Enterprise.Agent.Contracts.Agents;
using Enterprise.Agent.Services;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>
/// Agent管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AgentsController : ControllerBase
{
    private readonly IAgentManagementService _agentManagementService;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        IAgentManagementService agentManagementService,
        ILogger<AgentsController> logger)
    {
        _agentManagementService = agentManagementService ?? throw new ArgumentNullException(nameof(agentManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取所有Agent列表
    /// </summary>
    /// <returns>Agent信息列表</returns>
    [HttpGet]
    public ActionResult<IEnumerable<AgentInfo>> GetAgents()
    {
        try
        {
            var agents = _agentManagementService.ListAgents();
            return Ok(agents);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation getting agents list");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// 根据ID获取Agent信息
    /// </summary>
    /// <param name="id">Agent ID</param>
    /// <returns>Agent信息</returns>
    [HttpGet("{id}")]
    public ActionResult<AgentInfo> GetAgent(string id)
    {
        try
        {
            var agentInfo = _agentManagementService.GetAgentInfo(id);
            if (agentInfo == null)
            {
                return NotFound(new { Error = $"Agent with ID '{id}' not found" });
            }

            return Ok(agentInfo);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation getting agent {AgentId}", id);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// 创建新的Agent
    /// </summary>
    /// <param name="request">创建Agent请求</param>
    /// <returns>创建的Agent信息</returns>
    [HttpPost]
    public async Task<ActionResult<AgentInfo>> CreateAgent([FromBody] CreateAgentRequest request)
    {
        if (request == null)
            return BadRequest("Request cannot be null");

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var configuration = new AgentConfiguration
            {
                Name = request.Name,
                Description = request.Description,
                AgentType = request.AgentType,
                SystemMessage = request.SystemMessage,
                ModelName = request.ModelName,
                ToolNames = request.ToolNames ?? Array.Empty<string>(),
                Properties = request.Properties ?? new Dictionary<string, object>()
            };

            var agent = await _agentManagementService.CreateAgentAsync(configuration);
            var agentInfo = _agentManagementService.GetAgentInfo(agent.AgentId);

            return CreatedAtAction(nameof(GetAgent), new { id = agent.AgentId }, agentInfo);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for creating agent");
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation creating agent {AgentName}", request.Name);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// 删除Agent
    /// </summary>
    /// <param name="id">Agent ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAgent(string id)
    {
        try
        {
            var success = await _agentManagementService.RemoveAgentAsync(id);
            if (!success)
            {
                return NotFound(new { Error = $"Agent with ID '{id}' not found" });
            }