using FinanceApp.Domain.Enums;
using FinanceApp.Localization;
using Microsoft.Extensions.Localization;

namespace FinanceApp.Web.Helpers;

public static class AccountDisplayHelper
{
    public static string LocalizedAccountType(IStringLocalizer<SharedResource> localizer, AccountType type) =>
        type switch
        {
            AccountType.Checking => localizer["Acc_Type_Checking"].Value,
            AccountType.Savings => localizer["Acc_Type_Savings"].Value,
            AccountType.CreditCard => localizer["Acc_Type_CreditCard"].Value,
            AccountType.Cash => localizer["Acc_Type_Cash"].Value,
            AccountType.Investment => localizer["Acc_Type_Investment"].Value,
            _ => type.ToString()
        };
}
