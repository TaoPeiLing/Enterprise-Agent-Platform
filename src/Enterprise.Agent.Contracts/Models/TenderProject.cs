namespace Enterprise.Agent.Contracts.Models
{
    public class TenderProject
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string RequirementDocument { get; set; } // 原始需求文档路径或引用
        public string CurrentStage { get; set; } // 当前处理阶段
        public string CreatedBy { get; set; }
        public DateTime CreatedTime { get; set; }
        public string StructuredRequirementsOutput { get; set; } 
        public string CurrentOutlineJson { get; set; } // 新增属性
    }
}
