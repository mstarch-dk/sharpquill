namespace SharpQuill.Core.WebsiteUsers;

public class WebsiteLoginToken
{
    public int Id { get; set; }
    public int WebsiteUserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public WebsiteUser WebsiteUser { get; set; } = null!;

    public bool IsValid => UsedAt == null && DateTime.UtcNow < ExpiresAt;
}
