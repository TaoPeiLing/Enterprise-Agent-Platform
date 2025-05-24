# Enterprise Agent Platform 开发环境启动脚本

Write-Host "🚀 启动 Enterprise Agent Platform 开发环境..." -ForegroundColor Green

# 检查.NET SDK
Write-Host "📋 检查.NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK版本: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ 未找到.NET SDK，请先安装.NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

# 检查Ollama
Write-Host "📋 检查Ollama服务..." -ForegroundColor Yellow
try {
    $ollamaResponse = Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method Get -TimeoutSec 5
    Write-Host "✅ Ollama服务运行正常" -ForegroundColor Green
    
    # 显示可用模型
    if ($ollamaResponse.models -and $ollamaResponse.models.Count -gt 0) {
        Write-Host "📦 可用的Ollama模型:" -ForegroundColor Cyan
        foreach ($model in $ollamaResponse.models) {
            Write-Host "   - $($model.name)" -ForegroundColor White
        }
    } else {
        Write-Host "⚠️  未找到Ollama模型，建议下载推荐模型:" -ForegroundColor Yellow
        Write-Host "   ollama pull qwen2.5:7b" -ForegroundColor White
        Write-Host "   ollama pull deepseek-coder:6.7b" -ForegroundColor White
    }
} catch {
    Write-Host "⚠️  Ollama服务未运行或不可访问" -ForegroundColor Yellow
    Write-Host "   请确保Ollama已安装并运行在 http://localhost:11434" -ForegroundColor White
    Write-Host "   安装命令: curl -fsSL https://ollama.ai/install.sh | sh" -ForegroundColor White
}

# 构建解决方案
Write-Host "🔨 构建解决方案..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 构建失败" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 构建成功" -ForegroundColor Green

# 运行测试
Write-Host "🧪 运行单元测试..." -ForegroundColor Yellow
$testResult = dotnet test tests/Enterprise.Agent.Tests.Unit --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  部分测试失败，但继续启动服务" -ForegroundColor Yellow
} else {
    Write-Host "✅ 所有测试通过" -ForegroundColor Green
}

# 创建日志目录
$logDir = "src/Enterprise.Agent.Api/logs"
if (!(Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    Write-Host "📁 创建日志目录: $logDir" -ForegroundColor Cyan
}

# 启动API服务
Write-Host "🌐 启动API服务..." -ForegroundColor Yellow
Write-Host "📍 API地址: https://localhost:5001" -ForegroundColor Cyan
Write-Host "📖 API文档: https://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host "🔍 健康检查: https://localhost:5001/health" -ForegroundColor Cyan
Write-Host "" -ForegroundColor White
Write-Host "按 Ctrl+C 停止服务" -ForegroundColor Gray

Set-Location "src/Enterprise.Agent.Api"
dotnet run --configuration Debug