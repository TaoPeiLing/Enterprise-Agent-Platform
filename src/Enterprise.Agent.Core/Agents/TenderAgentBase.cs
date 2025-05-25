using Enterprise.Agent.Core.Agents; // Correct namespace for ChatAgentBase

namespace Enterprise.Agent.Core.Agents 
{
    public abstract class TenderAgentBase : ChatAgentBase
    {
        // Constructor, calling base class constructor
        // Note: ChatAgentBase constructor requires ILanguageModelService
        // We need to decide if TenderAgentBase will also require it, 
        // or if it will be passed differently.
        // For now, let's assume it needs to be passed up.
        // This means the constructor signature from the example needs adjustment.
        protected TenderAgentBase(string agentId, string agentName, ILanguageModelService languageModelService) 
            : base(agentId, agentName, languageModelService)
        {
        }

        // Future common methods or properties specific to tender processing agents can be added here.
    }
}
