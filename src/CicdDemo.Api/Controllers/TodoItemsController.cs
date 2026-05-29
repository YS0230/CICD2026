using CicdDemo.Api.DTOs;
using CicdDemo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CicdDemo.Api.Controllers;

/// <summary>
/// 待辦事項的 RESTful API 控制器。
/// Controller 的職責應該很薄（Thin Controller）：
///   1. 接收 HTTP 請求並解析參數
///   2. 呼叫 Service 層執行業務邏輯
///   3. 將結果轉換成適當的 HTTP 回應（狀態碼 + 回應體）
/// Controller 本身不應包含業務邏輯或直接存取資料庫。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TodoItemsController : ControllerBase
{
    private readonly ITodoItemService _svc;

    /// <summary>建構子注入 Service 介面，不依賴具體實作。</summary>
    public TodoItemsController(ITodoItemService svc) => _svc = svc;

    /// <summary>取得所有待辦事項清單。</summary>
    /// <returns>TodoItem 陣列，無資料時回傳空陣列（不是 404）。</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await _svc.GetAllAsync());

    /// <summary>依 Id 取得單筆待辦事項。</summary>
    /// <param name="id">待辦事項的唯一識別碼（正整數）。</param>
    /// <returns>200 + TodoItem 物件，找不到時回傳 404。</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _svc.GetByIdAsync(id);
        // 使用 null 條件判斷決定回傳 404 或 200，而非拋出 Exception
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>建立新的待辦事項。</summary>
    /// <param name="dto">包含 Title 的請求體（JSON）。</param>
    /// <returns>
    /// 201 Created + 新增的 TodoItem，
    /// Location Header 指向新資源的 GET 端點（符合 REST 規範）。
    /// </returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTodoItemDto dto)
    {
        var item = await _svc.CreateAsync(dto.Title);
        // CreatedAtAction 自動產生 Location Header，指向 GET /api/todoitems/{id}
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    /// <summary>更新現有待辦事項的標題和完成狀態。</summary>
    /// <param name="id">要更新的待辦事項 Id。</param>
    /// <param name="dto">包含 Title 和 IsCompleted 的完整更新資料。</param>
    /// <returns>200 + 更新後的 TodoItem，找不到時回傳 404。</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTodoItemDto dto)
    {
        var item = await _svc.UpdateAsync(id, dto.Title, dto.IsCompleted);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>刪除指定 Id 的待辦事項。</summary>
    /// <param name="id">要刪除的待辦事項 Id。</param>
    /// <returns>204 No Content（刪除成功無回應體），找不到時回傳 404。</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _svc.DeleteAsync(id);
        // 204 No Content 是刪除成功的標準 HTTP 回應，不回傳任何資料
        return deleted ? NoContent() : NotFound();
    }
}
