using CicdDemo.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CicdDemo.Api.Tests.Helpers;

/// <summary>
/// 整合測試用的 WebApplicationFactory，將生產環境的 SQLite 替換為 InMemory 資料庫。
/// 為什麼要替換：
///   1. SQLite 需要磁碟 I/O，且在平行測試時會有檔案鎖定問題
///   2. InMemory DB 速度更快，且每個 Factory 實例可以有獨立的資料庫（避免測試間互相干擾）
///   3. CI 環境可能沒有寫入磁碟的權限
///
/// 繼承 WebApplicationFactory&lt;Program&gt; 可以啟動完整的 HTTP Pipeline，
/// 讓整合測試能夠測試從 HTTP 請求到資料庫操作的完整流程。
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 設定為測試環境，避免觸發生產環境才有的設定
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // 移除原本由 Program.cs 注冊的 DbContext 設定（SQLite 版本）
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // 先產生資料庫名稱再傳入 lambda，而非在 lambda 內部呼叫 Guid.NewGuid()。
            // 若在 lambda 內部呼叫，每次 DbContext 被 DI 容器解析時都會產生新 Guid，
            // 導致每個 HTTP 請求都連到不同的 InMemory 資料庫，資料無法跨請求共用。
            var dbName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // 確保 InMemory 資料庫結構已建立（等同於執行 Migration）
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
