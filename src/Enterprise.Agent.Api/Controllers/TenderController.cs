using Enterprise.Agent.Services; // For service interfaces
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks; // For async methods
using Microsoft.AspNetCore.Http; // For IFormFile, StatusCodes
using System.IO; // For MemoryStream
using Enterprise.Agent.Contracts.Models; // For TenderProject, TenderOutline
using System; // For Exception, Console, ArgumentNullException
using System.Collections.Generic; // For List<object>
using System.Text.Json; // For JsonSerializer

namespace Enterprise.Agent.Api.Controllers
{
    [ApiController]
    [Route("api/tender")] // Base route for this controller
    public class TenderController : ControllerBase
    {
        private readonly ITenderProjectService _tenderProjectService;
        private readonly ITenderWorkflowService _tenderWorkflowService;
        // private readonly IDocumentProcessingService _documentProcessingService; 
        // private readonly IUserInteractionService _userInteractionService;

        public TenderController(
            ITenderProjectService tenderProjectService,
            ITenderWorkflowService tenderWorkflowService,
            IDocumentProcessingService documentProcessingService,
            IUserInteractionService userInteractionService)
        {
            _tenderProjectService = tenderProjectService ?? throw new ArgumentNullException(nameof(tenderProjectService));
            _tenderWorkflowService = tenderWorkflowService ?? throw new ArgumentNullException(nameof(tenderWorkflowService));
            // _documentProcessingService = documentProcessingService; 
            // _userInteractionService = userInteractionService; 
        }

        // POST api/tender/projects
        [HttpPost("projects")]
        public async Task<IActionResult> CreateTenderProject([FromBody] TenderProjectCreationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProjectName) || string.IsNullOrWhiteSpace(request.CreatedBy))
            {
                return BadRequest("ProjectName and CreatedBy are required.");
            }

