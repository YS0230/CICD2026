using CicdDemo.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CicdDemo.Api.Tests.Helpers;

/// <summary>
/// 整合測試用的 WebApplicationFactory，將生產環境的 SQLite 檔案資料庫
/// 替換為 SQLite in-memory（Shared Cache 模式）資料庫。
///
/// 為什麼不用 EF Core InMemory provider：
///   EF Core 9 加強了驗證，若 DI 容器同時存在兩個不同的 provider（SQLite + InMemory），
///   即使已嘗試移除舊的 options，EF Core 仍會拋出「多個 provider」例外。
///   改用 SQLite in-memory 可以保持單一 provider（SQLite），完全繞開此限制。
///
/// Shared Cache 模式說明：
///   SQLite in-memory 資料庫在「最後一個連線關閉時」即消失。
///   _keepAlive 連線負責讓資料庫在測試期間持續存在；
///   EF Core 的每個 DbContext 另外開啟自己的連線，透過 Shared Cache 連到同一個資料庫。
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // 唯一的名稱確保每個 Factory 實例有自己的資料庫（避免不同測試類別互相干擾）
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    // 保持開啟的「錨點連線」：只要此連線存在，in-memory 資料庫就不會消失
    private readonly SqliteConnection _keepAlive;

    public TestWebApplicationFactory()
    {
        _keepAlive = new SqliteConnection($"Data Source={_dbName};Mode=Memory;Cache=Shared");
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // 移除原本指向 .db 檔的 SQLite 設定
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // 改用 SQLite in-memory（Shared Cache）
            // 同樣是 SQLite provider，不會觸發 EF Core 9 的「多個 provider」驗證
            // Program.cs 的 db.Database.Migrate() 會在啟動時建立 Schema（IsRelational() = true）
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={_dbName};Mode=Memory;Cache=Shared"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _keepAlive.Dispose();   // 關閉錨點連線，in-memory 資料庫隨之釋放

        base.Dispose(disposing);
    }
}
