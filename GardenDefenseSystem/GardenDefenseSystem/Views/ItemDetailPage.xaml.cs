using GardenDefenseSystem.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace GardenDefenseSystem.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}