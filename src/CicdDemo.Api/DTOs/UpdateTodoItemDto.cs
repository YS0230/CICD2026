using System.ComponentModel.DataAnnotations;

namespace CicdDemo.Api.DTOs;

/// <summary>
/// 更新待辦事項的請求 DTO。
/// PUT 語意要求呼叫端提供完整的欄位（Title + IsCompleted），
/// 而不是像 PATCH 只傳部分欄位，這樣可以避免「空白 PATCH」造成誤判。
/// </summary>
/// <param name="Title">更新後的標題，驗證規則同 CreateTodoItemDto。</param>
/// <param name="IsCompleted">更新後的完成狀態，true 表示已完成。</param>
public record UpdateTodoItemDto(
    [Required(ErrorMessage = "標題為必填欄位")]
    [MinLength(1, ErrorMessage = "標題至少需要 1 個字元")]
    [MaxLength(200, ErrorMessage = "標題最多 200 個字元")]
    string Title,

    bool IsCompleted
);
