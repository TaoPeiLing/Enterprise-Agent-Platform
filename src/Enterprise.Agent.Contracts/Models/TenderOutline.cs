namespace Enterprise.Agent.Contracts.Models
{
    public class TenderOutline
    {
        public string OutlineId { get; set; }
        public string ProjectId { get; set; }
        public string OutlineContent { get; set; } // 大纲内容（例如，JSON字符串描述树形结构）
        public int Version { get; set; }
        public string Status { get; set; } // 状态（草稿/用户审核中/已确认）
        public string UserFeedback { get; set; }
    }
}
