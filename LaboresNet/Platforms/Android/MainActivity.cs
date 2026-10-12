using Android.App;
using Android.Content.PM;
using Android.OS;

namespace LaboresNet
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, HardwareAccelerated =true ,ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
