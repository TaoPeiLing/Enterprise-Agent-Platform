namespace Enterprise.Agent.Contracts.Models
{
    public class TenderSection
    {
        public string SectionId { get; set; }
        public string ProjectId { get; set; }
        public string ParentSectionId { get; set; } // 父章节ID，用于层级
        public string SectionTitle { get; set; }
        public string SectionContent { get; set; }
        public int Order { get; set; } // 排序
        public string Status { get; set; } // 编写状态
        public string AssignedAgent { get; set; } // 负责的智能体ID (可选)
    }
}
