namespace CicdDemo.Api.Models;

/// <summary>
/// 待辦事項的資料模型（對應資料庫的 TodoItems 資料表）。
/// 此類別由 EF Core 負責管理，屬性名稱會直接對應欄位名稱。
/// </summary>
public class TodoItem
{
    /// <summary>主鍵，由資料庫自動遞增產生，不需要手動設定。</summary>
    public int Id { get; set; }

    /// <summary>待辦事項的標題，長度限制由 AppDbContext 的 Fluent API 設定（最長 200 字）。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>是否已完成。預設為 false（未完成），完成後透過 PUT 端點更新。</summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 建立時間，使用 UTC 時間以避免時區問題。
    /// 儲存時由應用程式層設定，而非交給資料庫 DEFAULT 處理，
    /// 這樣整合測試與生產環境的行為保持一致。
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最後更新時間，初始值為 null（表示從未被更新過）。
    /// 只有在呼叫 PUT 端點後才會被設定，方便追蹤修改歷史。
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
