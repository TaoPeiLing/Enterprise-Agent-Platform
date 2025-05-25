using Enterprise.Agent.Contracts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Enterprise.Agent.Services
{
    public interface ITenderProjectService
    {
        Task<TenderProject> CreateProjectAsync(string projectName, string createdBy);
        Task<TenderProject> GetProjectAsync(string projectId);
        Task<bool> UpdateProjectStatusAsync(string projectId, string newStatus);
        Task<IEnumerable<TenderProject>> GetAllProjectsAsync();
        Task<bool> UpdateProjectAsync(TenderProject projectToUpdate); // New method
        // Future methods: AddRequirementDocument, GetProjectProgress, etc.
    }
}
