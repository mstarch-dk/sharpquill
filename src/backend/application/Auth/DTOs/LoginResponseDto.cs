namespace SharpQuill.Application.Auth.DTOs;

public record LoginResponseDto(
    string AccessToken,
    int ExpiresIn,
    AuthorProfileDto Author
);
