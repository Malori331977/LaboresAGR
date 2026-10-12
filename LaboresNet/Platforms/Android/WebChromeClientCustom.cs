using Android.Webkit;

public class WebChromeClientCustom : WebChromeClient
{
    public override void OnPermissionRequest(PermissionRequest request)
    {
        // Concede todas las solicitudes (puedes filtrar request.GetResources() si necesitas)
        request.Grant(request.GetResources());
    }
}