#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Example;

using Android.App;
using Android.Content.PM;
using Android.OS;

using AndroidX.Activity;

[Activity(
    Name = "clamchart.example.MainActivity",
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.Portrait)]
public sealed class MainActivity : MauiAppCompatActivity
{
    private BackPressedCallback? backPressedCallback;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        backPressedCallback = new BackPressedCallback(this);
        OnBackPressedDispatcher.AddCallback(this, backPressedCallback);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            backPressedCallback?.Dispose();
            backPressedCallback = null;
        }

        base.Dispose(disposing);
    }

    private sealed class BackPressedCallback : OnBackPressedCallback
    {
        private readonly MainActivity activity;

        public BackPressedCallback(MainActivity activity)
            : base(true)
        {
            this.activity = activity;
        }

        public override void HandleOnBackPressed()
        {
            var windows = Microsoft.Maui.Controls.Application.Current?.Windows;
            var page = windows is { Count: > 0 } ? windows[^1].Page : null;
            if (page?.SendBackButtonPressed() ?? false)
            {
                return;
            }

            Enabled = false;
            try
            {
                activity.OnBackPressedDispatcher.OnBackPressed();
            }
            finally
            {
                Enabled = true;
            }
        }
    }
}
