using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Employees;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class EmployeeService(ApplicationDbContext context) : IEmployeeService
{
    public async Task<List<Employee>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Employees.Include(e => e.Branch).OrderBy(e => e.LastName).ToListAsync(cancellationToken);

    public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Employees.Include(e => e.Branch).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<(bool Succeeded, string? Error)> CreateAsync(EmployeeViewModel model, CancellationToken cancellationToken = default)
    {
        var branchError = await ValidateBranchAsync(model.BranchId, cancellationToken);
        if (branchError is not null)
        {
            return (false, branchError);
        }

        context.Employees.Add(new Employee
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Position = model.Position,
            BranchId = model.BranchId,
            EmploymentStatus = model.EmploymentStatus,
            ContactNumber = model.ContactNumber,
            Email = model.Email,
            HireDate = model.HireDate,
            MonthlySalary = model.MonthlySalary
        });

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(EmployeeViewModel model, CancellationToken cancellationToken = default)
    {
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == model.Id, cancellationToken);
        if (employee is null)
        {
            return (false, "Employee not found.");
        }

        var branchError = await ValidateBranchAsync(model.BranchId, cancellationToken);
        if (branchError is not null)
        {
            return (false, branchError);
        }

        employee.FirstName = model.FirstName;
        employee.LastName = model.LastName;
        employee.Position = model.Position;
        employee.BranchId = model.BranchId;
        employee.EmploymentStatus = model.EmploymentStatus;
        employee.ContactNumber = model.ContactNumber;
        employee.Email = model.Email;
        employee.HireDate = model.HireDate;
        employee.MonthlySalary = model.MonthlySalary;
        employee.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private async Task<string?> ValidateBranchAsync(int branchId, CancellationToken cancellationToken)
    {
        var isValid = await context.Branches.AnyAsync(b => b.Id == branchId && b.IsActive, cancellationToken);
        return isValid ? null : "Selected branch is not valid.";
    }
}
