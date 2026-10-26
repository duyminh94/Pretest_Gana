using Microsoft.EntityFrameworkCore;
using Pretest_GenAI.Entities;

namespace Pretest_GenAI.Data
{
    // Cầu nối giữa code C# và SQL Server: mỗi DbSet tương ứng một bảng
    public class AppDbContext : DbContext
    {
        // Nhận cấu hình (provider, connection string) từ DI, được đăng ký ở Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<SummaryRecord> SummaryRecords => Set<SummaryRecord>();
    }
}
