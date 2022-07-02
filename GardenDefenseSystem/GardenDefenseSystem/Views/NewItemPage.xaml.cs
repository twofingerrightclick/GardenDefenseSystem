using GardenDefenseSystem.Models;
using GardenDefenseSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GardenDefenseSystem.Views
{
    public partial class NewItemPage : ContentPage
    {
        public ObjectDetectedShotLog Item { get; set; }

        public NewItemPage()
        {
            InitializeComponent();
            BindingContext = new NewItemViewModel();
        }
    }
}