using System.ComponentModel.DataAnnotations;

namespace FarmaciaSmart.Web.Models.ViewModels.Attendances;

public class AttendanceCorrectionViewModel
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly AttendanceDate { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Time in")]
    public DateTime? TimeIn { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Time out")]
    public DateTime? TimeOut { get; set; }

    [Required, StringLength(300)]
    [Display(Name = "Reason for correction")]
    public string CorrectionReason { get; set; } = string.Empty;
}
