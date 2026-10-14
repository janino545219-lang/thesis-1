namespace FarmaciaSmart.Web.Models.Entities;

public enum NotificationType
{
    LowStock = 0,
    ExpiringSoon = 1,
    Expired = 2,
    DamagedItem = 3,
    Return = 4,
    Payroll = 5,
    Administrative = 6
}

public class Notification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
