using Enterprise.Agent.Contracts.Models;
using System;
using System.Threading.Tasks;
using Enterprise.Agent.Agents; // For AgentFactory, DocumentAnalysisAgent, OutlineGenerationAgent
using Enterprise.Agent.Core.Services; // For ILanguageModelService
using System.Text.Json; // For JsonSerializer

namespace Enterprise.Agent.Services
{
    public class TenderWorkflowService : ITenderWorkflowService
    {
        private readonly IAgentManagementService _agentManagementService;
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly ITenderProjectService _tenderProjectService;
        private readonly AgentFactory _agentFactory;
        private readonly ILanguageModelService _languageModelService;

        public TenderWorkflowService(
            IAgentManagementService agentManagementService,
            IDocumentProcessingService documentProcessingService,
            ITenderProjectService tenderProjectService,
            AgentFactory agentFactory,
            ILanguageModelService languageModelService)
        {
            _agentManagementService = agentManagementService ?? throw new ArgumentNullException(nameof(agentManagementService));
            _documentProcessingService = documentProcessingService ?? throw new ArgumentNullException(nameof(documentProcessingService));
            _tenderProjectService = tenderProjectService ?? throw new ArgumentNullException(nameof(tenderProjectService));
            _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
            _languageModelService = languageModelService ?? throw new ArgumentNullException(nameof(languageModelService));
        }

        public async Task StartProjectWorkflowAsync(string projectId, byte[] requirementPdfData)
        {
            await _tenderProjectService.UpdateProjectStatusAsync(projectId, "DocumentProcessing");
            string extractedText = await _documentProcessingService.ExtractTextFromPdfAsync(requirementPdfData);
            
            if (string.IsNullOrEmpty(extractedText))
            {
                Console.WriteLine($"No text extracted from PDF for project {projectId}. Aborting further processing in this step.");
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "DocumentProcessingFailed");
                return;
            }

            await _tenderProjectService.UpdateProjectStatusAsync(projectId, "RequirementAnalysis");

            var documentAnalysisAgent = (DocumentAnalysisAgent)_agentFactory.CreateAgent(
                "DocumentAnalysisAgent", 
                $"doc-analyzer-{projectId}", 
                $"Document Analyzer for {projectId}", 
                _languageModelService
            );
            
            string structuredRequirements = await documentAnalysisAgent.ProcessDocumentAsync(extractedText);
            Console.WriteLine($"Structured requirements for project {projectId} (raw): {structuredRequirements.Substring(0, Math.Min(structuredRequirements.Length, 200))}...");

            var project = await _tenderProjectService.GetProjectAsync(projectId);
            if (project == null)
            {
                Console.WriteLine($"Error: Project {projectId} not found when trying to save structured requirements or generate outline.");
                // It's crucial to stop if project is not found at this stage.
                // Although GetProjectAsync might not change status, we reflect a critical failure.
                // Consider if UpdateProjectStatusAsync should even be called if project is null.
                // For now, following the pattern of trying to update status to reflect the error.
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "CriticalErrorProjectNotFound");
                return;
            }
            
            project.StructuredRequirementsOutput = structuredRequirements;
            bool srUpdateSuccess = await _tenderProjectService.UpdateProjectAsync(project);
            if (srUpdateSuccess)
            {
                Console.WriteLine($"Structured requirements saved for project {projectId}.");
            }
            else
            {
                Console.WriteLine($"Error: Failed to update project {projectId} with structured requirements. Aborting outline generation.");
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "UpdateFailedSR");
                return;
            }

            Console.WriteLine($"Project {projectId} status updated. Now attempting to generate outline...");
            await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineGenerationInProgress");

            // Create OutlineGenerationAgent
            var outlineGenerationAgent = (OutlineGenerationAgent)_agentFactory.CreateAgent(
                "OutlineGenerationAgent", // Type string for AgentFactory
                $"outline-gen-{projectId}", // Example instance ID, make it unique per project/run
                "Default Outline Generator",
                _languageModelService
            );

            // Generate the outline using the structured requirements stored in the project object
            TenderOutline generatedOutline = await outlineGenerationAgent.GenerateOutlineAsync(projectId, project.StructuredRequirementsOutput);

            if (generatedOutline != null && generatedOutline.Status != "Error")
            {
                // Serialize TenderOutline to JSON
                string outlineJson = JsonSerializer.Serialize(generatedOutline);

                // Update project with the generated outline JSON
                // Re-fetch the project to ensure we have the latest version before updating CurrentOutlineJson,
                // especially if other processes could modify it, or if UpdateProjectAsync doesn't return the updated object.
                // However, in this sequential flow within a single method, using the existing 'project' instance
                // should be safe if TenderProjectService's UpdateProjectAsync modifies the instance in-place
                // or if no other modifications are expected between the SR update and this outline update.
                // For simplicity and based on the current structure, we'll use the existing 'project' instance.
                project.CurrentOutlineJson = outlineJson;
                bool outlineSaveSuccess = await _tenderProjectService.UpdateProjectAsync(project);

                if (outlineSaveSuccess)
                {
                    Console.WriteLine($"Generated outline for project {projectId} saved: {outlineJson.Substring(0, Math.Min(outlineJson.Length,100))}...");
                    await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineGenerated"); // Or "PendingOutlineReview"
                }
                else
                {
                    Console.WriteLine($"Error: Failed to save generated outline for project {projectId}.");
                    await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineGenerationFailed");
                }
            }
            else
            {
                Console.WriteLine($"Error: Outline generation failed or returned an error state for project {projectId}. Outline content: {generatedOutline?.OutlineContent}");
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineGenerationFailed");
            }
        }

        public async Task ProcessOutlineFeedbackAsync(string projectId, string outlineId, string userFeedback)
        {
            await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineRegenerationPending");
            Console.WriteLine($"Feedback for outline {outlineId} on project {projectId} received: {userFeedback}");
        }

        public async Task ConfirmOutlineAsync(string projectId, string outlineId)
        {
            await _tenderProjectService.UpdateProjectStatusAsync(projectId, "ContentStructurePending");
            Console.WriteLine($"Outline {outlineId} for project {projectId} confirmed by user.");
        }
    }
}
