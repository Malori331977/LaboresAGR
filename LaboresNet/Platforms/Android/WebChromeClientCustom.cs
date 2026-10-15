using Android.Webkit;
using Microsoft.Maui.ApplicationModel;

public class WebChromeClientCustom : WebChromeClient
{
    public override void OnPermissionRequest(PermissionRequest request)
    {
        // El WebView necesita que primero se conceda el permiso de runtime de Android;
        // declarar el permiso en el manifest no es suficiente (Android 6+).
        _ = HandlePermissionRequestAsync(request);
    }

    private static async Task HandlePermissionRequestAsync(PermissionRequest request)
    {
        try
        {
            var resources = request.GetResources() ?? Array.Empty<string>();
            var granted = new List<string>();

            foreach (var resource in resources)
            {
                if (resource == PermissionRequest.ResourceVideoCapture)
                {
                    var status = await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var s = await Permissions.CheckStatusAsync<Permissions.Camera>();
                        if (s != PermissionStatus.Granted)
                            s = await Permissions.RequestAsync<Permissions.Camera>();
                        return s;
                    });
                    if (status == PermissionStatus.Granted) granted.Add(resource);
                }
                else if (resource == PermissionRequest.ResourceAudioCapture)
                {
                    var status = await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var s = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                        if (s != PermissionStatus.Granted)
                            s = await Permissions.RequestAsync<Permissions.Microphone>();
                        return s;
                    });
                    if (status == PermissionStatus.Granted) granted.Add(resource);
                }
                else
                {
                    granted.Add(resource);
                }
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (granted.Count > 0)
                    request.Grant(granted.ToArray());
                else
                    request.Deny();
            });
        }
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(() => request.Deny());
        }
    }
}
