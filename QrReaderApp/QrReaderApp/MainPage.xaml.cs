using QrReaderApp.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace QrReaderApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            PickButton.Clicked += OnPickButton_Clicked;
        }

        private async void OnPickButton_Clicked(object sender, EventArgs e)
        {
            byte[] ImageArray = default;
            var Photo = await MediaPicker.PickPhotoAsync();
            PhotoImage.Source = ImageSource.FromFile(Photo.FullPath);
            var Stream = await Photo.OpenReadAsync();
            using (MemoryStream memory = new MemoryStream())
            {
                Stream.CopyTo(memory);
                ImageArray = memory.ToArray();
            }
            var Result = DependencyService.Get<IQrReader>().GetQRValue(ImageArray);
            TextQr.Text = Result.QRText;

        }
    }
}
