using SharpQuill.Application.Tags.DTOs;
using SharpQuill.Core.Tags;
using SharpQuill.Application.Tags.Repositories;

namespace SharpQuill.Application.Tags.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<IReadOnlyList<TagDto>> GetAllAsync()
    {
        var tags = await _tagRepository.GetAllWithArticleCountAsync();
        return tags.Select(t =>
        {
            var articleCount = (t as TagWithCount)?.ArticleCount ?? 0;
            return new TagDto(t.Id, t.Name, t.Slug, articleCount);
        }).ToList();
    }

    public async Task<TagDto?> GetBySlugAsync(string slug)
    {
        var tag = await _tagRepository.GetBySlugAsync(slug);
        return tag == null
            ? null
            : new TagDto(tag.Id, tag.Name, tag.Slug, 0);
    }
}
