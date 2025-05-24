#!/bin/bash

# Ollama模型初始化脚本
# 用于在Docker容器中自动下载推荐的模型

set -e

OLLAMA_HOST=${OLLAMA_HOST:-"http://localhost:11434"}
echo "🚀 开始初始化Ollama模型..."
echo "📍 Ollama服务地址: $OLLAMA_HOST"

# 等待Ollama服务完全启动
echo "⏳ 等待Ollama服务启动..."
for i in {1..30}; do
    if curl -s "$OLLAMA_HOST/api/tags" > /dev/null 2>&1; then
        echo "✅ Ollama服务已启动"
        break
    fi
    echo "   等待中... ($i/30)"
    sleep 2
done

# 检查服务是否可用
if ! curl -s "$OLLAMA_HOST/api/tags" > /dev/null 2>&1; then
    echo "❌ Ollama服务不可用，退出初始化"
    exit 1
fi

# 定义要下载的模型列表
MODELS=(
    "qwen2.5:7b"
    "deepseek-coder:6.7b"
    "llama3.1:8b"
)

# 下载模型函数
download_model() {
    local model=$1
    echo "📦 开始下载模型: $model"
    
    # 使用ollama pull命令下载模型
    if ollama pull "$model"; then
        echo "✅ 模型 $model 下载成功"
    else
        echo "❌ 模型 $model 下载失败"
        return 1
    fi
}