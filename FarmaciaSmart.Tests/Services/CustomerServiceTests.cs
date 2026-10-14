using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class CustomerServiceTests
{
    private static Customer Seed(ApplicationDbContext context)
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "jane@example.com",
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Dela Cruz",
            IsActive = true
        };
        context.Users.Add(user);

        var customer = new Customer { UserId = user.Id, Status = CustomerStatus.Active };
        context.Customers.Add(customer);
        context.SaveChanges();

        return customer;
    }

    [Fact]
    public async Task SetStatusAsync_Suspended_AlsoDeactivatesUnderlyingUser()
    {
        using var context = TestDbContextFactory.Create();
        var customer = Seed(context);
        var service = new CustomerService(context);

        var (succeeded, error) = await service.SetStatusAsync(customer.Id, CustomerStatus.Suspended);

        Assert.True(succeeded);
        Assert.Null(error);
        var updated = context.Customers.Single(c => c.Id == customer.Id);
        Assert.Equal(CustomerStatus.Suspended, updated.Status);
        var user = context.Users.Single(u => u.Id == customer.UserId);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task SetStatusAsync_BackToActive_ReactivatesUnderlyingUser()
    {
        using var context = TestDbContextFactory.Create();
        var customer = Seed(context);
        var service = new CustomerService(context);

        await service.SetStatusAsync(customer.Id, CustomerStatus.Suspended);
        var (succeeded, _) = await service.SetStatusAsync(customer.Id, CustomerStatus.Active);

        Assert.True(succeeded);
        var user = context.Users.Single(u => u.Id == customer.UserId);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task SetStatusAsync_Suspended_RotatesSecurityStampSoExistingCookieIsInvalidated()
    {
        using var context = TestDbContextFactory.Create();
        var customer = Seed(context);
        var originalStamp = context.Users.Single(u => u.Id == customer.UserId).SecurityStamp;
        var service = new CustomerService(context);

        await service.SetStatusAsync(customer.Id, CustomerStatus.Suspended);

        var updatedStamp = context.Users.Single(u => u.Id == customer.UserId).SecurityStamp;
        Assert.NotEqual(originalStamp, updatedStamp);
    }

    [Fact]
    public async Task SetStatusAsync_AlreadySuspended_DoesNotRotateStampAgain()
    {
        using var context = TestDbContextFactory.Create();
        var customer = Seed(context);
        var service = new CustomerService(context);
        await service.SetStatusAsync(customer.Id, CustomerStatus.Suspended);
        var stampAfterFirstSuspend = context.Users.Single(u => u.Id == customer.UserId).SecurityStamp;

        await service.SetStatusAsync(customer.Id, CustomerStatus.Suspended);

        var stampAfterSecondSuspend = context.Users.Single(u => u.Id == customer.UserId).SecurityStamp;
        Assert.Equal(stampAfterFirstSuspend, stampAfterSecondSuspend);
    }

    [Fact]
    public async Task SetStatusAsync_UnknownCustomer_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CustomerService(context);

        var (succeeded, error) = await service.SetStatusAsync(999, CustomerStatus.Suspended);

        Assert.False(succeeded);
        Assert.Contains("not found", error);
    }
}
