using QrReaderApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace QrReaderApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CameraPage : ContentPage
    {
        public CameraPage()
        {
            InitializeComponent();
        }

        public void SendPhoto(byte[] image)
        {
            var Handler = OnTakedPhoto;
            Handler?.Invoke(this, image);
        }

        public string QrText { get; set; }

        public event EventHandler<byte[]> OnTakedPhoto;
    }
}