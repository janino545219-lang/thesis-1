using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Inventories;

public class StockRowViewModel
{
    public Inventory Inventory { get; set; } = null!;
    public DateTime? NearestExpiration { get; set; }
}
