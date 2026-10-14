using System.Text.Json;
using Microsoft.AspNetCore.Http;
using FarmaciaSmart.Tests.Fakes;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class CustomerCartServiceTests
{
    private static (CustomerCartService Service, ISession Session) CreateService()
    {
        var httpContext = new DefaultHttpContext { Session = new FakeSession() };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        return (new CustomerCartService(accessor), httpContext.Session);
    }

    [Fact]
    public void AddItem_ThenGetCart_ReturnsTheItem()
    {
        var (service, _) = CreateService();

        service.AddItem(1, "Biogesic 500mg", 85.50m, 2);
        var items = service.GetCart();

        Assert.Single(items);
        Assert.Equal(2, items[0].Quantity);
    }

    [Fact]
    public void AddItem_SameProductTwice_AccumulatesQuantity()
    {
        var (service, _) = CreateService();

        service.AddItem(1, "Biogesic 500mg", 85.50m, 2);
        service.AddItem(1, "Biogesic 500mg", 85.50m, 3);
        var items = service.GetCart();

        Assert.Single(items);
        Assert.Equal(5, items[0].Quantity);
    }

    [Fact]
    public void GetCart_CartYoungerThan24Hours_IsReturnedIntact()
    {
        var (service, session) = CreateService();
        var freshJson = JsonSerializer.Serialize(new
        {
            CreatedAt = DateTime.UtcNow.AddHours(-23),
            Items = new[] { new { ProductId = 1, ProductName = "Biogesic 500mg", UnitPrice = 85.50m, Quantity = 4 } }
        });
        session.SetString("Shop_Cart", freshJson);

        var items = service.GetCart();

        Assert.Single(items);
        Assert.Equal(4, items[0].Quantity);
    }

    [Fact]
    public void GetCart_CartOlderThan24Hours_IsDiscarded()
    {
        var (service, session) = CreateService();
        var staleJson = JsonSerializer.Serialize(new
        {
            CreatedAt = DateTime.UtcNow.AddHours(-25),
            Items = new[] { new { ProductId = 1, ProductName = "Biogesic 500mg", UnitPrice = 85.50m, Quantity = 30 } }
        });
        session.SetString("Shop_Cart", staleJson);

        var items = service.GetCart();

        Assert.Empty(items);
    }

    [Fact]
    public void AddItem_ToAnExpiredCart_StartsFreshRatherThanExtendingTheOldOne()
    {
        var (service, session) = CreateService();
        var staleJson = JsonSerializer.Serialize(new
        {
            CreatedAt = DateTime.UtcNow.AddHours(-25),
            Items = new[] { new { ProductId = 1, ProductName = "Biogesic 500mg", UnitPrice = 85.50m, Quantity = 30 } }
        });
        session.SetString("Shop_Cart", staleJson);

        service.AddItem(2, "Alaxan FR", 12.00m, 1);
        var items = service.GetCart();

        Assert.Single(items);
        Assert.Equal(2, items[0].ProductId);
    }
}
