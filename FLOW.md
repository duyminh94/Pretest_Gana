# Luồng Hoạt Động Hệ Thống GenAI Document Summarizer

Tài liệu mô tả chi tiết kiến trúc, sơ đồ tuần tự và luồng xử lý của hệ thống **GenAI Document Summarizer** theo các yêu cầu từ **R1 đến R5** trong đề bài Pretest.

---

## 1. Kiến trúc Tổng thể Hệ thống

```mermaid
flowchart TD
    subgraph ClientLayer["Lớp Client"]
        Client["Swagger UI / Postman / HTTP Client"]
    end

    subgraph ApiLayer["Lớp Web API (Pretest_GenAI)"]
        Controller["SummariesController\n(POST /api/summaries)"]
        
        subgraph Services["Dịch Vụ & Xử Lý AI"]
            PromptBuilder["SummaryPromptBuilder\n(Xây dựng Prompt R4)"]
            GeminiService["GeminiSummaryService\n(Tích hợp Gemini R2, R3)"]
            Validator["AiSummaryValidator\n(Kiểm định Output R4)"]
        end

        subgraph DataAccess["Lớp Dữ Liệu"]
            DbContext["AppDbContext\n(Entity Framework Core)"]
        end
    end

    subgraph External["Dịch Vụ Ngoài & Lưu Trữ"]
        GeminiCloud["Google Gemini AI Cloud\n(gemini-2.5-flash)"]
        SqlServer[("SQL Server (Docker: 1433)\nDatabase: GenAIDocumentSummarizer\nTable: SummaryRecords")]
    end

    Client -->|1. Gửi Content| Controller
    Controller -->|2. Yêu cầu tóm tắt| GeminiService
    GeminiService -->|3. Tạo Prompt quy chuẩn| PromptBuilder
    GeminiService -->|4. Gửi nội dung qua API| GeminiCloud
    GeminiCloud -->|5. Trả JSON thô| GeminiService
    GeminiService -->|6. Kiểm tra tính hợp lệ| Validator
    Validator -->|7. Kết quả hợp lệ| Controller
    Controller -->|8. Lưu Entity SummaryRecord| DbContext
    DbContext -->|9. Ghi dữ liệu| SqlServer
    Controller -->|10. Trả HTTP 200 + DTO| Client
```

---

## 2. Sơ đồ Tuần tự (Sequence Diagram)

Mô tả luồng giao tiếp giữa các thành phần từ khi Client gửi request đến khi lưu dữ liệu thành công hoặc bắt lỗi.

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client (Swagger / Postman)
    participant Ctrl as SummariesController
    participant Svc as GeminiSummaryService
    participant PB as SummaryPromptBuilder
    participant AI as Google Gemini API
    participant Val as AiSummaryValidator
    participant DB as AppDbContext (EF Core)
    participant SQL as SQL Server

    Client->>Ctrl: POST /api/summaries { content: "..." }
    
    rect rgb(240, 248, 255)
        Note over Ctrl: Bước 1: Kiểm tra đầu vào (R5)
        alt Content rỗng hoặc chỉ có khoảng trắng
            Ctrl-->>Client: HTTP 400 Bad Request ("Content cannot be missing or blank.")
        end
    end

    Ctrl->>Svc: SummarizeDocumentAsync(content)

    rect rgb(255, 250, 240)
        Note over Svc: Bước 2: Đọc API Key & Chuẩn bị Prompt (R2, R3, R4)
        Svc->>Svc: Đọc GEMINI_API_KEY từ Env hoặc appsettings
        alt Không tìm thấy API Key
            Svc-->>Ctrl: Ném AiConfigurationException
            Ctrl-->>Client: HTTP 503 Service Unavailable
        end
        Svc->>PB: Build(content)
        PB-->>Svc: Prompt (≤100 từ, 3-5 keypoints, JSON, cấm bịa đặt)
    end

    rect rgb(245, 255, 250)
        Note over Svc,AI: Bước 3: Gọi Gemini AI (R2)
        Svc->>AI: GenerateContent(Prompt, ResponseMimeType="application/json")
        alt Lỗi kết nối / Quota / Timeout
            AI-->>Svc: Exception
            Svc-->>Ctrl: Lỗi gọi AI
            Ctrl-->>Client: HTTP 502 Bad Gateway (Ẩn chi tiết lỗi nhạy cảm)
        end
        AI-->>Svc: rawResponse (JSON string)
    end

    rect rgb(255, 240, 245)
        Note over Svc,Val: Bước 4: Kiểm định Output của AI (R4)
        Svc->>Val: Validate(rawResponse)
        Val->>Val: Parse JSON, đếm từ summary (≤100), đếm key points (3..5)
        alt Không hợp lệ (Summary rỗng, >100 từ, <3 hoặc >5 keypoints)
            Val-->>Svc: ValidationResult (IsValid = false)
            Svc-->>Ctrl: Ném AiValidationException (Reject)
            Ctrl-->>Client: HTTP 502 Bad Gateway ("AI generated invalid output...")
        end
        Val-->>Svc: ValidationResult (IsValid = true, Output)
    end

    Svc-->>Ctrl: AiSummaryOutput { summary, keyPoints }

    rect rgb(240, 255, 240)
        Note over Ctrl,SQL: Bước 5: Lưu dữ liệu vào SQL Server (R1)
        Ctrl->>DB: AddAsync(SummaryRecord)
        Ctrl->>DB: SaveChangesAsync()
        DB->>SQL: INSERT INTO SummaryRecords (Content, Summary, KeyPoints, CreatedAt)
        alt Lỗi cơ sở dữ liệu
            SQL-->>DB: SqlException / DbUpdateException
            DB-->>Ctrl: Exception
            Ctrl-->>Client: HTTP 500 Internal Server Error (Ẩn connection string/stack trace)
        end
        SQL-->>DB: Ghi thành công, trả Id tự tăng
    end

    Ctrl-->>Client: HTTP 200 OK { id, content, summary, keyPoints, createdAt }
