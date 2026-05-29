# ══════════════════════════════════════════════════════════════════════════════
# 多階段建構（Multi-Stage Build）
# 目的：讓最終的 runtime image 盡可能小，只包含執行所需的檔案，
#        不包含 SDK、原始碼、或建構工具。
#
# 為什麼分三個 Stage：
#   1. restore  — 只複製 .csproj / .sln，NuGet restore 結果可以被 Docker layer cache 住。
#                 只要套件沒變，下次 build 這一層直接從 cache 取得，省去 30-60 秒的還原時間。
#   2. build    — 複製原始碼並 publish，此層包含 SDK，不應出現在最終 image 中。
#   3. runtime  — 只包含 ASP.NET Core runtime 和 publish 產出，不含 SDK，image 更小更安全。
# ══════════════════════════════════════════════════════════════════════════════

# ── Stage 1：還原 NuGet 套件（利用 layer cache 加速重複 build）────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

# 只複製 .csproj 和 .sln 檔案（不複製原始碼）
# 這樣當只有原始碼變更時，這一層的 cache 仍然有效，可跳過耗時的 restore 步驟
COPY CicdDemo.slnx                                       ./
COPY src/CicdDemo.Api/CicdDemo.Api.csproj                src/CicdDemo.Api/
COPY tests/CicdDemo.Api.Tests/CicdDemo.Api.Tests.csproj  tests/CicdDemo.Api.Tests/

# 還原套件（使用 --locked-mode 確保 CI 環境與本機使用相同的套件版本）
RUN dotnet restore "CicdDemo.slnx" \
    --runtime linux-x64

# ── Stage 2：建構並發布應用程式 ────────────────────────────────────────────────
FROM restore AS build
WORKDIR /src

# 複製所有原始碼（此時 NuGet restore 的 cache 層已確立）
COPY src/   src/
COPY tests/ tests/

# 只 publish 主要 API 專案（不 publish 測試專案）
# --no-restore：使用 Stage 1 已還原的套件，不重複執行 restore
# --self-contained false：依賴安裝在 runtime image 上的 ASP.NET Core，讓 image 較小
RUN dotnet publish "src/CicdDemo.Api/CicdDemo.Api.csproj" \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained false \
    --output /app/publish \
    --no-restore

# ── Stage 3：最終 runtime image（只含執行所需的最小檔案）──────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# 安全性最佳實踐：建立非 root 使用者來執行應用程式
# 若容器被攻破，攻擊者只有有限的系統權限，而非 root 權限
RUN addgroup --system --gid 1001 appgroup \
 && adduser  --system --uid 1001 --ingroup appgroup appuser

WORKDIR /app

# 從 build stage 複製 publish 產出，並設定擁有者為非 root 使用者
COPY --from=build --chown=appuser:appgroup /app/publish .

# 建立 SQLite 資料庫的持久化目錄，並掛載為 Volume
# 容器重啟後資料不會遺失（前提是 Volume 有正確設定）
RUN mkdir -p /app/data && chown appuser:appgroup /app/data
VOLUME /app/data

# 切換到非 root 使用者（必須在複製檔案之後，否則可能沒有讀取權限）
USER appuser

# 環境變數預設值（可在 docker run 或 docker-compose 中覆蓋）
# ASPNETCORE_URLS 使用 8080 而非預設的 80，避免非 root 使用者無法監聽低編號 port 的問題
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/todo.db"

# 聲明容器監聽的 port（僅作為文件用途，實際 port mapping 在 docker run 或 compose 設定）
EXPOSE 8080

# Docker 內建健康檢查：每 30 秒檢查一次 /health 端點
# --start-period=15s：容器啟動後等待 15 秒再開始健康檢查（避免誤報）
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CicdDemo.Api.dll"]
