# Enterprise Agent Platform

基于AutoGen for .NET的企业级Agent平台，专门支持国产模型和Ollama。

## 🚀 特性

- **多模型支持**: 支持Ollama本地模型和国产云端模型（通义千问等）
- **模块化架构**: 清晰的分层架构，易于扩展和维护
- **多种Agent类型**: 通用聊天、代码助手、文档助手、数据分析等
- **RESTful API**: 完整的HTTP API接口
- **流式响应**: 支持实时流式聊天
- **会话管理**: 完整的对话历史管理
- **企业级特性**: 日志记录、错误处理、性能监控

## 🏗️ 架构概览

```
Enterprise.Agent.Platform/
├── src/
│   ├── Enterprise.Agent.Contracts/      # 核心接口和契约
│   ├── Enterprise.Agent.Core/           # 核心组件实现
│   ├── Enterprise.Agent.Models.Ollama/  # Ollama模型提供商
│   ├── Enterprise.Agent.Models.Domestic/# 国产模型提供商
│   ├── Enterprise.Agent.Agents/         # Agent实现
│   ├── Enterprise.Agent.Services/       # 业务服务层
│   └── Enterprise.Agent.Api/            # REST API
├── tests/                               # 测试项目
├── docs/                               # 文档
└── scripts/                            # 脚本文件
```

## 🛠️ 技术栈

- **.NET 8.0**: 现代化的.NET平台
- **AutoGen**: Microsoft的多Agent框架
- **ASP.NET Core**: Web API框架
- **Serilog**: 结构化日志记录
- **Swagger/OpenAPI**: API文档
- **xUnit**: 单元测试框架

## 📦 支持的模型

### Ollama本地模型
- **Qwen2.5 7B/14B**: 阿里巴巴通义千问模型
- **Llama 3.1 8B**: Meta的开源模型
- **DeepSeek Coder 6.7B**: 专业代码生成模型