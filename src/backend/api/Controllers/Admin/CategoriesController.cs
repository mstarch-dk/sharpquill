using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Categories.DTOs;
using SharpQuill.Application.Categories.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class CategoriesController : ControllerBase
{
    private readonly IAdminCategoryService _categoryService;

    public CategoriesController(IAdminCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(new ApiResponse<IReadOnlyList<CategoryDto>>(categories));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Category with id '{id}' not found"));
        }
        return Ok(new ApiResponse<CategoryDto>(category));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto dto)
    {
        var (category, error) = await _categoryService.CreateAsync(dto);

        if (category == null)
        {
            return BadRequest(new ApiErrorResponse("CREATION_FAILED", error ?? "Failed to create category"));
        }

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new ApiResponse<CategoryDto>(category));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto dto)
    {
        var (category, error) = await _categoryService.UpdateAsync(id, dto);

        if (category == null)
        {
            if (error == "Category not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("UPDATE_FAILED", error ?? "Failed to update category"));
        }

        return Ok(new ApiResponse<CategoryDto>(category));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var (success, error) = await _categoryService.DeleteAsync(id);

        if (!success)
        {
            if (error == "Category not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("DELETE_FAILED", error ?? "Failed to delete category"));
        }

        return NoContent();
    }
}
