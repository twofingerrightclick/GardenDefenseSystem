using GardenDefenseSystem.ViewModels;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GardenDefenseSystem.Views
{
    public partial class DetectionPage : ContentPage
    {

        DetectionViewModel ViewModel = new DetectionViewModel();

        public DetectionPage()
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