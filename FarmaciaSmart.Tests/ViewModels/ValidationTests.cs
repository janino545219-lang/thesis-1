using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Inventories;
using FarmaciaSmart.Web.Models.ViewModels.Products;
using FarmaciaSmart.Web.Models.ViewModels.Suppliers;

namespace FarmaciaSmart.Tests.ViewModels;

public class ValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ProductViewModel_ValidModel_HasNoErrors()
    {
        var model = new ProductViewModel { Name = "Biogesic", CategoryId = 1, Price = 85.5m, ProductType = ProductType.Pharmacy };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void ProductViewModel_MissingName_FailsValidation()
    {
        var model = new ProductViewModel { Name = "", CategoryId = 1, Price = 85.5m, ProductType = ProductType.Pharmacy };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(ProductViewModel.Name)));
    }

    [Fact]
    public void ProductViewModel_ZeroPrice_FailsValidation()
    {
        var model = new ProductViewModel { Name = "Biogesic", CategoryId = 1, Price = 0, ProductType = ProductType.Pharmacy };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(ProductViewModel.Price)));
    }

    [Fact]
    public void ProductViewModel_NegativePrice_FailsValidation()
    {
        var model = new ProductViewModel { Name = "Biogesic", CategoryId = 1, Price = -5, ProductType = ProductType.Pharmacy };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(ProductViewModel.Price)));
    }

    [Fact]
    public void ProductViewModel_NameExceedingMaxLength_FailsValidation()
    {
        var model = new ProductViewModel { Name = new string('A', 201), CategoryId = 1, Price = 85.5m, ProductType = ProductType.Pharmacy };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(ProductViewModel.Name)));
    }

    [Fact]
    public void SupplierViewModel_ValidModel_HasNoErrors()
    {
        var model = new SupplierViewModel { Name = "MedSupply Co.", Email = "orders@medsupply.ph", Phone = "+63 917 123 4567" };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void SupplierViewModel_InvalidEmail_FailsValidation()
    {
        var model = new SupplierViewModel { Name = "MedSupply Co.", Email = "not-an-email" };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(SupplierViewModel.Email)));
    }

    [Fact]
    public void SupplierViewModel_NullOptionalFields_IsStillValid()
    {
        var model = new SupplierViewModel { Name = "MedSupply Co." };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void StockInViewModel_ValidModel_HasNoErrors()
    {
        var model = new StockInViewModel { ProductId = 1, BranchId = 1, BatchNumber = "B-001", Quantity = 10 };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void StockInViewModel_ZeroQuantity_FailsValidation()
    {
        var model = new StockInViewModel { ProductId = 1, BranchId = 1, BatchNumber = "B-001", Quantity = 0 };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(StockInViewModel.Quantity)));
    }

    [Fact]
    public void StockInViewModel_MissingBatchNumber_FailsValidation()
    {
        var model = new StockInViewModel { ProductId = 1, BranchId = 1, BatchNumber = "", Quantity = 10 };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(StockInViewModel.BatchNumber)));
    }
}
