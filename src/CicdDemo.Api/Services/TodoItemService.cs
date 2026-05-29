using CicdDemo.Api.Models;
using CicdDemo.Api.Repositories;

namespace CicdDemo.Api.Services;

/// <summary>
/// ITodoItemService 的實作，負責待辦事項的業務邏輯。
/// 此類別不直接引用 EF Core，所有資料存取都透過 ITodoItemRepository 介面，
/// 這讓單元測試可以用 Moq 替換 Repository，完全隔離資料庫依賴。
/// </summary>
public class TodoItemService : ITodoItemService
{
    private readonly ITodoItemRepository _repo;

    /// <summary>建構子注入 Repository 介面，而非具體的 EF Core 實作。</summary>
    public TodoItemService(ITodoItemRepository repo) => _repo = repo;

    /// <inheritdoc/>
    public Task<IEnumerable<TodoItem>> GetAllAsync() =>
        _repo.GetAllAsync();

    /// <inheritdoc/>
    public Task<TodoItem?> GetByIdAsync(int id) =>
        _repo.GetByIdAsync(id);

    /// <inheritdoc/>
    public async Task<TodoItem> CreateAsync(string title)
    {
        // Trim 是業務規則：儲存前去除首尾空白，避免「  Buy milk  」和「Buy milk」被視為不同記錄
        var item = new TodoItem { Title = title.Trim() };
        await _repo.AddAsync(item);
        await _repo.SaveChangesAsync();
        return item;
    }

    /// <inheritdoc/>
    public async Task<TodoItem?> UpdateAsync(int id, string title, bool isCompleted)
    {
        var item = await _repo.GetByIdAsync(id);

        // 找不到時提前回傳 null，讓 Controller 決定回傳 404，而非拋出 Exception
        if (item is null) return null;

        item.Title       = title.Trim();
        item.IsCompleted = isCompleted;
        // 記錄最後修改時間，使用 UTC 以確保時區一致性
        item.UpdatedAt   = DateTime.UtcNow;

        await _repo.UpdateAsync(item);
        await _repo.SaveChangesAsync();
        return item;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _repo.GetByIdAsync(id);

        // 找不到時回傳 false（而非拋出 Exception），讓呼叫端決定如何處理
        if (item is null) return false;

        await _repo.DeleteAsync(item);
        await _repo.SaveChangesAsync();
        return true;
    }
}