            var project = await _tenderProjectService.CreateProjectAsync(request.ProjectName, request.CreatedBy);
            if (project == null)
            {
                return BadRequest("Failed to create project."); 
            }
            return Ok(project);
        }

        // POST api/tender/projects/{id}/upload
        [HttpPost("projects/{id}/upload")]
        public async Task<IActionResult> UploadRequirementDocument(string id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            var project = await _tenderProjectService.GetProjectAsync(id);
            if (project == null)
            {
                return NotFound($"Project with ID '{id}' not found.");
            }

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            try
            {
                await _tenderWorkflowService.StartProjectWorkflowAsync(id, fileBytes);
                return Ok(new { Message = $"File uploaded successfully for project {id}. Workflow started." });
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error processing file for project {id}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while starting the project workflow.");
            }
        }

        // GET api/tender/projects/{id}/outline
        [HttpGet("projects/{id}/outline")]
        public async Task<IActionResult> GetProjectOutline(string id)
        {
            var project = await _tenderProjectService.GetProjectAsync(id);
            if (project == null)
            {
                return NotFound($"Project with ID '{id}' not found.");
            }

            if (string.IsNullOrEmpty(project.CurrentOutlineJson))
            {
                // No outline has been generated or saved yet.
                return NotFound($"No outline found or generated yet for project with ID '{id}'.");
            }

            try
            {
                // Deserialize the TenderOutline from the JSON string
                var tenderOutline = JsonSerializer.Deserialize<TenderOutline>(project.CurrentOutlineJson);
                if (tenderOutline == null)
                {
                    // This case should ideally not happen if CurrentOutlineJson is valid JSON
                    // and represents a TenderOutline object.
                    Console.WriteLine($"Error: Deserialized outline is null for project {id}. JSON: {project.CurrentOutlineJson}");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to deserialize the stored outline (result was null).");
                }
                return Ok(tenderOutline);
            }
            catch (JsonException jsonEx)
            {
                // Log the deserialization exception: jsonEx.Message
                Console.WriteLine($"Error deserializing outline for project {id}: {jsonEx.Message}. JSON: {project.CurrentOutlineJson}"); 
                return StatusCode(StatusCodes.Status500InternalServerError, "The stored outline data is corrupted or invalid.");
            }
            catch (Exception ex)
            {
                // Log any other unexpected exception: ex.Message
                Console.WriteLine($"Unexpected error retrieving outline for project {id}: {ex.Message}"); 
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the outline.");
            }
        }

        // PUT api/tender/projects/{id}/outline
        [HttpPut("projects/{id}/outline")]
        public async Task<IActionResult> UpdateProjectOutline(string id, [FromBody] OutlineUpdateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OutlineId) || string.IsNullOrWhiteSpace(request.UserFeedback))
            {
                return BadRequest("OutlineId and UserFeedback are required.");
            }

            var project = await _tenderProjectService.GetProjectAsync(id);
            if (project == null)
            {
                return NotFound($"Project with ID '{id}' not found.");
            }

            try
            {
                await _tenderWorkflowService.ProcessOutlineFeedbackAsync(id, request.OutlineId, request.UserFeedback);
                return Ok(new { Message = $"Feedback for outline {request.OutlineId} on project {id} received and is being processed." });
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error processing outline feedback for project {id}, outline {request.OutlineId}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing outline feedback.");
            }
        }

        // POST api/tender/projects/{id}/outline/confirm
        [HttpPost("projects/{id}/outline/confirm")]
        public async Task<IActionResult> ConfirmProjectOutline(string id, [FromBody] OutlineConfirmationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OutlineId))
            {
                return BadRequest("OutlineId is required for confirmation.");
            }

            var project = await _tenderProjectService.GetProjectAsync(id);
            if (project == null)
            {
                return NotFound($"Project with ID '{id}' not found.");
            }

            try
            {
                await _tenderWorkflowService.ConfirmOutlineAsync(id, request.OutlineId);
                return Ok(new { Message = $"Outline {request.OutlineId} for project {id} has been confirmed." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error confirming outline {request.OutlineId} for project {id}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while confirming the outline.");
            }
        }

        // GET /api/tender/projects/{id}/sections
        [HttpGet("projects/{id}/sections")]
        public async Task<IActionResult> GetProjectSections(string id)
        {
            var project = await _tenderProjectService.GetProjectAsync(id);
            if (project == null)
            {
                return NotFound($"Project with ID '{id}' not found.");
            }

            var simulatedSections = new List<object>
            {
                new { SectionId = "section-001", ProjectId = id, ParentSectionId = "simulated-outline-001", SectionTitle = "Chapter 1: Introduction", Order = 1, Status = "Completed" },
                new { SectionId = "section-002", ProjectId = id, ParentSectionId = "simulated-outline-001", SectionTitle = "Chapter 2: Requirements", Order = 2, Status = "InProgress" }
            };
            return Ok(simulatedSections);
        }

        // PUT /api/tender/projects/{id}/sections/{sectionId}
        [HttpPut("projects/{id}/sections/{sectionId}")]
        public async Task<IActionResult> UpdateProjectSection(string id, string sectionId, [FromBody] SectionUpdateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SectionContent))
            {
                return BadRequest("SectionContent is required.");
            }

            var project = await _tenderProjectService.GetProjectAsync(id);
            if (project == null)
            {
                return NotFound($"Project with ID '{id}' not found.");
            }
            
            Console.WriteLine($"Simulated update for project {id}, section {sectionId} with content: {request.SectionContent.Substring(0, Math.Min(request.SectionContent.Length, 50))}...");
            return Ok(new { Message = $"Section {sectionId} in project {id} updated (simulated)." });
        }

        // POST /api/tender/projects/{id}/generate
        [HttpPost("projects/{id}/generate")]
        public async Task<IActionResult> GenerateFullTender(string id)
        {
            var project = await _tenderProjectService.GetProjectAsync(id);
            if (project == null)
            {
                return NotFound($"Project with ID '{id}' not found.");
            }

            try
            {
                Console.WriteLine($"Simulated generation of full tender document for project {id}.");
                return Ok(new { Message = "Full tender document generation process started (simulated).", DocumentReference = $"generated_tender_{id}.docx" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating full tender for project {id}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during the generation of the full tender document.");
            }
        }
    }
}

// Request DTO for creating a tender project
public class TenderProjectCreationRequest
{
    public string ProjectName { get; set; }
    public string CreatedBy { get; set; }
}

// Request DTO for updating an outline (providing feedback)
public class OutlineUpdateRequest
{
    public string OutlineId { get; set; } 
    public string UserFeedback { get; set; }
}

// Request DTO for confirming an outline
public class OutlineConfirmationRequest
{
    public string OutlineId { get; set; }
}

// Request DTO for updating a section
public class SectionUpdateRequest
{
    public string SectionContent { get; set; }
}
