using System.ComponentModel.DataAnnotations;

namespace CicdDemo.Api.DTOs;

/// <summary>
/// 建立待辦事項的請求資料傳輸物件（DTO）。
/// 使用 record 型別：不可變（immutable）、自動產生 Equals/GetHashCode，
/// 且語法簡潔。DataAnnotations 讓 ASP.NET Core 在進入 Controller 前自動驗證。
/// </summary>
/// <param name="Title">
/// 待辦事項標題。最少 1 個字元（防止空白字串），最多 200 個字元（對應資料庫欄位長度）。
/// </param>
public record CreateTodoItemDto(
    [Required(ErrorMessage = "標題為必填欄位")]
    [MinLength(1, ErrorMessage = "標題至少需要 1 個字元")]
    [MaxLength(200, ErrorMessage = "標題最多 200 個字元")]
    string Title
);
