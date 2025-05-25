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

            var outlineGenerationAgent = (OutlineGenerationAgent)_agentFactory.CreateAgent(
                "OutlineGenerationAgent", 
                $"outline-gen-{projectId}", 
                "Default Outline Generator",
                _languageModelService
            );

            TenderOutline generatedOutline = await outlineGenerationAgent.GenerateOutlineAsync(projectId, project.StructuredRequirementsOutput);

            if (generatedOutline != null && generatedOutline.Status != "Error")
            {
                string outlineJson = JsonSerializer.Serialize(generatedOutline);
                project.CurrentOutlineJson = outlineJson;
                bool outlineSaveSuccess = await _tenderProjectService.UpdateProjectAsync(project);

                if (outlineSaveSuccess)
                {
                    Console.WriteLine($"Generated outline for project {projectId} saved: {outlineJson.Substring(0, Math.Min(outlineJson.Length,100))}...");
                    await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineGenerated");
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
            var project = await _tenderProjectService.GetProjectAsync(projectId);
            if (project == null)
            {
                Console.WriteLine($"Error: Project {projectId} not found in ProcessOutlineFeedbackAsync.");
                return;
            }

            if (string.IsNullOrEmpty(project.CurrentOutlineJson))
            {
                Console.WriteLine($"Error: No current outline found for project {projectId} to process feedback.");
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "FeedbackError_NoOutline");
                return;
            }

            try
            {
                var currentOutline = JsonSerializer.Deserialize<TenderOutline>(project.CurrentOutlineJson);
                if (currentOutline == null)
                {
                    Console.WriteLine($"Error: Failed to deserialize current outline for project {projectId}.");
                    await _tenderProjectService.UpdateProjectStatusAsync(projectId, "FeedbackError_Deserialization");
                    return;
                }

                currentOutline.UserFeedback = userFeedback;
                currentOutline.Status = "FeedbackReceived"; 
                currentOutline.Version += 1; 

                project.CurrentOutlineJson = JsonSerializer.Serialize(currentOutline);
                
                bool updateSuccess = await _tenderProjectService.UpdateProjectAsync(project);
                if (updateSuccess)
                {
                    Console.WriteLine($"User feedback processed and outline updated for project {projectId}. New version: {currentOutline.Version}, Status: {currentOutline.Status}.");
                    await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineFeedbackProcessed"); 
                }
                else
                {
                    Console.WriteLine($"Error: Failed to save updated outline with feedback for project {projectId}.");
                    await _tenderProjectService.UpdateProjectStatusAsync(projectId, "FeedbackError_SaveFailed");
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error deserializing outline for feedback processing on project {projectId}: {jsonEx.Message}");
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "FeedbackError_JsonException");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in ProcessOutlineFeedbackAsync for project {projectId}: {ex.Message}");
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "FeedbackError_Unexpected");
            }
        }

        public async Task ConfirmOutlineAsync(string projectId, string outlineId)
        {
            var project = await _tenderProjectService.GetProjectAsync(projectId);
            if (project == null)
            {
                Console.WriteLine($"Error: Project {projectId} not found in ConfirmOutlineAsync.");
                // Handle error (e.g., by returning or throwing, or setting a specific status if desired)
                return;
            }

            if (string.IsNullOrEmpty(project.CurrentOutlineJson))
            {
                Console.WriteLine($"Error: No current outline found for project {projectId} to confirm.");
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "ConfirmError_NoOutline");
                return;
            }

            try
            {
                var currentOutline = JsonSerializer.Deserialize<TenderOutline>(project.CurrentOutlineJson);
                if (currentOutline == null)
                {
                    Console.WriteLine($"Error: Failed to deserialize current outline for project {projectId} during confirmation.");
                    await _tenderProjectService.UpdateProjectStatusAsync(projectId, "ConfirmError_Deserialization");
                    return;
                }

                // Optional: Validate outlineId. For now, assume confirmation applies to the current outline.
                // if (currentOutline.OutlineId != outlineId)
                // {
                //     Console.WriteLine($"Error: Provided outlineId {outlineId} does not match current outline ID {currentOutline.OutlineId} for project {projectId} during confirmation.");
                //     // Handle mismatch, e.g., by returning or setting a specific status
                //     return;
                // }

                currentOutline.Status = "Confirmed";
                currentOutline.UserFeedback = string.Empty; // Clear feedback as it's now confirmed (optional)
                // Version might or might not be incremented here. Let's assume it doesn't for just confirmation.

                project.CurrentOutlineJson = JsonSerializer.Serialize(currentOutline);
                
                bool updateSuccess = await _tenderProjectService.UpdateProjectAsync(project);
                if (updateSuccess)
                {
                    Console.WriteLine($"Outline for project {projectId} confirmed by user. Status: {currentOutline.Status}.");
                    await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineConfirmed_ContentStructurePending"); 
                }
                else
                {
                    Console.WriteLine($"Error: Failed to save confirmed outline for project {projectId}.");
                    await _tenderProjectService.UpdateProjectStatusAsync(projectId, "ConfirmError_SaveFailed");
                }

                // TODO (Future Step): This is the point to trigger the ContentStructureAgent
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error deserializing outline for confirmation on project {projectId}: {jsonEx.Message}");
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "ConfirmError_JsonException");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in ConfirmOutlineAsync for project {projectId}: {ex.Message}");
                await _tenderProjectService.UpdateProjectStatusAsync(projectId, "ConfirmError_Unexpected");
            }
        }
    }
}
