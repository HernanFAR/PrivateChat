// ReSharper disable once CheckNamespace
namespace CurrieTechnologies.Razor.SweetAlert2;

public static class SweetAlertServiceExtensions
{
    public static Task<SweetAlertResult> FireWithFooterAsync(this SweetAlertService @this, string title, string html, string footer)
    {
        return @this.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Html = html,
            Footer = footer,
            Icon = SweetAlertIcon.Question
        });
    }

    public static Task<SweetAlertResult> FireConfirmAsync(this SweetAlertService @this, string title, string html)
    {
        return @this.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Html = html,
            Icon = SweetAlertIcon.Question,
            AllowOutsideClick = false,
            AllowEnterKey = false,
            AllowEscapeKey = false,
            ShowCloseButton = false,
            ShowDenyButton = true,
            ShowCancelButton = false,
            ShowConfirmButton = true
        });
    }

    public static Task<SweetAlertResult> FireBlockedMessageAsync(this SweetAlertService @this, string title, string html)
    {
        return @this.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Html = html,
            Icon = SweetAlertIcon.Info,
            AllowOutsideClick = false,
            AllowEnterKey = false,
            AllowEscapeKey = false,
            ShowCloseButton = false,
            ShowDenyButton = false,
            ShowCancelButton = false,
            ShowConfirmButton = false,
            DidOpen = new SweetAlertCallback(@this.ShowLoadingAsync)
        });
    }

    public static Task<SweetAlertResult> FireUncloseableToastMessageAsync(this SweetAlertService @this, string title, string html)
    {
        return @this.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Html = html,
            Icon = SweetAlertIcon.Info,
            Position = SweetAlertPosition.BottomRight,
            ShowCloseButton = false,
            ShowDenyButton = false,
            ShowCancelButton = false,
            ShowConfirmButton = false,
            Toast = true,
            TimerProgressBar = true,
            DidOpen = new SweetAlertCallback(@this.ShowLoadingAsync)
        });
    }

    public static Task<SweetAlertResult> FireTimedToastMessageAsync(this SweetAlertService @this, string title, string html, SweetAlertIcon icon)
    {
        return @this.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Html = html,
            Icon = icon,
            Timer = 5000,
            Position = SweetAlertPosition.BottomRight,
            ShowConfirmButton = false,
            Toast = true,
            TimerProgressBar = true
        });
    }

    public static Task<SweetAlertResult> FireValidationErrorsMessageAsync(this SweetAlertService @this,
        string title, IEnumerable<string> errors, SweetAlertIcon icon)
    {
        var html = string.Concat(errors, "<br />");

        return @this.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Html = html,
            Icon = icon,
            Timer = 5000,
            Position = SweetAlertPosition.BottomRight,
            Toast = true,
            TimerProgressBar = true,
            DidOpen = new SweetAlertCallback(@this.ShowLoadingAsync)
        });
    }

    public static Task<SweetAlertResult> FireValidationErrorsMessageAsync(this SweetAlertService @this,
        string title, string error, SweetAlertIcon icon)
    {
        return @this.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Html = error,
            Icon = icon,
            Timer = 5000,
            Position = SweetAlertPosition.BottomRight,
            Toast = true,
            TimerProgressBar = true,
            DidOpen = new SweetAlertCallback(@this.ShowLoadingAsync)
        });
    }
}
