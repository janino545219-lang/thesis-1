namespace FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;

public class ChatMessageRequest
{
    public string? Message { get; set; }
    public int? CategoryId { get; set; }
    public int? CurrentRecommendationId { get; set; }
}

public class ChatFeedbackRequest
{
    public int RecommendationItemId { get; set; }
    public int RecommendationId { get; set; }
    public int Rating { get; set; } // 0 = Helpful, 1 = NotHelpful
    public string? Comment { get; set; }
}

public class ChatAddToCartRequest
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
}

public class ChatItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string? Warning { get; set; }
    public int Rank { get; set; }
    public decimal Score { get; set; }
}

public class ChatCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
