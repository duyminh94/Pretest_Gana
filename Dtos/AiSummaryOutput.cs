using System.Text.Json.Serialization;

namespace Pretest_GenAI.Dtos
{
    // DTO đại diện cho định dạng JSON AI trả về theo yêu cầu của đề bài
    public class AiSummaryOutput
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("keyPoints")]
        public List<string> KeyPoints { get; set; } = new();
    }
}
