using Enterprise.Agent.Contracts.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enterprise.Agent.Models.Domestic.Qwen;

/// <summary>
/// 通义千问聊天完成客户端
/// </summary>
public class QwenChatCompletionClient : IChatCompletionClient
{
    private readonly HttpClient _httpClient;
    private readonly ModelInfo _modelInfo;
    private readonly ModelConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly string _endpoint;

    public QwenChatCompletionClient(
        HttpClient httpClient,
        ModelInfo modelInfo,
        ModelConfiguration configuration,
        ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _modelInfo = modelInfo ?? throw new ArgumentNullException(nameof(modelInfo));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _endpoint = _modelInfo.Metadata.TryGetValue("endpoint", out var ep) ? ep.ToString()! :
            "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";

        // 设置认证头
        if (!string.IsNullOrEmpty(_configuration.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration.ApiKey);
        }
    }

    public string ModelName => _modelInfo.Name;

    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var request = CreateQwenRequest(messages, options, false);

        try
        {
            _logger.LogDebug("Sending chat completion request to Qwen for model {ModelName}", ModelName);

            var json = JsonConvert.SerializeObject(request, Formatting.None);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var qwenResponse = JsonConvert.DeserializeObject<QwenResponse>(responseJson);

            return ConvertToStandardResponse(qwenResponse!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Qwen chat completion for model {ModelName}", ModelName);
            throw;
        }
    }

    public async IAsyncEnumerable<ChatCompletionStreamResponse> CreateChatCompletionStreamAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = CreateQwenRequest(messages, options, true);

        _logger.LogDebug("Sending streaming chat completion request to Qwen for model {ModelName}", ModelName);

        var json = JsonConvert.SerializeObject(request, Formatting.None);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]")
                    yield break;

                QwenStreamResponse? chunk = null;
                try
                {
                    chunk = JsonConvert.DeserializeObject<QwenStreamResponse>(data);
                }r
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming response chunk: {Data}", data);
                    continue;
                }

                if (chunk?.Output?.Text != null)
                {
                    yield return new ChatCompletionStreamResponse
                    {
                        Id = chunk.RequestId ?? Guid.NewGuid().ToString(),
                        Model = ModelName,
                        Choices = new[]
                        {
                            new ChatStreamChoice
                            {
                                Index = 0,
                                Delta = new ChatMessageDelta
                                {
                                    Role = "assistant",
                                    Content = chunk.Output.Text
                                },
                                FinishReason = chunk.Output.FinishReason
                            }
                        }
                    };
                }
            }
        }
    }

    private QwenRequest CreateQwenRequest(IEnumerable<ChatMessage> messages, ChatCompletionOptions? options, bool stream)
    {
        var qwenMessages = messages.Select(ConvertToQwenMessage).ToList();

        var request = new QwenRequest
        {
            Model = ModelName,
            Input = new QwenInput
            {
                Messages = qwenMessages
            },
            Parameters = new QwenParameters
            {
                IncrementalOutput = stream
            }
        };

        // 设置参数
        if (options != null)
        {
            if (options.Temperature.HasValue)
                request.Parameters.Temperature = (float)options.Temperature.Value;

            if (options.TopP.HasValue)
                request.Parameters.TopP = (float)options.TopP.Value;

            if (options.MaxTokens.HasValue)
                request.Parameters.MaxTokens = options.MaxTokens.Value;

            if (options.Stop?.Any() == true)
                request.Parameters.Stop = options.Stop.ToArray();
        }

        return request;
    }

    private static QwenMessage ConvertToQwenMessage(ChatMessage message)
    {
        return new QwenMessage
        {
            Role = message.Role,
            Content = message.Content
        };
    }

    private ChatCompletionResponse ConvertToStandardResponse(QwenResponse response)
    {
        var message = new ChatMessage
        {
            Role = "assistant",
            Content = response.Output?.Text ?? string.Empty
        };

        var choice = new ChatChoice
        {
            Index = 0,
            Message = message,
            FinishReason = response.Output?.FinishReason ?? "stop"
        };

        var usage = new UsageInfo
        {
            PromptTokens = response.Usage?.InputTokens ?? 0,
            CompletionTokens = response.Usage?.OutputTokens ?? 0,
            TotalTokens = response.Usage?.TotalTokens ?? 0
        };

        return new ChatCompletionResponse
        {
            Id = response.RequestId ?? Guid.NewGuid().ToString(),
            Model = ModelName,
            Choices = new[] { choice },
            Usage = usage
        };
    }
}

// 通义千问API数据模型
internal class QwenRequest
{
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [JsonProperty("input")]
    public QwenInput Input { get; set; } = new();

    [JsonProperty("parameters")]
    public QwenParameters Parameters { get; set; } = new();
}

internal class QwenInput
{
    [JsonProperty("messages")]
    public List<QwenMessage> Messages { get; set; } = new();
}

internal class QwenMessage
{
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}

internal class QwenParameters
{
    [JsonProperty("temperature")]
    public float? Temperature { get; set; }

    [JsonProperty("top_p")]
    public float? TopP { get; set; }

    [JsonProperty("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonProperty("stop")]
    public string[]? Stop { get; set; }

    [JsonProperty("incremental_output")]
    public bool IncrementalOutput { get; set; }
}

internal class QwenResponse
{
    [JsonProperty("request_id")]
    public string? RequestId { get; set; }

    [JsonProperty("output")]
    public QwenOutput? Output { get; set; }

    [JsonProperty("usage")]
    public QwenUsage? Usage { get; set; }
}

internal class QwenStreamResponse
{
    [JsonProperty("request_id")]
    public string? RequestId { get; set; }

    [JsonProperty("output")]
    public QwenOutput? Output { get; set; }
}

internal class QwenOutput
{
    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("finish_reason")]
    public string? FinishReason { get; set; }
}

internal class QwenUsage
{
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }

    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
}