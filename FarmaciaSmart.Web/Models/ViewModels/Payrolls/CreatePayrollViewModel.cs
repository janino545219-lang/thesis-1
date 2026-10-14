using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Payrolls;

public class CreatePayrollViewModel
{
    [Required, Display(Name = "Employee")]
    public int EmployeeId { get; set; }

    [Required, Display(Name = "Period start")]
    public DateOnly PeriodStart { get; set; }

    [Required, Display(Name = "Period end")]
    public DateOnly PeriodEnd { get; set; }

    public IEnumerable<Employee> Employees { get; set; } = [];
}
