using GardenDefenseSystem.ViewModels;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GardenDefenseSystem.Views
{
    public partial class AboutPage : ContentPage
    {

        AboutViewModel ViewModel = new AboutViewModel();

        public AboutPage()
        {
            
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing(); //call this before unsubscribing
            ViewModel.OnDisappearing();
           
        }
    }
}