```

---

## 3. Sơ đồ Luồng Quyết định Chi tiết (Decision Flowchart)

```mermaid
flowchart TD
    Start(["Nhận Request POST /api/summaries"]) --> CheckBody{"Body có rỗng hoặc\nContent là whitespace?"}
    
    CheckBody -->|Đúng| Err400["Trả về HTTP 400 Bad Request\n'Content cannot be missing or blank.'"]
    
    CheckBody -->|Sai| CheckKey{"Có GEMINI_API_KEY\nhoặc Gemini:ApiKey?"}
    
    CheckKey -->|Không| Err503["Trả về HTTP 503 Service Unavailable\n(Ghi log, không lộ secret)"]
    
    CheckKey -->|Có| CallGemini["Gửi Prompt quy chuẩn sang\nGemini 2.5 Flash"]
    
    CallGemini --> CheckAiCall{"Gemini phản hồi\nthành công?"}
    
    CheckAiCall -->|Thất bại / Timeout / Quota| Err502_AI["Trả về HTTP 502 Bad Gateway\n'An error occurred while communicating with the AI service.'"]
    
    CheckAiCall -->|Thành công| ValidateJson{"Output là JSON hợp lệ?"}
    
    ValidateJson -->|Sai| Reject502["Reject: Trả về HTTP 502 Bad Gateway\n'AI generated invalid output...'"]
    
    ValidateJson -->|Đúng| CheckSummary{"Summary không rỗng\nvà số từ <= 100?"}
    
    CheckSummary -->|Sai| Reject502
    
    CheckSummary -->|Đúng| CheckKeyPoints{"KeyPoints từ 3 đến 5 ý\nvà không có ý nào rỗng?"}
    
    CheckKeyPoints -->|Sai| Reject502
    
    CheckKeyPoints -->|Đúng| SaveDb["Tạo Entity SummaryRecord\nvà lưu vào AppDbContext"]
    
    SaveDb --> CheckDb{"Lưu SQL Server\nthành công?"}
    
    CheckDb -->|Lỗi DB| Err500["Trả về HTTP 500 Internal Server Error\n(Ghi log chi tiết, ẩn SQL trace)"]
    
    CheckDb -->|Thành công| Ret200(["Trả về HTTP 200 OK\nkèm kết quả tóm tắt & Id"])
```

---

## 4. Mô hình Dữ liệu (Entity & DTO)

```mermaid
classDiagram
    class SummarizeRequest {
        +string? Content
    }

    class AiSummaryOutput {
        +string Summary
        +List~string~ KeyPoints
    }

    class SummaryRecord {
        +int Id
        +string Content
        +string Summary
        +string KeyPoints
        +DateTime CreatedAt
    }

    class SummaryResponse {
        +int Id
        +string Content
        +string Summary
        +List~string~ KeyPoints
        +DateTime CreatedAt
    }

    SummarizeRequest ..> SummaryRecord : "Input của client"
    AiSummaryOutput ..> SummaryRecord : "Dữ liệu AI sinh ra"
    SummaryRecord ..> SummaryResponse : "Ánh xạ trả về client"
```

---

## 5. Bảng Mã Lỗi và Xử Lý An Toàn (R5)

| Mã HTTP | Tình huống | Thông điệp trả về Client | Xử lý nội bộ (Server) |
| :--- | :--- | :--- | :--- |
| **`200 OK`** | Xử lý thành công toàn bộ luồng | JSON chứa `id`, `content`, `summary`, `keyPoints`, `createdAt` | Ghi log thông tin tóm tắt và thời gian xử lý. |
| **`400 Bad Request`** | Nội dung bị thiếu, chuỗi rỗng hoặc chỉ có khoảng trắng | `{"error": "Content cannot be missing or blank."}` | Không gọi AI, không lưu DB, tiết kiệm chi phí/quota. |
| **`502 Bad Gateway`** | Gemini gặp lỗi mạng/quota HOẶC output của AI vi phạm quy tắc validation (R4) | `{"error": "AI generated invalid output that does not meet the summarization rules."}` | Ghi log chi tiết chuỗi AI trả về để debug; **không lưu DB**. |
| **`503 Service Unavailable`** | Thiếu cấu hình API Key của Gemini | `{"error": "AI service is currently not configured or unavailable."}` | Ghi log cảnh báo thiếu key, hướng dẫn kiểm tra cấu hình. |
| **`500 Internal Server Error`** | Lỗi kết nối hoặc ghi cơ sở dữ liệu SQL Server | `{"error": "A database error occurred while saving the summary record."}` | Ghi log chi tiết Exception, ẩn connection string và stack trace. |
