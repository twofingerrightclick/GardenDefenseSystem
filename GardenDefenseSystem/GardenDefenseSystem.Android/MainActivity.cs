using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using System.IO;
using AndroidX.Core.Content;
using Android;
using AndroidX.Core.App;
using Java.IO;
using File = Java.IO.File;
using Android.Provider;
using Android.Content;
using Mobile.BuildTools.Configuration;

namespace GardenDefenseSystem.Droid
{
    [Activity(
        Label = "GardenDefenseSystem",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize
            | ConfigChanges.Orientation
            | ConfigChanges.UiMode
            | ConfigChanges.ScreenLayout
            | ConfigChanges.SmallestScreenSize
    )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ConfigurationManager.Init(true, this);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            EnsurePermissions();
            
            LoadApplication(new App());
            Window.SetStatusBarColor(Android.Graphics.Color.Black);
        }

        private void EnsurePermissions()
        {
            if (
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera)
                != Permission.Granted
            )
            {
                ActivityCompat.RequestPermissions(
                    this,
                    new string[]
                    {
                        Manifest.Permission.Camera,
                        Manifest.Permission.WriteExternalStorage,
                        Manifest.Permission.ReadExternalStorage
                    },
                    0
                );
            }
        }

        public override void OnRequestPermissionsResult(
            int requestCode,
            string[] permissions,
            [GeneratedEnum] Android.Content.PM.Permission[] grantResults
        )
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(
                requestCode,
                permissions,
                grantResults
            );
        }
    }
}
