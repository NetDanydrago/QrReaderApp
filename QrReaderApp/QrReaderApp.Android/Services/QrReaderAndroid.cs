using Android.App;
using Android.Content;
using Android.Graphics;
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
using ZXing;
using ZXing.Common;

[assembly: Xamarin.Forms.Dependency(typeof(QrReaderAndroid))]
namespace QrReaderApp.Droid.Services
{
    public class QrReaderAndroid : IQrReader
    {
        public QrReaderResult GetQRValue(byte[] image)
        {
            QrReaderResult ResultReader = default;

            if (image == null || image.Length == 0)
            {
                ResultReader = new QrReaderResult(null, "No hay imagen");
            }
            else
            {
                try
                {
                    //Obtener una representación de la imagen en Bitmat a partir del arreglo de bytes.
                    BitmapFactory.Options Options = new BitmapFactory.Options();
                    Options.InSampleSize = 2;
                    Bitmap Bitmap = BitmapFactory.DecodeByteArray(image, 0, image.Length, Options);

                    //Tratar la imagen para obtener el arreglo de bytes en RGB
                    byte[] RgbBytes = GetRgbBytes(Bitmap);
                    LuminanceSource Source = new RGBLuminanceSource(RgbBytes, Bitmap.Width, Bitmap.Height);
                    BinaryBitmap ImageBitMap = new BinaryBitmap(new HybridBinarizer(Source));

                    //Leer el QR de La Imagen
                    var Reader = new MultiFormatReader();
                    IDictionary<DecodeHintType, object> Hints = new Dictionary<DecodeHintType, object>();
                    Hints.Add(ZXing.DecodeHintType.PURE_BARCODE, true);
                    Hints.Add(ZXing.DecodeHintType.TRY_HARDER, true);
                    Reader.Hints = Hints;
                    var Result = Reader.decode(ImageBitMap);
                    ResultReader = new QrReaderResult(image, Result.Text);
                }
                catch (Exception ex)
                {
                    ResultReader = new QrReaderResult(image, $"Error al tratar de leer la imagen");
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

            }
            return ResultReader;
        }

        private byte[] GetRgbBytes(Bitmap image)
        {
            var RgbBytes = new List<byte>();
            var Pixels = new int[image.Width * image.Height];
            image.GetPixels(Pixels, 0, image.Width, 0, 0, image.Width, image.Height);
            foreach (var argb in Pixels)
            {
                var Color = new Android.Graphics.Color(argb);
                RgbBytes.AddRange(new[] { Color.R, Color.G, Color.B });
            }
            return RgbBytes.ToArray();
        }
    }
}