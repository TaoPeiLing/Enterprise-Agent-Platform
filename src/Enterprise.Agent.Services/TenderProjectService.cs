using Enterprise.Agent.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Enterprise.Agent.Services
{
    public class TenderProjectService : ITenderProjectService
    {
        private readonly List<TenderProject> _projects = new List<TenderProject>(); // 内存存储

        public Task<TenderProject> CreateProjectAsync(string projectName, string createdBy)
        {
            var project = new TenderProject
            {
                ProjectId = Guid.NewGuid().ToString(),
                ProjectName = projectName,
                CreatedBy = createdBy,
                CreatedTime = DateTime.UtcNow,
                CurrentStage = "Initial", // 初始阶段
                RequirementDocument = string.Empty, // 初始化为空
                StructuredRequirementsOutput = string.Empty, // Initialize existing new property
                CurrentOutlineJson = string.Empty // Initialize CurrentOutlineJson
            };
            _projects.Add(project);
            return Task.FromResult(project);
        }

        public Task<TenderProject> GetProjectAsync(string projectId)
        {
            var project = _projects.FirstOrDefault(p => p.ProjectId == projectId);
            return Task.FromResult(project); // 如果找不到，会返回 null
        }

        public Task<IEnumerable<TenderProject>> GetAllProjectsAsync()
        {
            return Task.FromResult(_projects.AsEnumerable());
        }

        public Task<bool> UpdateProjectStatusAsync(string projectId, string newStatus)
        {
            var project = _projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project != null)
            {
                project.CurrentStage = newStatus;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> UpdateProjectAsync(TenderProject projectToUpdate)
        {
            if (projectToUpdate == null)
            {
                return Task.FromResult(false); // Or throw ArgumentNullException
            }

            var existingProject = _projects.FirstOrDefault(p => p.ProjectId == projectToUpdate.ProjectId);
            if (existingProject != null)
            {
                // Update all relevant properties from projectToUpdate to existingProject
                existingProject.ProjectName = projectToUpdate.ProjectName;
                existingProject.RequirementDocument = projectToUpdate.RequirementDocument; // Assuming this can be updated
                existingProject.CurrentStage = projectToUpdate.CurrentStage;
                existingProject.StructuredRequirementsOutput = projectToUpdate.StructuredRequirementsOutput; 
                existingProject.CurrentOutlineJson = projectToUpdate.CurrentOutlineJson; // Ensure this line is present and correct
                
                return Task.FromResult(true);
            }
            return Task.FromResult(false); // Project not found
        }
    }
}
