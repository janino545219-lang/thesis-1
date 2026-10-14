using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Payrolls;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class PayrollService(ApplicationDbContext context) : IPayrollService
{
    public async Task<List<Payroll>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Payrolls
            .Include(p => p.Employee)
            .OrderByDescending(p => p.PeriodStart)
            .ThenBy(p => p.Employee.LastName)
            .ToListAsync(cancellationToken);

    public async Task<Payroll?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Payrolls
            .Include(p => p.Employee)
            .Include(p => p.Items)
            .Include(p => p.ProcessedByUser)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(bool Succeeded, string? Error, int? PayrollId)> CreateDraftAsync(CreatePayrollViewModel model, string processedByUserId, CancellationToken cancellationToken = default)
    {
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == model.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return (false, "Employee not found.", null);
        }

        if (model.PeriodEnd < model.PeriodStart)
        {
            return (false, "Period end cannot be before period start.", null);
        }

        if (await context.Payrolls.AnyAsync(
                p => p.EmployeeId == model.EmployeeId && p.PeriodStart == model.PeriodStart && p.PeriodEnd == model.PeriodEnd,
                cancellationToken))
        {
            return (false, "A payroll already exists for this employee and period.", null);
        }

        var basicPay = Math.Round(employee.MonthlySalary / 2, 2);

        var payroll = new Payroll
        {
            EmployeeId = model.EmployeeId,
            PeriodStart = model.PeriodStart,
            PeriodEnd = model.PeriodEnd,
            BasicPay = basicPay,
            GrossPay = basicPay,
            TotalDeductions = 0,
            NetPay = basicPay,
            ProcessedByUserId = processedByUserId
        };

        context.Payrolls.Add(payroll);
        await context.SaveChangesAsync(cancellationToken);

        return (true, null, payroll.Id);
    }

    public async Task<(bool Succeeded, string? Error)> AddItemAsync(int payrollId, PayrollItemViewModel model, CancellationToken cancellationToken = default)
    {
        var payroll = await context.Payrolls.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);
        if (payroll is null)
        {
            return (false, "Payroll record not found.");
        }

        if (payroll.Status != PayrollStatus.Draft)
        {
            return (false, "Cannot modify a finalized payroll.");
        }

        context.PayrollItems.Add(new PayrollItem
        {
            PayrollId = payrollId,
            ItemType = model.ItemType,
            Description = model.Description,
            Amount = model.Amount
        });

        await context.SaveChangesAsync(cancellationToken);
        await RecalculateAsync(payrollId, cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> RemoveItemAsync(int payrollItemId, CancellationToken cancellationToken = default)
    {
        var item = await context.PayrollItems.Include(i => i.Payroll).FirstOrDefaultAsync(i => i.Id == payrollItemId, cancellationToken);
        if (item is null)
        {
            return (false, "Payroll item not found.");
        }

        if (item.Payroll.Status != PayrollStatus.Draft)
        {
            return (false, "Cannot modify a finalized payroll.");
        }

        var payrollId = item.PayrollId;
        context.PayrollItems.Remove(item);
        await context.SaveChangesAsync(cancellationToken);
        await RecalculateAsync(payrollId, cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> FinalizeAsync(int payrollId, string processedByUserId, CancellationToken cancellationToken = default)
    {
        var payroll = await context.Payrolls.FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);
        if (payroll is null)
        {
            return (false, "Payroll record not found.");
        }

        if (payroll.Status == PayrollStatus.Finalized)
        {
            return (false, "Payroll is already finalized.");
        }

        payroll.Status = PayrollStatus.Finalized;
        payroll.ProcessedByUserId = processedByUserId;
        await context.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    private async Task RecalculateAsync(int payrollId, CancellationToken cancellationToken)
    {
        var payroll = await context.Payrolls.Include(p => p.Items).FirstAsync(p => p.Id == payrollId, cancellationToken);

        var earnings = payroll.Items.Where(i => i.ItemType == PayrollItemType.Earning).Sum(i => i.Amount);
        var deductions = payroll.Items.Where(i => i.ItemType == PayrollItemType.Deduction).Sum(i => i.Amount);

        payroll.GrossPay = payroll.BasicPay + earnings;
        payroll.TotalDeductions = deductions;
        payroll.NetPay = payroll.GrossPay - payroll.TotalDeductions;

        await context.SaveChangesAsync(cancellationToken);
    }
}
