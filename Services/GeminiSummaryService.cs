using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;
using Pretest_GenAI.Dtos;

namespace Pretest_GenAI.Services
{
    // Dịch vụ tích hợp Google Gemini API để tóm tắt tài liệu (R2, R3, R4)
    public class GeminiSummaryService : IGeminiSummaryService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiSummaryService> _logger;

        public GeminiSummaryService(IConfiguration configuration, ILogger<GeminiSummaryService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Lấy API key từ biến môi trường GEMINI_API_KEY hoặc file cấu hình (R2, R3 - không hardcode)
        private string GetApiKey()
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return apiKey;
            }

            apiKey = _configuration["Gemini:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return apiKey;
            }

            apiKey = _configuration["GEMINI_API_KEY"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return apiKey;
            }

            throw new AiConfigurationException("Gemini API key is not configured. Please set the GEMINI_API_KEY environment variable or configure Gemini:ApiKey in appsettings.");
        }

        public async Task<AiSummaryOutput> SummarizeDocumentAsync(string content, CancellationToken cancellationToken = default)
        {
            var apiKey = GetApiKey();
            var modelName = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

            var googleAi = new GoogleAI(apiKey: apiKey);
            var generationConfig = new GenerationConfig
            {
                Temperature = 0.2f,
                ResponseMimeType = "application/json"
            };

            var model = googleAi.GenerativeModel(modelName, generationConfig);
            var prompt = SummaryPromptBuilder.Build(content);

            _logger.LogInformation("Sending document summarization request to Gemini ({ModelName})...", modelName);

            var response = await model.GenerateContent(prompt, cancellationToken: cancellationToken);
            var rawText = response.Text ?? string.Empty;

            _logger.LogInformation("Received raw response from Gemini: {ResponseText}", rawText);

            // Xác thực cấu trúc và điều kiện JSON do AI trả về (R4)
            var validationResult = AiSummaryValidator.Validate(rawText);
            if (!validationResult.IsValid || validationResult.Output == null)
            {
                _logger.LogWarning("Gemini response rejected: {ErrorMessage}", validationResult.ErrorMessage);
                throw new AiValidationException(validationResult.ErrorMessage ?? "AI output failed validation.");
            }

            return validationResult.Output;
        }
    }
}
