using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FarmaciaSmart.Web.Data;

namespace FarmaciaSmart.Tests;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        // The in-memory provider doesn't support transactions (unlike the real SQL Server target),
        // so services that correctly wrap multi-step writes in a transaction just need that specific
        // warning ignored here rather than treated as an error.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }
}
