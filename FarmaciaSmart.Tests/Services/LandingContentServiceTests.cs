using FarmaciaSmart.Web.Services;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Tests.Services;

public class LandingContentServiceTests
{
    private readonly ILandingContentService _sut = new LandingContentService();

    [Fact]
    public void GetTrustItems_Returns_Four_Items()
    {
        var items = _sut.GetTrustItems();
        Assert.Equal(4, items.Count);
    }

    [Fact]
    public void GetSteps_Returns_Five_Items()
    {
        var items = _sut.GetSteps();
        Assert.Equal(5, items.Count);
    }

    [Fact]
    public void GetFeatures_Returns_Four_Items()
    {
        var items = _sut.GetFeatures();
        Assert.Equal(4, items.Count);
    }

    [Fact]
    public void All_TrustItems_Have_NonEmpty_Text()
    {
        var items = _sut.GetTrustItems();
        Assert.All(items, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Text));
            Assert.False(string.IsNullOrWhiteSpace(item.Icon));
        });
    }

    [Fact]
    public void All_Features_Have_NonEmpty_Titles()
    {
        var items = _sut.GetFeatures();
        Assert.All(items, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Title));
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
            Assert.False(string.IsNullOrWhiteSpace(item.Icon));
            Assert.False(string.IsNullOrWhiteSpace(item.Href));
        });
    }

    [Fact]
    public void Steps_Are_Numbered_Sequentially()
    {
        var items = _sut.GetSteps();
        for (int i = 0; i < items.Count; i++)
        {
            Assert.Equal(i + 1, items[i].Number);
            Assert.False(string.IsNullOrWhiteSpace(items[i].Title));
            Assert.False(string.IsNullOrWhiteSpace(items[i].Description));
        }
    }
}
