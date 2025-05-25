// src/Enterprise.Agent.Agents/AgentFactory.cs
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Services;

namespace Enterprise.Agent.Agents 
{
    public class AgentFactory
    {
        private readonly ILanguageModelService _languageModelService;

        public AgentFactory(ILanguageModelService languageModelService)
        {
            _languageModelService = languageModelService;
        }

        public AgentBase CreateAgent(string agentType, string agentId, string agentName)
        {
            switch (agentType)
            {
                case "ChatAgent":
                    return new ChatAgent(agentId, agentName, _languageModelService);
                
                case "DocumentAnalysisAgent":
                    return new DocumentAnalysisAgent(agentId, agentName, _languageModelService);
                case "OutlineGenerationAgent":
                    return new OutlineGenerationAgent(agentId, agentName, _languageModelService);
                case "ContentStructureAgent":
                    return new ContentStructureAgent(agentId, agentName, _languageModelService);
                case "SectionWritingAgent":
                    return new SectionWritingAgent(agentId, agentName, _languageModelService);
                case "ContentIntegrationAgent":
                    return new ContentIntegrationAgent(agentId, agentName, _languageModelService);

                default:
                    throw new ArgumentException($"Unknown agent type: {agentType}");
            }
        }
    }
}
