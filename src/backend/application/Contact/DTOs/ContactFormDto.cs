namespace SharpQuill.Application.Contact.DTOs;

public record ContactFormDto(
    string Name,
    string Email,
    string? Subject,
    string Message
);
