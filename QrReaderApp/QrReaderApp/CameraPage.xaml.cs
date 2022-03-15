using QrReaderApp.Interfaces;
using QrReaderApp.POCOs;
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

        public void SendPhoto(byte[] image,string qr)
        {
            var HandlerPhoto = OnTakedPhoto;
           HandlerPhoto?.Invoke(this,new QrReaderResult(image,qr));

        }

        public string QrText { get; set; }

        public event EventHandler<QrReaderResult> OnTakedPhoto;
    }
}