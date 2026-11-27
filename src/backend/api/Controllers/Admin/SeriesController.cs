using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Series.DTOs;
using SharpQuill.Application.Series.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/series")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class SeriesController : ControllerBase
{
    private readonly IAdminSeriesService _seriesService;

    public SeriesController(IAdminSeriesService seriesService)
    {
        _seriesService = seriesService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SeriesDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeries()
    {
        var series = await _seriesService.GetAllAsync();
        return Ok(new ApiResponse<IReadOnlyList<SeriesDto>>(series));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SeriesAdminDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeriesById(int id)
    {
        var series = await _seriesService.GetByIdAsync(id);
        if (series == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Series with id '{id}' not found"));
        }
        return Ok(new ApiResponse<SeriesAdminDetailDto>(series));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SeriesDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSeries([FromBody] SeriesCreateDto dto)
    {
        var (series, error) = await _seriesService.CreateAsync(dto);

        if (series == null)
        {
            return BadRequest(new ApiErrorResponse("CREATION_FAILED", error ?? "Failed to create series"));
        }

        return CreatedAtAction(nameof(GetSeriesById), new { id = series.Id }, new ApiResponse<SeriesDto>(series));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SeriesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSeries(int id, [FromBody] SeriesUpdateDto dto)
    {
        var (series, error) = await _seriesService.UpdateAsync(id, dto);

        if (series == null)
        {
            if (error == "Series not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("UPDATE_FAILED", error ?? "Failed to update series"));
        }

        return Ok(new ApiResponse<SeriesDto>(series));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSeries(int id)
    {
        var (success, error) = await _seriesService.DeleteAsync(id);

        if (!success)
        {
            if (error == "Series not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("DELETE_FAILED", error ?? "Failed to delete series"));
        }

        return NoContent();
    }

    [HttpPost("{id:int}/articles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddArticle(int id, [FromBody] SeriesAddArticleDto dto)
    {
        var (success, error) = await _seriesService.AddArticleAsync(id, dto);

        if (!success)
        {
            if (error?.Contains("not found") == true)
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("ADD_FAILED", error ?? "Failed to add article to series"));
        }

        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}/articles/{articleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveArticle(int id, int articleId)
    {
        var (success, error) = await _seriesService.RemoveArticleAsync(id, articleId);

        if (!success)
        {
            if (error?.Contains("not found") == true)
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("REMOVE_FAILED", error ?? "Failed to remove article from series"));
        }

        return NoContent();
    }

    [HttpPut("{id:int}/articles/reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderArticles(int id, [FromBody] SeriesReorderDto dto)
    {
        var (success, error) = await _seriesService.ReorderArticlesAsync(id, dto);

        if (!success)
        {
            if (error == "Series not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("REORDER_FAILED", error ?? "Failed to reorder articles"));
        }

        return Ok(new { success = true });
    }
}
