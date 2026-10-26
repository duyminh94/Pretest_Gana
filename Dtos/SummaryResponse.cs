namespace Pretest_GenAI.Dtos
{
    // DTO kết quả API trả về cho client sau khi tóm tắt và lưu vào SQL Server
    public class SummaryResponse
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public List<string> KeyPoints { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }
}
