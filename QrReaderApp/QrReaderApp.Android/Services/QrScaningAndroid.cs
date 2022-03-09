using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using QrReaderApp.Droid.Services;
using QrReaderApp.Interfaces;
using QrReaderApp.POCOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing.Mobile;
using ZXing.Net.Mobile;

[assembly: Xamarin.Forms.Dependency(typeof(QrScaningAndroid))]
namespace QrReaderApp.Droid.Services
{
    internal class QrScaningAndroid : IScaning
    {
        public async Task<QrReaderResult> ScanAsync()
        {
               var optionsCustom = new MobileBarcodeScanningOptions();
            optionsCustom.CameraResolutionSelector += (Resolution) =>
            {
                 Resolution = new List<CameraResolution>()
                {
                    new CameraResolution()
                    {
                        Width = 720,
                        Height = 480
                    }
                };
                return Resolution[0];
            };


            var scanner = new MobileBarcodeScanner() ;

            var scanResult = await scanner.Scan(optionsCustom);
            return new QrReaderResult(scanResult.RawBytes,scanResult.Text);
        }
    }
}