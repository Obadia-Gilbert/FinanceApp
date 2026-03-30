using System.Globalization;
using System.Resources;
using FinanceApp.Localization;

namespace FinanceApp.Tests.Localization;

public class SharedResourceEmbeddedTests
{
    [Fact]
    public void Acc_Subtitle_and_Acc_Add_exist_in_neutral_resources()
    {
        var rm = new ResourceManager("FinanceApp.Localization.SharedResource", typeof(SharedResource).Assembly);
        var inv = CultureInfo.InvariantCulture;

        var subtitle = rm.GetString("Acc_Subtitle", inv);
        var add = rm.GetString("Acc_Add", inv);

        Assert.False(string.IsNullOrEmpty(subtitle));
        Assert.False(string.IsNullOrEmpty(add));
        Assert.DoesNotContain("Acc_Subtitle", subtitle, StringComparison.Ordinal);
        Assert.DoesNotContain("Acc_Add", add, StringComparison.Ordinal);
    }
}
