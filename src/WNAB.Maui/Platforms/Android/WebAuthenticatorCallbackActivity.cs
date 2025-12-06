using Android.App;
using Android.Content;

namespace WNAB.Maui;

[Activity(Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataSchemes = new[] { "wnab" },
    DataHosts = new[] { "callback" })]
public class WebAuthenticatorCallbackActivity : global::Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
