using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Series.DTOs;
using SharpQuill.Application.Series.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class SeriesController : ControllerBase
{
    private readonly ISeriesService _seriesService;

    public SeriesController(ISeriesService seriesService)
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

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ApiResponse<SeriesDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeriesBySlug(string slug)
    {
        var series = await _seriesService.GetBySlugAsync(slug);
        if (series == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Series with slug '{slug}' not found"));
        }
        return Ok(new ApiResponse<SeriesDetailDto>(series));
    }
}
