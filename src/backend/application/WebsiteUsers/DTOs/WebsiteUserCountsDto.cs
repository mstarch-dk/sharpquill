namespace SharpQuill.Application.WebsiteUsers.DTOs;

public record WebsiteUserCountsDto(
    int Pending,
    int Approved,
    int Rejected
);
