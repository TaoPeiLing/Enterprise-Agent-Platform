using Enterprise.Agent.Contracts.Models;
using System.Threading.Tasks;

namespace Enterprise.Agent.Services
{
    public interface ITenderWorkflowService
    {
        Task StartProjectWorkflowAsync(string projectId, byte[] requirementPdfData);
        Task ProcessOutlineFeedbackAsync(string projectId, string outlineId, string userFeedback);
        Task ConfirmOutlineAsync(string projectId, string outlineId);
        // Future methods: ProcessSectionFeedback, GenerateFullTender, etc.
    }
}
