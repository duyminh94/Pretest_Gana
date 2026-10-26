using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pretest_GenAI.Data;
using Pretest_GenAI.Dtos;
using Pretest_GenAI.Entities;
using Pretest_GenAI.Services;

namespace Pretest_GenAI.Controllers
{
    // API Controller quản lý và xử lý tóm tắt tài liệu (R1, R4, R5)
    [ApiController]
    [Route("api/[controller]")]
    public class SummariesController : ControllerBase
    {
        private readonly IGeminiSummaryService _summaryService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<SummariesController> _logger;

        public SummariesController(
            IGeminiSummaryService summaryService,
            AppDbContext dbContext,
            ILogger<SummariesController> logger)
        {
            _summaryService = summaryService;
            _dbContext = dbContext;
            _logger = logger;
        }

        // Tóm tắt tài liệu, kiểm tra hợp lệ và lưu vào cơ sở dữ liệu
        [HttpPost]
        [Route("")]
        [Route("/api/documents/summarize")]
        [ProducesResponseType(typeof(SummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Summarize([FromBody] SummarizeRequest request, CancellationToken cancellationToken)
        {
            // R5: Trả về HTTP 400 nếu nội dung bị thiếu hoặc chỉ gồm khoảng trắng
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Content cannot be missing or blank." });
            }

            AiSummaryOutput aiOutput;

            // Xử lý gọi Gemini AI và kiểm tra output
            try
            {
                aiOutput = await _summaryService.SummarizeDocumentAsync(request.Content, cancellationToken);
            }
            catch (AiConfigurationException ex)
            {
                _logger.LogError(ex, "Gemini AI service configuration error.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = "AI service is currently not configured or unavailable."
                });
            }
            catch (AiValidationException ex)
            {
                _logger.LogWarning("AI output rejected: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = "AI generated invalid output that does not meet the summarization rules."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini AI API communication error.");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = "An error occurred while communicating with the AI service. Please try again later."
                });
            }

            // Lưu kết quả hợp lệ vào SQL Server qua Entity Framework Core (R1)
            try
            {
                var record = new SummaryRecord
                {
                    Content = request.Content.Trim(),
                    Summary = aiOutput.Summary.Trim(),
                    KeyPoints = JsonSerializer.Serialize(aiOutput.KeyPoints),
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.SummaryRecords.AddAsync(record, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var response = new SummaryResponse
                {
                    Id = record.Id,
                    Content = record.Content,
                    Summary = record.Summary,
                    KeyPoints = aiOutput.KeyPoints,
                    CreatedAt = record.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error occurred while persisting SummaryRecord.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "A database error occurred while saving the summary record."
                });
            }
        }

        // Lấy danh sách toàn bộ các bản ghi tóm tắt đã lưu trong database
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var records = await _dbContext.SummaryRecords
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            var result = records.Select(r => new SummaryResponse
            {
                Id = r.Id,
                Content = r.Content,
                Summary = r.Summary,
                KeyPoints = DeserializeKeyPoints(r.KeyPoints),
                CreatedAt = r.CreatedAt
            });

            return Ok(result);
        }

        // Lấy chi tiết bản ghi tóm tắt theo Id
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var record = await _dbContext.SummaryRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (record == null)
            {
                return NotFound(new { error = $"Summary record with ID {id} was not found." });
            }

            var response = new SummaryResponse
            {
                Id = record.Id,
                Content = record.Content,
                Summary = record.Summary,
                KeyPoints = DeserializeKeyPoints(record.KeyPoints),
                CreatedAt = record.CreatedAt
            };

            return Ok(response);
        }

        // Giải mã chuỗi JSON key points thành danh sách
        private static List<string> DeserializeKeyPoints(string keyPointsJson)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(keyPointsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
