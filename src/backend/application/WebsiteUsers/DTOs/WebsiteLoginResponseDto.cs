namespace SharpQuill.Application.WebsiteUsers.DTOs;

public record WebsiteLoginResponseDto(
    string AccessToken,
    int ExpiresIn,
    WebsiteUserProfileDto User
);
