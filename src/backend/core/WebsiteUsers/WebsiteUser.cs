namespace SharpQuill.Core.WebsiteUsers;

public class WebsiteUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public WebsiteUserStatus Status { get; set; } = WebsiteUserStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
