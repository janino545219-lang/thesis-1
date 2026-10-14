using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Payrolls;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class PayrollServiceTests
{
    private static Employee Seed(ApplicationDbContext context, decimal monthlySalary = 30000m)
    {
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        context.Branches.Add(branch);
        context.SaveChanges();

        var employee = new Employee
        {
            FirstName = "Ana",
            LastName = "Reyes",
            Position = "Pharmacist",
            BranchId = branch.Id,
            EmploymentStatus = EmploymentStatus.Active,
            HireDate = DateTime.Today,
            MonthlySalary = monthlySalary
        };
        context.Employees.Add(employee);
        context.SaveChanges();

        return employee;
    }

    [Fact]
    public async Task CreateDraftAsync_ComputesBasicPayAsHalfMonthlySalary()
    {
        using var context = TestDbContextFactory.Create();
        var employee = Seed(context, monthlySalary: 30000m);
        var service = new PayrollService(context);

        var (succeeded, error, payrollId) = await service.CreateDraftAsync(new CreatePayrollViewModel
        {
            EmployeeId = employee.Id,
            PeriodStart = new DateOnly(2026, 8, 1),
            PeriodEnd = new DateOnly(2026, 8, 15)
        }, "hr-1");

        Assert.True(succeeded);
        Assert.Null(error);
        var payroll = context.Payrolls.Single(p => p.Id == payrollId);
        Assert.Equal(15000m, payroll.BasicPay);
        Assert.Equal(15000m, payroll.GrossPay);
        Assert.Equal(15000m, payroll.NetPay);
        Assert.Equal(PayrollStatus.Draft, payroll.Status);
    }

    [Fact]
    public async Task CreateDraftAsync_DuplicatePeriod_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var employee = Seed(context);
        var service = new PayrollService(context);
        var model = new CreatePayrollViewModel { EmployeeId = employee.Id, PeriodStart = new DateOnly(2026, 8, 1), PeriodEnd = new DateOnly(2026, 8, 15) };

        await service.CreateDraftAsync(model, "hr-1");
        var (succeeded, error, _) = await service.CreateDraftAsync(model, "hr-1");

        Assert.False(succeeded);
        Assert.Contains("already exists", error);
    }

    [Fact]
    public async Task AddItemAsync_RecalculatesGrossAndNetPay()
    {
        using var context = TestDbContextFactory.Create();
        var employee = Seed(context, monthlySalary: 30000m);
        var service = new PayrollService(context);
        var (_, _, payrollId) = await service.CreateDraftAsync(
            new CreatePayrollViewModel { EmployeeId = employee.Id, PeriodStart = new DateOnly(2026, 8, 1), PeriodEnd = new DateOnly(2026, 8, 15) }, "hr-1");

        await service.AddItemAsync(payrollId!.Value, new PayrollItemViewModel { ItemType = PayrollItemType.Earning, Description = "Overtime", Amount = 1500 });
        await service.AddItemAsync(payrollId.Value, new PayrollItemViewModel { ItemType = PayrollItemType.Deduction, Description = "SSS", Amount = 500 });

        var payroll = context.Payrolls.Single(p => p.Id == payrollId);
        Assert.Equal(16500m, payroll.GrossPay);
        Assert.Equal(500m, payroll.TotalDeductions);
        Assert.Equal(16000m, payroll.NetPay);
    }

    [Fact]
    public async Task AddItemAsync_OnFinalizedPayroll_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var employee = Seed(context);
        var service = new PayrollService(context);
        var (_, _, payrollId) = await service.CreateDraftAsync(
            new CreatePayrollViewModel { EmployeeId = employee.Id, PeriodStart = new DateOnly(2026, 8, 1), PeriodEnd = new DateOnly(2026, 8, 15) }, "hr-1");

        await service.FinalizeAsync(payrollId!.Value, "hr-1");

        var (succeeded, error) = await service.AddItemAsync(
            payrollId.Value, new PayrollItemViewModel { ItemType = PayrollItemType.Earning, Description = "Late addition", Amount = 100 });

        Assert.False(succeeded);
        Assert.Contains("finalized", error);

        // Confirm nothing actually changed in the data, not just that the call reported failure.
        var payroll = context.Payrolls.Single(p => p.Id == payrollId);
        Assert.Equal(15000m, payroll.NetPay);
        Assert.Empty(payroll.Items);
    }

    [Fact]
    public async Task FinalizeAsync_AlreadyFinalized_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var employee = Seed(context);
        var service = new PayrollService(context);
        var (_, _, payrollId) = await service.CreateDraftAsync(
            new CreatePayrollViewModel { EmployeeId = employee.Id, PeriodStart = new DateOnly(2026, 8, 1), PeriodEnd = new DateOnly(2026, 8, 15) }, "hr-1");

        await service.FinalizeAsync(payrollId!.Value, "hr-1");
        var (succeeded, error) = await service.FinalizeAsync(payrollId.Value, "hr-1");

        Assert.False(succeeded);
        Assert.Contains("already finalized", error);
    }
}
