using CicdDemo.Api.Models;

namespace CicdDemo.Api.Services;

/// <summary>
/// 待辦事項業務邏輯層的介面。
/// Service 層負責：
///   - 業務規則驗證（如欄位 Trim、狀態轉換邏輯）
///   - 協調多個 Repository 操作（若有跨資料表的複雜邏輯）
///   - 隔離 Controller 與資料存取細節
/// Controller 只依賴此介面，不直接操作 Repository，維持清晰的分層責任。
/// </summary>
public interface ITodoItemService
{
    /// <summary>取得所有待辦事項。</summary>
    Task<IEnumerable<TodoItem>> GetAllAsync();

    /// <summary>依 Id 查詢單筆待辦事項，找不到時回傳 null。</summary>
    Task<TodoItem?> GetByIdAsync(int id);

    /// <summary>
    /// 建立新的待辦事項。
    /// Service 層負責對 title 做 Trim 處理，確保不儲存前後空白，
    /// 這個業務規則不應該洩漏到 Controller 或 Repository。
    /// </summary>
    Task<TodoItem> CreateAsync(string title);

    /// <summary>
    /// 更新現有待辦事項的標題和完成狀態。
    /// 找不到指定 Id 時回傳 null（Controller 據此回傳 404）。
    /// </summary>
    Task<TodoItem?> UpdateAsync(int id, string title, bool isCompleted);

    /// <summary>
    /// 刪除指定 Id 的待辦事項。
    /// 回傳 true 表示刪除成功，false 表示找不到該筆記錄（Controller 據此回傳 404）。
    /// </summary>
    Task<bool> DeleteAsync(int id);
}
