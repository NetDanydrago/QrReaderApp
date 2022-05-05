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
            ScanButton.Clicked += OnScanButton_Clicked;
        }

        private async void OnScanButton_Clicked(object sender, EventArgs e)
        {

                var Camera = new CameraPage();
                //Camera.CameraBorder = CameraPageBorder.Horizontal;
                Camera.OnTakedPhoto += async (camerasender, result) =>
                {
                    PhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(result.Photo));
                    TextQr.Text =result.QRText;
                    await Navigation.PopModalAsync();
                };
            await Navigation.PushModalAsync(Camera);

           }



        private async void OnPickButton_Clicked(object sender, EventArgs e)
        {       
            var Photo = await MediaPicker.PickPhotoAsync();
            await ReadQr(Photo);
        }

        private async void OnTakeButton_Clicked(object sender, EventArgs e)
        {
            var Photo = await MediaPicker.CapturePhotoAsync();
            await ReadQr(Photo);
        }

        private async Task ReadQr(FileResult photo)
        {
            byte[] ImageArray = default;
            PhotoImage.Source = ImageSource.FromFile(photo.FullPath);
            var Stream = await photo.OpenReadAsync();
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
