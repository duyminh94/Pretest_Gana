using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pretest_GenAI.Entities
{
    // Một lần tóm tắt: nội dung gốc, bản tóm tắt và các ý chính do Gemini sinh ra
    [Table("SummaryRecords")]
    public class SummaryRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string Summary { get; set; } = string.Empty;

        // Danh sách ý chính lưu dạng chuỗi JSON, ví dụ ["ý 1","ý 2","ý 3"]
        [Required]
        public string KeyPoints { get; set; } = string.Empty;

        // Lưu giờ UTC để không lệch khi chạy ở máy khác múi giờ
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
