# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

.NET 8 CI/CD 範例專案，示範完整的 GitHub Actions 流程。核心功能為 TodoItem CRUD API，搭配三層架構（Controller → Service → Repository）。

## 常用指令

```powershell
# 建構
dotnet build CicdDemo.slnx --configuration Release

# 執行所有測試（含單元測試 + 整合測試）
dotnet test CicdDemo.slnx --configuration Release

# 執行單一測試（依名稱篩選）
dotnet test CicdDemo.slnx --filter "DisplayName~GetByIdAsync"

# 執行測試並產生覆蓋率報告
dotnet test tests/CicdDemo.Api.Tests --collect:"XPlat Code Coverage"

# 本機啟動 API（預設 http://localhost:5000，Swagger: /swagger）
dotnet run --project src/CicdDemo.Api

# Docker 本機驗證
docker build -t cicddemo:local .
docker-compose up -d
# 驗證：curl http://localhost:8080/health

# 新增 EF Core Migration（結構變更後執行）
dotnet ef migrations add <MigrationName> --project src/CicdDemo.Api --startup-project src/CicdDemo.Api --output-dir Data/Migrations
```

> **注意**：本機 SDK 為 .NET 10，solution 檔使用新格式 `CicdDemo.slnx`（非 `.sln`）。

## 架構

### 分層結構

```
HTTP 請求
  → Controller（路由、HTTP 狀態碼轉換）
  → Service（業務邏輯，如 Title.Trim()、UpdatedAt 設定）
  → Repository（EF Core 資料存取，唯一知道 EF Core 存在的地方）
  → AppDbContext（SQLite / InMemory）
```

- **Controller** 只做路由解析和 HTTP 回應轉換，不含業務邏輯
- **Service** 依賴 `ITodoItemRepository` 介面（非具體類別），便於測試時 Mock
- **Repository** 使用 `AsNoTracking()` 處理只讀查詢，`FindAsync` 處理需後續修改的查詢

### 資料庫

- 生產 / 開發環境：SQLite，連線字串從 `ConnectionStrings:DefaultConnection` 讀取，預設 `Data Source=todo.db`
- 啟動時自動執行 `db.Database.Migrate()`，但加了 `IsRelational()` 判斷以支援測試環境
- Migration 檔位於 `src/CicdDemo.Api/Data/Migrations/`

### 測試策略

- **單元測試**（`tests/.../Services/`）：使用 Moq mock `ITodoItemRepository`，完全不碰資料庫
- **整合測試**（`tests/.../Integration/`）：使用 `WebApplicationFactory<Program>` 啟動完整 HTTP pipeline，以 InMemory DB 替換 SQLite

**關鍵陷阱**：`TestWebApplicationFactory` 中的 InMemory DB 名稱必須在 lambda 外部產生（`var dbName = $"TestDb_{Guid.NewGuid()}"` 再傳入），否則每次 DI 容器解析 DbContext 時都會產生新的資料庫，導致跨請求資料遺失。

### GitHub Actions

- `ci.yml`：push / PR 到 `main` 或 `develop` → Build + Test + 上傳 coverage artifact
- `cd.yml`：push 到 `main` → 重跑 CI（job: `ci`）→ Docker multi-arch build（amd64 + arm64）→ 推送到 Docker Hub
- CD 所需 Secrets：`DOCKERHUB_USERNAME`、`DOCKERHUB_TOKEN`

### Docker

- Dockerfile 使用三階段建構：`restore`（只複製 .csproj 以利用 layer cache）→ `build`（publish）→ `runtime`（最小 image，非 root user）
- 容器以非 root user（uid 1001）執行
- SQLite 資料持久化透過 Volume 掛載至 `/app/data`
- Swagger UI 在 `Docker` 環境（`ASPNETCORE_ENVIRONMENT=Docker`）下開啟
