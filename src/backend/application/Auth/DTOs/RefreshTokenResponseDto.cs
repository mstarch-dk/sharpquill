namespace SharpQuill.Application.Auth.DTOs;

public record RefreshTokenResponseDto(
    string AccessToken,
    int ExpiresIn
);
