using CicdDemo.Api.Data;
using CicdDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CicdDemo.Api.Repositories;

/// <summary>
/// ITodoItemRepository 的 EF Core 實作。
/// 此類別是唯一知道 EF Core 存在的地方，Service 層和測試都不需要直接碰 EF Core API。
/// 生命週期設定為 Scoped（每個 HTTP 請求一個實例），與 AppDbContext 的生命週期一致，
/// 確保同一個請求內的操作共用同一個 DbContext 和 Transaction。
/// </summary>
public class TodoItemRepository : ITodoItemRepository
{
    private readonly AppDbContext _db;

    /// <summary>
    /// 透過建構子注入 AppDbContext，而非直接 new 或使用靜態存取，
    /// 這樣 DI 容器可以管理 DbContext 的生命週期（Scoped）。
    /// </summary>
    public TodoItemRepository(AppDbContext db) => _db = db;

    /// <inheritdoc/>
    /// <remarks>
    /// 使用 AsNoTracking() 讓 EF Core 不追蹤回傳的實體，
    /// 對於只讀查詢可節省記憶體和 CPU，適合 GET All 這種清單場景。
    /// </remarks>
    public async Task<IEnumerable<TodoItem>> GetAllAsync() =>
        await _db.TodoItems.AsNoTracking().ToListAsync();

    /// <inheritdoc/>
    /// <remarks>
    /// FindAsync 優先從 Change Tracker 快取中找，找不到才查資料庫。
    /// 對於後續可能需要修改的單筆查詢（GET by Id → PUT/DELETE）這是較佳選擇。
    /// </remarks>
    public async Task<TodoItem?> GetByIdAsync(int id) =>
        await _db.TodoItems.FindAsync(id);

    /// <inheritdoc/>
    public async Task<TodoItem> AddAsync(TodoItem item)
    {
        await _db.TodoItems.AddAsync(item);
        return item;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Update 只是將實體狀態標記為 Modified，並不會立即執行 SQL UPDATE。
    /// 必須呼叫 SaveChangesAsync 才會真正寫入資料庫。
    /// </remarks>
    public Task UpdateAsync(TodoItem item)
    {
        _db.TodoItems.Update(item);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(TodoItem item)
    {
        _db.TodoItems.Remove(item);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
