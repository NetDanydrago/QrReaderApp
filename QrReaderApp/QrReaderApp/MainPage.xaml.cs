using Plugin.Media;
using Plugin.Media.Abstractions;
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
            TakeButton.Clicked += OnTakeButton_Clicked;
        }

        private async void OnPickButton_Clicked(object sender, EventArgs e)
        {       
            var MediaFile = await CrossMedia.Current.PickPhotoAsync();
            await ReadQr(MediaFile);
        }

        private async void OnTakeButton_Clicked(object sender, EventArgs e)
        {
            var MediaFile = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions()
            {
                PhotoSize = PhotoSize.Small,
                Name = $"{Guid.NewGuid()}.jpg"
            });
            await ReadQr(MediaFile);
        }

        private async Task ReadQr(MediaFile file)
        {
            byte[] ImageArray = default;
            using (Stream Stream = file.GetStream())
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    Stream.CopyTo(memory);
                    ImageArray = memory.ToArray();
                }
            }
            var Result = DependencyService.Get<IQrReader>().GetQRValue(ImageArray);
            TextQr.Text = Result.QRText;
            PhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(ImageArray));
        }
    }
}
