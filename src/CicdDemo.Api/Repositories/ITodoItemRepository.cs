using CicdDemo.Api.Models;

namespace CicdDemo.Api.Repositories;

/// <summary>
/// 待辦事項 Repository 的介面（抽象層）。
/// 定義介面的目的：
///   1. 讓 Service 層只依賴介面，不直接依賴 EF Core 或資料庫，符合依賴反轉原則（DIP）
///   2. 方便在單元測試中以 Mock 物件替換，隔離外部依賴
///   3. 未來若需要換成 Dapper 或其他 ORM，只需新增實作類別，不需修改 Service 層
/// </summary>
public interface ITodoItemRepository
{
    /// <summary>取得所有待辦事項，使用 AsNoTracking 避免不必要的 Change Tracking 開銷。</summary>
    Task<IEnumerable<TodoItem>> GetAllAsync();

    /// <summary>
    /// 依 Id 查詢單筆待辦事項。
    /// 找不到時回傳 null（而非拋出 Exception），讓 Service 層決定如何處理。
    /// </summary>
    Task<TodoItem?> GetByIdAsync(int id);

    /// <summary>新增一筆待辦事項到資料庫（需搭配 SaveChangesAsync 才會真正寫入）。</summary>
    Task<TodoItem> AddAsync(TodoItem item);

    /// <summary>標記一筆待辦事項為「已修改」狀態（需搭配 SaveChangesAsync 才會真正更新）。</summary>
    Task UpdateAsync(TodoItem item);

    /// <summary>標記一筆待辦事項為「待刪除」狀態（需搭配 SaveChangesAsync 才會真正刪除）。</summary>
    Task DeleteAsync(TodoItem item);

    /// <summary>
    /// 將所有掛起的變更（新增、修改、刪除）一次性寫入資料庫。
    /// 將 SaveChanges 獨立出來是為了支援 Unit of Work 模式：
    /// 可以在一個 Transaction 內執行多個操作，最後統一提交。
    /// </summary>
    Task SaveChangesAsync();
}
