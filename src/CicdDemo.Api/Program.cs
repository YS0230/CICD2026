using CicdDemo.Api.Data;
using CicdDemo.Api.Repositories;
using CicdDemo.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════════
// 服務注冊（依賴注入容器設定）
// 原則：先設定服務，再建構應用程式（builder.Build() 之後不能再 AddXxx）
// ═══════════════════════════════════════════════════════════════════════════════

// 注冊 Controller 服務（自動掃描並注冊 Controllers 資料夾下的所有 Controller）
builder.Services.AddControllers();

// 注冊 Swagger 相關服務
// AddEndpointsApiExplorer：讓 Swagger 能偵測 minimal API 端點
// AddSwaggerGen：產生 OpenAPI 規格文件
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title       = "CicdDemo API",
        Version     = "v1",
        Description = "示範 .NET 8 CI/CD 流程的 TodoItem REST API"
    });

    // 載入 XML 文件讓 Swagger UI 顯示 XML 摘要註解（/// <summary>）
    // 需要在 .csproj 中設定 <GenerateDocumentationFile>true</GenerateDocumentationFile>
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// 設定 EF Core + SQLite 資料庫
// 連線字串優先從 appsettings.json 讀取，若未設定則使用預設本機檔案路徑
// 這樣可以透過環境變數或 Docker 覆蓋連線字串，不需要修改程式碼
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=todo.db";
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(connectionString));

// 注冊 Repository 和 Service（Scoped：每個 HTTP 請求一個實例）
// 使用 Scoped 而非 Singleton 是因為 AppDbContext 是 Scoped，
// Repository 和 Service 需要與 DbContext 同生命週期以確保同一個 Transaction
builder.Services.AddScoped<ITodoItemRepository, TodoItemRepository>();
builder.Services.AddScoped<ITodoItemService,    TodoItemService>();

// 健康檢查端點（/health）
// AddDbContextCheck 會嘗試對資料庫執行簡單查詢，確認連線是否正常
// Kubernetes / Docker 的 Liveness / Readiness Probe 常使用此端點
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ═══════════════════════════════════════════════════════════════════════════════
// 應用程式管道（Middleware Pipeline）設定
// 順序非常重要：Middleware 依照加入順序執行，請勿隨意調換
// ═══════════════════════════════════════════════════════════════════════════════

var app = builder.Build();

// Swagger UI 在開發環境和 Docker 環境皆開啟（生產環境可視需求關閉）
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CicdDemo v1"));
}

// 將 HTTP 請求重導向 HTTPS（在反向代理後面執行時可視情況關閉）
app.UseHttpsRedirection();

// 授權 Middleware（此範例未使用驗證，但保留此行以備未來擴充）
app.UseAuthorization();

// 將 Controller 的 Route 屬性對應到 URL
app.MapControllers();

// 映射健康檢查端點，回應格式為純文字 "Healthy" / "Unhealthy"
app.MapHealthChecks("/health");

// ═══════════════════════════════════════════════════════════════════════════════
// 啟動時自動套用資料庫 Migration
// 注意：這個做法適合單一執行個體的 Demo 或小型服務。
// 生產環境建議拆成獨立的 init-container 或 CI/CD 步驟，
// 避免多個執行個體同時啟動時造成 Migration 競爭條件（Race Condition）。
// ═══════════════════════════════════════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Migrate() 只適用於關聯式資料庫（SQLite、SQL Server 等），
    // InMemory DB（整合測試使用）不支援此方法，需要先判斷才能呼叫
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

app.Run();

// 這個 partial class 宣告讓整合測試的 WebApplicationFactory<Program> 能夠存取此 Entry Point
// 若沒有這行，測試專案會因為找不到 Program 類別而編譯失敗
public partial class Program { }
