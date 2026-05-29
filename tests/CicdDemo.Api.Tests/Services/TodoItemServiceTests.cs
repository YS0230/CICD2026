using CicdDemo.Api.Models;
using CicdDemo.Api.Repositories;
using CicdDemo.Api.Services;
using FluentAssertions;
using Moq;

namespace CicdDemo.Api.Tests.Services;

/// <summary>
/// TodoItemService 的單元測試（Unit Tests）。
///
/// 單元測試策略：
///   - 使用 Moq 建立 ITodoItemRepository 的 Mock 物件，完全隔離資料庫依賴
///   - 每個 [Fact] 只測試一個行為（Single Responsibility）
///   - 遵循 AAA 結構：Arrange（準備）→ Act（執行）→ Assert（驗證）
///   - 測試名稱格式：方法名稱_情境_預期結果（讓失敗訊息一目了然）
/// </summary>
public class TodoItemServiceTests
{
    private readonly Mock<ITodoItemRepository> _repoMock;
    private readonly ITodoItemService          _sut;   // System Under Test（被測試的系統）

    public TodoItemServiceTests()
    {
        _repoMock = new Mock<ITodoItemRepository>();
        _sut      = new TodoItemService(_repoMock.Object);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAllAsync 測試
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_正常情況下_應回傳Repository的所有資料()
    {
        // Arrange：準備 Mock 回傳的假資料
        var expected = new List<TodoItem>
        {
            new() { Id = 1, Title = "買牛奶" },
            new() { Id = 2, Title = "遛狗" }
        };
        _repoMock.Setup(r => r.GetAllAsync())
                 .ReturnsAsync(expected);

        // Act：呼叫被測試的方法
        var result = await _sut.GetAllAsync();

        // Assert：驗證結果與預期一致，並確認 Repository 只被呼叫一次
        result.Should().BeEquivalentTo(expected);
        _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetByIdAsync 測試
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_指定Id存在_應回傳該筆待辦事項()
    {
        // Arrange
        var item = new TodoItem { Id = 1, Title = "買牛奶" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert：驗證回傳物件的屬性
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Title.Should().Be("買牛奶");
    }

    [Fact]
    public async Task GetByIdAsync_指定Id不存在_應回傳Null()
    {
        // Arrange：模擬 Repository 找不到資料時回傳 null
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TodoItem?)null);

        // Act
        var result = await _sut.GetByIdAsync(99);

        // Assert：Service 應將 null 原封不動回傳給 Controller
        result.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateAsync 測試
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_標題含有首尾空白_應自動Trim後儲存()
    {
        // Arrange：包含前後空白的標題，測試 Service 的 Trim 業務邏輯
        const string titleWithSpaces = "  新任務  ";

        _repoMock.Setup(r => r.AddAsync(It.IsAny<TodoItem>()))
                 .ReturnsAsync((TodoItem t) => t);  // 回傳傳入的物件本身
        _repoMock.Setup(r => r.SaveChangesAsync())
                 .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateAsync(titleWithSpaces);

        // Assert：驗證 Trim 有正確執行，且 Repository 被呼叫了正確的參數
        result.Title.Should().Be("新任務");
        _repoMock.Verify(
            r => r.AddAsync(It.Is<TodoItem>(t => t.Title == "新任務")),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_正常標題_應呼叫AddAsync與SaveChangesAsync各一次()
    {
        // Arrange
        _repoMock.Setup(r => r.AddAsync(It.IsAny<TodoItem>()))
                 .ReturnsAsync((TodoItem t) => t);
        _repoMock.Setup(r => r.SaveChangesAsync())
                 .Returns(Task.CompletedTask);

        // Act
        await _sut.CreateAsync("正常標題");

        // Assert：驗證 Repository 的呼叫次數符合預期（各呼叫一次）
        _repoMock.Verify(r => r.AddAsync(It.IsAny<TodoItem>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateAsync 測試
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_指定Id不存在_應回傳Null且不呼叫Update()
    {
        // Arrange：模擬找不到資料的情況
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                 .ReturnsAsync((TodoItem?)null);

        // Act
        var result = await _sut.UpdateAsync(999, "更新標題", true);

        // Assert：找不到時應提前回傳 null，不應該呼叫 UpdateAsync 或 SaveChangesAsync
        result.Should().BeNull();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TodoItem>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(),                 Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_指定Id存在_應更新屬性並設定UpdatedAt()
    {
        // Arrange
        var existing = new TodoItem { Id = 1, Title = "舊標題", IsCompleted = false };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<TodoItem>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await _sut.UpdateAsync(1, "新標題", true);

        // Assert：驗證所有屬性都正確更新，且 UpdatedAt 有被設定
        result.Should().NotBeNull();
        result!.Title.Should().Be("新標題");
        result.IsCompleted.Should().BeTrue();
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DeleteAsync 測試
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_指定Id存在_應回傳True並呼叫DeleteAsync()
    {
        // Arrange
        var item = new TodoItem { Id = 1, Title = "待刪除" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _repoMock.Setup(r => r.DeleteAsync(item)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var deleted = await _sut.DeleteAsync(1);

        // Assert
        deleted.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteAsync(item), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_指定Id不存在_應回傳False且不呼叫Delete()
    {
        // Arrange：模擬找不到資料
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                 .ReturnsAsync((TodoItem?)null);

        // Act
        var deleted = await _sut.DeleteAsync(999);

        // Assert：找不到時應回傳 false，且不應執行任何刪除操作
        deleted.Should().BeFalse();
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<TodoItem>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(),                 Times.Never);
    }
}
