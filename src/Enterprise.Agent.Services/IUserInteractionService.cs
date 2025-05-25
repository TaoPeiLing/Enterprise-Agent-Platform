using Enterprise.Agent.Contracts.Models; // For TenderOutline, TenderSection if needed
using System.Threading.Tasks;

namespace Enterprise.Agent.Services
{
    public interface IUserInteractionService
    {
        Task NotifyUserOfNewOutlineAsync(string projectId, TenderOutline outline);
        Task<string> GetUserFeedbackOnOutlineAsync(string projectId, string outlineId); // Might involve waiting for user input
        Task NotifyUserOfSectionCompletionAsync(string projectId, TenderSection section);
        // Future methods: HandleVersionControl, ManageAuditTrail, etc.
    }
}
