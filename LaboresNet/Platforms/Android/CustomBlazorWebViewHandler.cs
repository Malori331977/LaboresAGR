#if ANDROID
using Android.Webkit;
using Microsoft.Maui.Handlers;
using Microsoft.AspNetCore.Components.WebView.Maui;

public class CustomBlazorWebViewHandler : BlazorWebViewHandler
{
    protected override Android.Webkit.WebView CreatePlatformView()
    {
        var webView = base.CreatePlatformView();

        webView.Settings.JavaScriptEnabled = true;
        webView.Settings.MediaPlaybackRequiresUserGesture = false;
        webView.Settings.AllowFileAccessFromFileURLs = true;
        webView.Settings.AllowUniversalAccessFromFileURLs = true;
        webView.SetWebChromeClient(new WebChromeClientCustom());

        return webView;
    }
}
#endif