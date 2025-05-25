using Enterprise.Agent.Contracts.Models;
using System;
using System.Threading.Tasks;
using Enterprise.Agent.Agents; // Added for AgentFactory, DocumentAnalysisAgent
using Enterprise.Agent.Core.Services; // Added for ILanguageModelService
// using System.Linq; // Not strictly needed for Math.Min as Math is in System namespace

namespace Enterprise.Agent.Services
{
    public class TenderWorkflowService : ITenderWorkflowService
    {
        private readonly IAgentManagementService _agentManagementService; // Kept, though not used in this specific method revision
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly ITenderProjectService _tenderProjectService;
        private readonly AgentFactory _agentFactory; // Added
        private readonly ILanguageModelService _languageModelService; // Added

        public TenderWorkflowService(
            IAgentManagementService agentManagementService,
            IDocumentProcessingService documentProcessingService,
            ITenderProjectService tenderProjectService,
            AgentFactory agentFactory, // Added
            ILanguageModelService languageModelService) // Added
        {
            _agentManagementService = agentManagementService ?? throw new ArgumentNullException(nameof(agentManagementService));
            _documentProcessingService = documentProcessingService ?? throw new ArgumentNullException(nameof(documentProcessingService));
            _tenderProjectService = tenderProjectService ?? throw new ArgumentNullException(nameof(tenderProjectService));
            _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory)); // Added
            _languageModelService = languageModelService ?? throw new ArgumentNullException(nameof(languageModelService)); // Added
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

            // Use AgentFactory to create DocumentAnalysisAgent
            var documentAnalysisAgent = (DocumentAnalysisAgent)_agentFactory.CreateAgent(
                "DocumentAnalysisAgent", // This is the type string AgentFactory expects
                $"doc-analyzer-{projectId}", // Example instance ID, making it unique per project
                $"Document Analyzer for {projectId}", // Example instance name
                _languageModelService        // Pass the ILanguageModelService
            );
            
            string structuredRequirements = await documentAnalysisAgent.ProcessDocumentAsync(extractedText);

            Console.WriteLine($"Structured requirements for project {projectId}: {structuredRequirements.Substring(0, Math.Min(structuredRequirements.Length, 200))}..."); // Log a preview
            // TODO: Store structuredRequirements, e.g., associate with the project or pass to the next agent

            await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineGenerationPending");
            Console.WriteLine($"Workflow for project {projectId} advanced. Structured requirements processed.");
            // Next step would be to trigger OutlineGenerationAgent
        }

        public async Task ProcessOutlineFeedbackAsync(string projectId, string outlineId, string userFeedback)
        {
            // TODO: Retrieve outline, store feedback, potentially trigger OutlineGenerationAgent again
            await _tenderProjectService.UpdateProjectStatusAsync(projectId, "OutlineRegenerationPending");
            Console.WriteLine($"Feedback for outline {outlineId} on project {projectId} received: {userFeedback}");
            // This would likely involve an OutlineGenerationAgent
        }

        public async Task ConfirmOutlineAsync(string projectId, string outlineId)
        {
            // TODO: Update outline status to Confirmed
            await _tenderProjectService.UpdateProjectStatusAsync(projectId, "ContentStructurePending");
            Console.WriteLine($"Outline {outlineId} for project {projectId} confirmed by user.");
            // Next step would be to trigger ContentStructureAgent
        }
    }
}
