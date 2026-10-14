namespace FarmaciaSmart.Web.Models.Entities;

public class Attendance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly AttendanceDate { get; set; }
    public DateTime? TimeIn { get; set; }
    public DateTime? TimeOut { get; set; }

    public bool IsCorrected { get; set; }
    public string? CorrectionReason { get; set; }
    public string? CorrectedByUserId { get; set; }
    public ApplicationUser? CorrectedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
