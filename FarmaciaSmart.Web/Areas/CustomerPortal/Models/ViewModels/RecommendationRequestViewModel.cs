using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;

public class RecommendationRequestViewModel
{
    [Required, Display(Name = "What are you looking for help with?")]
    public int CategoryId { get; set; }

    public IEnumerable<Category> Categories { get; set; } = [];
}
