using SharpQuill.Application.Tags.DTOs;
using SharpQuill.Core.Tags;


namespace SharpQuill.Application.Tags.Services;

public class AdminTagService : IAdminTagService
{
    private readonly IAdminTagRepository _tagRepository;

    public AdminTagService(IAdminTagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<TagDto?> GetByIdAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null) return null;

        var articleCount = await _tagRepository.GetArticleCountAsync(id);
        return MapToTagDto(tag, articleCount);
    }

    public async Task<IReadOnlyList<TagDto>> GetAllAsync()
    {
        var tags = await _tagRepository.GetAllAsync();
        var result = new List<TagDto>();

        foreach (var tag in tags)
        {
            var articleCount = await _tagRepository.GetArticleCountAsync(tag.Id);
            result.Add(MapToTagDto(tag, articleCount));
        }

        return result;
    }

    public async Task<(TagDto? Tag, string? Error)> CreateAsync(TagCreateDto dto)
    {
        if (await _tagRepository.SlugExistsAsync(dto.Slug))
        {
            return (null, "A tag with this slug already exists");
        }

        var tag = new Tag
        {
            Name = dto.Name,
            Slug = dto.Slug,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _tagRepository.CreateAsync(tag);
        return (MapToTagDto(created, 0), null);
    }

    public async Task<(TagDto? Tag, string? Error)> UpdateAsync(int id, TagUpdateDto dto)
    {
        var existing = await _tagRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return (null, "Tag not found");
        }

        if (await _tagRepository.SlugExistsAsync(dto.Slug, id))
        {
            return (null, "A tag with this slug already exists");
        }

        existing.Name = dto.Name;
        existing.Slug = dto.Slug;

        await _tagRepository.UpdateAsync(existing);

        var articleCount = await _tagRepository.GetArticleCountAsync(id);
        return (MapToTagDto(existing, articleCount), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var existing = await _tagRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return (false, "Tag not found");
        }

        // Tags can be deleted even if they have articles (they will be unlinked via cascade)
        var deleted = await _tagRepository.DeleteAsync(id);
        return (deleted, null);
    }

    private static TagDto MapToTagDto(Tag tag, int articleCount)
    {
        return new TagDto(
            tag.Id,
            tag.Name,
            tag.Slug,
            articleCount
        );
    }
}
