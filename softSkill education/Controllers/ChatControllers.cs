using Microsoft.AspNetCore.Mvc;
using GenerativeAI;
using GenerativeAI.Types;
using System.Text.Json;

namespace softSkill_education.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IGenerativeAI _generativeAi;
        private readonly IConfiguration _config;

        public ChatController(IGenerativeAI generativeAi, IConfiguration config)
        {
            _generativeAi = generativeAi;
            _config = config;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskGemini([FromBody] ChatRequest request)
        {
            try
            {
                // Lấy model từ cấu hình
                var modelName = _config["Gemini:Model"] ?? "gemini-1.5-pro";

                // Tạo model instance
                var model = _generativeAi.GenerativeModel(modelName);

                // Cấu hình generation settings
                var generationConfig = new GenerationConfig()
                {
                    Temperature = double.Parse(_config["Gemini:Temperature"] ?? "0.7"),
                    MaxOutputTokens = int.Parse(_config["Gemini:MaxTokens"] ?? "2048"),
                    TopP = 0.95,
                    TopK = 40
                };

                // Gọi API
                var response = await model.GenerateContentAsync(request.Message, generationConfig);

                return Ok(new
                {
                    success = true,
                    response = response.Text,
                    model = modelName,
                    usage = new
                    {
                        promptTokens = response.UsageMetadata?.PromptTokenCount,
                        candidatesTokens = response.UsageMetadata?.CandidatesTokenCount,
                        totalTokens = response.UsageMetadata?.TotalTokenCount
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("chat")]
        public async Task<IActionResult> StartChatSession([FromBody] ChatRequest request)
        {
            try
            {
                var model = _generativeAi.GenerativeModel("gemini-1.5-pro");

                // Tạo chat session (duy trì context)
                var chat = model.StartChat();

                // Gửi tin nhắn
                var response = await chat.SendMessageAsync(request.Message);

                return Ok(new
                {
                    sessionId = chat.SessionId,
                    response = response.Text,
                    history = chat.History.Select(h => new
                    {
                        role = h.Role,
                        text = h.Parts?.FirstOrDefault()?.Text
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("multimodal")]
        public async Task<IActionResult> MultimodalChat([FromBody] dynamic request)
        {
            // Ví dụ xử lý đa phương tiện
            var json = JsonSerializer.Serialize(request);
            var jsonDoc = JsonDocument.Parse(json);

            var message = jsonDoc.RootElement.GetProperty("message").GetString();
            var imageUrl = jsonDoc.RootElement.GetProperty("imageUrl").GetString();

            var model = _generativeAi.GenerativeModel("gemini-1.5-pro");

            // Tạo nội dung đa phương tiện
            var contents = new[]
            {
                Content.FromText(message),
                Content.FromImage(new Uri(imageUrl))
            };

            var response = await model.GenerateContentAsync(contents);

            return Ok(new { response = response.Text });
        }

        [HttpGet("models")]
        public async Task<IActionResult> GetAvailableModels()
        {
            try
            {
                // Lấy danh sách model có sẵn
                var models = await _generativeAi.ListModelsAsync();

                return Ok(new
                {
                    success = true,
                    models = models.Select(m => new
                    {
                        name = m.Name,
                        displayName = m.DisplayName,
                        description = m.Description,
                        inputTokenLimit = m.InputTokenLimit,
                        outputTokenLimit = m.OutputTokenLimit
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            var apiKey = _config["Gemini:ApiKey"];
            var hasApiKey = !string.IsNullOrEmpty(apiKey);

            return Ok(new
            {
                status = "online",
                apiKeyConfigured = hasApiKey,
                timestamp = DateTime.UtcNow,
                service = "Gemini AI via gunpal5 SDK"
            });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
        public string? SessionId { get; set; }
        public string? ImageUrl { get; set; }
    }
}