using GardenDefenseSystem.Models;
using GardenDefenseSystem.Services;
using GardenDefenseSystem.Views;
using System;
using System.ComponentModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GardenDefenseSystem
{
    public partial class App : Application
    {
        VisionApiCallCount VisionApiCallCount { get; }

        public App()
        {
            InitializeComponent();
            VisionApiCallCount = VisionApiCallCount.Instance;
            InitializeStoredSettings();

            DependencyService.Register<MockDataStore>();

            MainPage = new AppShell();
        }

        private void InitializeStoredSettings()
        {
            object apiCallCount;
            if (Properties.TryGetValue(_ApiCountKey, out apiCallCount))
            {
                VisionApiCallCount.CallCount = (int)apiCallCount;
            }

            VisionApiCallCount.ApiCountChanged += StoreApiCallCount;
        }

        protected override void OnStart()
        {
            DeviceDisplay.KeepScreenOn = true;
        }

        protected override void OnSleep() { }

        protected override void OnResume() { }

        const string _ApiCountKey = "apiCallCount";

        private void StoreApiCallCount(object sender, PropertyChangedEventArgs e)
        {
            Properties[_ApiCountKey] = VisionApiCallCount.CallCount;
        }
    }
}
