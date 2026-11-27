using SharpQuill.Application.LinkPreviews.DTOs;

namespace SharpQuill.Application.LinkPreviews.Services;

public interface ILinkPreviewService
{
    Task<LinkPreviewDto> FetchPreviewAsync(string url);
}
