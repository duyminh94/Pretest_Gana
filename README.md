# GenAI Document Summarizer (ASP.NET Core Web API)

Dự án Web API sử dụng ASP.NET Core (.NET 10) tích hợp Google Gemini AI để tóm tắt văn bản tài liệu và trích xuất các ý chính, xác thực dữ liệu đầu ra và lưu kết quả vào SQL Server theo chuẩn Entity Framework Core.

> 📖 **Xem sơ đồ luồng chi tiết tại**: [FLOW.md](FLOW.md)

---

## 1. Cấu trúc thư mục

```
Pretest_GenAI/
├── Controllers/
│   └── SummariesController.cs       # Endpoint POST /api/summaries, GET
├── Data/
│   └── AppDbContext.cs              # DbContext Entity Framework Core
├── Dtos/
│   ├── SummarizeRequest.cs          # Input DTO { content }
│   ├── AiSummaryOutput.cs           # DTO nhận JSON từ Gemini { summary, keyPoints }
│   └── SummaryResponse.cs           # Output DTO trả về cho client
├── Entities/
│   └── SummaryRecord.cs             # Entity ánh xạ bảng SummaryRecords trong SQL Server
├── Migrations/                      # EF Core Code First Migrations
├── Services/
│   ├── AiExceptions.cs              # Ngoại lệ nghiệp vụ AI
│   ├── AiSummaryValidator.cs        # Xác thực Output AI: JSON, ≤100 từ, 3-5 ý chính
│   ├── GeminiSummaryService.cs      # Service tích hợp Gemini API (không hardcode key)
│   ├── IGeminiSummaryService.cs     # Interface
│   └── SummaryPromptBuilder.cs      # Prompt Engineering (chống bịa đặt facts)
├── FLOW.md                          # Sơ đồ kiến trúc & luồng xử lý chi tiết (Mermaid)
├── Pretest_GenAI.http               # File test sẵn các request (VS Code / Visual Studio)
├── appsettings.json                 # Cấu hình chung (không chứa secret)
├── appsettings.Local.example.json   # Cấu hình mẫu cho local
└── Program.cs                       # Cấu hình DI, DbContext, Swagger UI
```

---

## 2. Hướng dẫn chạy ứng dụng

### Bước 1: Khởi động SQL Server
Khởi động container Docker chứa SQL Server (Azure SQL Edge cổng 1433):
```bash
docker start sqlserver
```

### Bước 2: Cấu hình thông tin kết nối và API Key
Tạo file `Pretest_GenAI/appsettings.Local.json` (file này đã được đưa vào `.gitignore` để không bị lộ):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=GenAIDocumentSummarizer;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-2.5-flash"
  }
}
```
*(Hoặc export biến môi trường `export GEMINI_API_KEY="YOUR_KEY"`).*

### Bước 3: Chạy Migrations (nếu chạy DB mới)
```bash
cd Pretest_GenAI
dotnet ef database update
```

### Bước 4: Khởi chạy API
```bash
dotnet run
```
Truy cập Swagger UI tại: **http://localhost:5226/swagger**

---

## 3. Chạy Unit Tests
Toàn bộ logic xác thực (`AiSummaryValidator`) và Controller (`SummariesController`) đã được viết test xUnit:
```bash
dotnet test ../Pretest_GenAI.Tests
```
Kết quả: **15/15 unit tests Passed**.
