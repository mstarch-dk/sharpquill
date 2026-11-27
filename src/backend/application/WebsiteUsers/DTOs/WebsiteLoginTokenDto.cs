namespace SharpQuill.Application.WebsiteUsers.DTOs;

public record WebsiteLoginTokenDto(
    string Token,
    DateTime ExpiresAt
);
