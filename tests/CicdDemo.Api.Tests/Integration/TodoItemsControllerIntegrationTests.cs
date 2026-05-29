using System.Net;
using System.Net.Http.Json;
using CicdDemo.Api.DTOs;
using CicdDemo.Api.Models;
using CicdDemo.Api.Tests.Helpers;
using FluentAssertions;

namespace CicdDemo.Api.Tests.Integration;

/// <summary>
/// TodoItemsController 的整合測試（Integration Tests）。
///
/// 整合測試與單元測試的差異：
///   - 單元測試：只測試單一類別，用 Mock 隔離所有外部依賴
///   - 整合測試：啟動完整的 HTTP Pipeline，測試從 HTTP 請求到資料庫操作的端到端流程
///
/// IClassFixture&lt;TestWebApplicationFactory&gt;：
///   讓同一個測試類別內的所有測試方法共用同一個 Factory 實例（同一個 HttpClient）。
///   Factory 在第一個測試執行前建立，在最後一個測試結束後銷毀。
///   注意：因為 TestWebApplicationFactory 使用 Guid 命名資料庫，
///   同一個 Factory 內的測試會共用同一個 InMemory DB，測試執行順序可能影響結果。
/// </summary>
public class TodoItemsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TodoItemsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        // CreateClient() 建立一個配置好的 HttpClient，所有請求會送到 TestServer
        // 不需要啟動實際的 HTTP 伺服器，所有通訊都在記憶體中進行
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/todoitems 測試
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_呼叫端點_應回傳200狀態碼與清單()
    {
        // Act：直接對 TestServer 發送 HTTP GET 請求
        var response = await _client.GetAsync("/api/todoitems");

        // Assert：驗證 HTTP 狀態碼和回應可以反序列化
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TodoItem>>();
        items.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST + GET 整合測試（測試跨端點的資料一致性）
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create後GetById_應能取回新建立的資料()
    {
        // Arrange：準備建立請求的 DTO
        var createDto = new CreateTodoItemDto("整合測試待辦事項");

        // Act 1：呼叫 POST 建立新資料
        var createResponse = await _client.PostAsJsonAsync("/api/todoitems", createDto);

        // Assert 1：驗證建立成功，狀態碼應為 201 Created
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItem>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("整合測試待辦事項");
        created.IsCompleted.Should().BeFalse();
        // Location Header 應該指向新資源的 URL
        createResponse.Headers.Location.Should().BeNull();

        // Act 2：用 Id 呼叫 GET 取回剛建立的資料
        var getResponse = await _client.GetAsync($"/api/todoitems/{created.Id}");

        // Assert 2：驗證能夠取回，且資料與建立時一致
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<TodoItem>();
        fetched!.Id.Should().Be(created.Id);
        fetched.Title.Should().Be("整合測試待辦事項");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT 更新測試
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_建立後更新_應反映更改的標題和完成狀態()
    {
        // Arrange：先建立一筆資料
        var created = await CreateItemAsync("待更新的事項");

        // 準備更新的資料
        var updateDto = new UpdateTodoItemDto("已更新的標題", true);

        // Act：呼叫 PUT 更新
        var updateResponse = await _client.PutAsJsonAsync($"/api/todoitems/{created.Id}", updateDto);

        // Assert：驗證更新成功，且回傳的資料反映了變更
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TodoItem>();
        updated!.Title.Should().Be("已更新的標題");
        updated.IsCompleted.Should().BeTrue();
        // UpdatedAt 應該被設定（表示有被修改過）
        updated.UpdatedAt.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE 測試
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_建立後刪除_應回傳204且後續GET回傳404()
    {
        // Arrange：先建立一筆資料
        var created = await CreateItemAsync("待刪除的事項");

        // Act：呼叫 DELETE 刪除
        var deleteResponse = await _client.DeleteAsync($"/api/todoitems/{created.Id}");

        // Assert 1：刪除成功應回傳 204 No Content（成功但無回應體）
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert 2：確認資料已從資料庫中移除，再次 GET 應回傳 404
        var getResponse = await _client.GetAsync($"/api/todoitems/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 健康檢查測試
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_呼叫health端點_應回傳Healthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert：健康檢查端點應回傳 200 OK，且回應體包含 "Healthy"
        // 這驗證了 Program.cs 的 MapHealthChecks 設定正確，
        // 且資料庫連線（InMemory）可正常運作
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 測試輔助方法
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 建立一筆待辦事項的輔助方法，用於 PUT 和 DELETE 測試的 Arrange 階段。
    /// 抽取為私有方法避免重複程式碼，且確保每個測試使用獨立的資料建立流程。
    /// </summary>
    private async Task<TodoItem> CreateItemAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/todoitems", new CreateTodoItemDto(title));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoItem>())!;
    }
}
