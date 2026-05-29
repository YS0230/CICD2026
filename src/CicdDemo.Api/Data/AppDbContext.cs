using CicdDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CicdDemo.Api.Data;

/// <summary>
/// EF Core 的資料庫上下文（DbContext）。
/// 所有與資料庫的操作都需透過此類別，它負責：
///   1. 定義資料表對應（DbSet）
///   2. 設定欄位約束（Fluent API，比 DataAnnotations 更靈活）
///   3. 管理連線和交易（Transaction）生命週期
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// 建構子接收 DbContextOptions，讓 DI 容器注入連線設定。
    /// 這個設計讓測試時可以傳入 InMemory 選項，生產環境傳入 SQLite 選項，
    /// AppDbContext 本身不需要知道使用哪種資料庫。
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>待辦事項資料表，使用表達式主體屬性以保持簡潔。</summary>
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    /// <summary>
    /// 使用 Fluent API 設定資料表結構。
    /// 選擇 Fluent API 而非 DataAnnotations 的原因：
    ///   - DataAnnotations 會污染 Domain Model（Model 不應該知道資料庫細節）
    ///   - Fluent API 的設定集中在 DbContext，方便集中管理和審查
    ///   - 某些進階設定（如 HasDefaultValueSql）只有 Fluent API 才支援
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(entity =>
        {
            // 主鍵設定（雖然慣例上 Id 會自動識別，明確設定讓程式碼更清晰）
            entity.HasKey(e => e.Id);

            // Title 欄位：必填且限制長度，對應 DTO 的驗證規則
            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            // IsCompleted 預設值：新增記錄時不需要呼叫端傳入此欄位
            entity.Property(e => e.IsCompleted)
                  .HasDefaultValue(false);

            // CreatedAt 預設值：由資料庫提供 SQL 函數確保記錄準確的時間戳記
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // UpdatedAt 允許 null（可為空值），表示此記錄從未被更新過
            entity.Property(e => e.UpdatedAt)
                  .IsRequired(false);
        });
    }
}
