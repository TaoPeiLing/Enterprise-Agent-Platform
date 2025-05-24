# Enterprise Agent Platform Dockerfile

# 使用官方.NET 8.0 SDK镜像作为构建环境
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制解决方案文件
COPY ["Enterprise.Agent.Platform.sln", "."]
COPY ["Directory.Build.props", "."]
COPY ["CodeAnalysis.ruleset", "."]

# 复制项目文件
COPY ["src/Enterprise.Agent.Contracts/Enterprise.Agent.Contracts.csproj", "src/Enterprise.Agent.Contracts/"]
COPY ["src/Enterprise.Agent.Core/Enterprise.Agent.Core.csproj", "src/Enterprise.Agent.Core/"]
COPY ["src/Enterprise.Agent.Models.Ollama/Enterprise.Agent.Models.Ollama.csproj", "src/Enterprise.Agent.Models.Ollama/"]
COPY ["src/Enterprise.Agent.Models.Domestic/Enterprise.Agent.Models.Domestic.csproj", "src/Enterprise.Agent.Models.Domestic/"]
COPY ["src/Enterprise.Agent.Agents/Enterprise.Agent.Agents.csproj", "src/Enterprise.Agent.Agents/"]
COPY ["src/Enterprise.Agent.Services/Enterprise.Agent.Services.csproj", "src/Enterprise.Agent.Services/"]
COPY ["src/Enterprise.Agent.Api/Enterprise.Agent.Api.csproj", "src/Enterprise.Agent.Api/"]

# 还原NuGet包
RUN dotnet restore "src/Enterprise.Agent.Api/Enterprise.Agent.Api.csproj"

# 复制所有源代码
COPY . .

# 构建应用
WORKDIR "/src/src/Enterprise.Agent.Api"
RUN dotnet build "Enterprise.Agent.Api.csproj" -c Release -o /app/build

# 发布应用
FROM build AS publish
RUN dotnet publish "Enterprise.Agent.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 使用官方.NET 8.0运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 创建非root用户
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# 复制发布的应用
COPY --from=publish /app/publish .

# 创建日志目录
RUN mkdir -p logs

# 暴露端口
EXPOSE 8080