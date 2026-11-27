using SharpQuill.Core.WebsiteUsers;

namespace SharpQuill.Application.WebsiteUsers.DTOs;

public record WebsiteUserDto(
    int Id,
    string Name,
    string Email,
    WebsiteUserStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
