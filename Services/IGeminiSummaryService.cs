using Pretest_GenAI.Dtos;

namespace Pretest_GenAI.Services
{
    // Interface cho dịch vụ tóm tắt nội dung tài liệu sử dụng Google Gemini AI
    public interface IGeminiSummaryService
    {
        Task<AiSummaryOutput> SummarizeDocumentAsync(string content, CancellationToken cancellationToken = default);
    }
}
