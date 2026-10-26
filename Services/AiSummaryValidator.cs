using System.Text.Json;
using Pretest_GenAI.Dtos;

namespace Pretest_GenAI.Services
{
    // Xác thực cấu trúc và điều kiện của nội dung JSON do Gemini AI sinh ra (R4)
    public class AiSummaryValidator
    {
        private const int MaxSummaryWords = 100;
        private const int MinKeyPoints = 3;
        private const int MaxKeyPoints = 5;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public record ValidationResult(bool IsValid, string? ErrorMessage, AiSummaryOutput? Output);

        // Làm sạch định dạng markdown code block nếu Gemini bọc json trong ```json ... ```
        public static string CleanJsonText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return string.Empty;
            }

            var trimmed = rawText.Trim();
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[7..];
            }
            else if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[3..];
            }

            if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^3];
            }

            return trimmed.Trim();
        }

        // Đếm số từ trong đoạn văn bằng cách tách theo khoảng trắng
        public static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        // Kiểm tra toàn diện output của AI: cấu trúc JSON, summary rỗng, giới hạn từ, số lượng key points
        public static ValidationResult Validate(string rawResponse)
        {
            var cleanedJson = CleanJsonText(rawResponse);
            if (string.IsNullOrWhiteSpace(cleanedJson))
            {
                return new ValidationResult(false, "AI response is empty.", null);
            }

            AiSummaryOutput? output;
            try
            {
                output = JsonSerializer.Deserialize<AiSummaryOutput>(cleanedJson, JsonOptions);
            }
            catch (JsonException ex)
            {
                return new ValidationResult(false, $"Invalid JSON structure from AI: {ex.Message}", null);
            }

            if (output == null)
            {
                return new ValidationResult(false, "Failed to deserialize AI response into expected structure.", null);
            }

            // Kiểm tra Summary không được rỗng
            if (string.IsNullOrWhiteSpace(output.Summary))
            {
                return new ValidationResult(false, "AI generated an empty summary.", null);
            }

            // Kiểm tra số lượng từ của Summary (tối đa 100 từ)
            var wordCount = CountWords(output.Summary);
            if (wordCount > MaxSummaryWords)
            {
                return new ValidationResult(false, $"Summary exceeds word limit ({wordCount} words, maximum allowed is {MaxSummaryWords}).", null);
            }

            // Kiểm tra KeyPoints không được null
            if (output.KeyPoints == null)
            {
                return new ValidationResult(false, "Key points collection is null.", null);
            }

            // Kiểm tra số lượng KeyPoints từ 3 đến 5
            if (output.KeyPoints.Count < MinKeyPoints || output.KeyPoints.Count > MaxKeyPoints)
            {
                return new ValidationResult(false, $"Key points count must be between {MinKeyPoints} and {MaxKeyPoints}, but got {output.KeyPoints.Count}.", null);
            }

            // Kiểm tra từng key point không được để trống hoặc chỉ có khoảng trắng
            for (var i = 0; i < output.KeyPoints.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(output.KeyPoints[i]))
                {
                    return new ValidationResult(false, $"Key point at index {i + 1} is empty or blank.", null);
                }
            }

            return new ValidationResult(true, null, output);
        }
    }
}
