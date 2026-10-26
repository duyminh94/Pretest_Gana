using Microsoft.EntityFrameworkCore;
using Pretest_GenAI.Data;
using Pretest_GenAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Đọc cấu hình bổ sung từ appsettings.Local.json nếu có (đã được cấu hình trong .gitignore)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký Entity Framework Core với SQL Server (R1)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký dịch vụ Gemini AI (R2, R3, R4)
builder.Services.AddScoped<IGeminiSummaryService, GeminiSummaryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "GenAI Document Summarizer";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
