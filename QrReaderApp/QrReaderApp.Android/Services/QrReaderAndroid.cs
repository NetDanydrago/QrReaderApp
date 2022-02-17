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
                    //Obtener una representacion de la imagen en bitmat a partir del arreglo de bytes.
                    BitmapFactory.Options Options = new BitmapFactory.Options();
                    Options.InSampleSize = 2;
                    Bitmap Bitmap = BitmapFactory.DecodeByteArray(image, 0, image.Length, Options);

                    //obtener el rgb de la imagen en un arreglo.
                    byte[] RgbBytes = GetRgbBytes(Bitmap);
                    LuminanceSource source = new RGBLuminanceSource(RgbBytes, Bitmap.Width, Bitmap.Height);
                    BinaryBitmap ImageBitMap = new BinaryBitmap(new HybridBinarizer(source));

                    //Leer el QR de La Imagen
                    var Reader = new MultiFormatReader();
                    IDictionary<DecodeHintType, object> hints = new Dictionary<DecodeHintType, object>();
                    hints.Add(ZXing.DecodeHintType.PURE_BARCODE, true);
                    hints.Add(ZXing.DecodeHintType.TRY_HARDER, true);
                    Reader.Hints = hints;
                    var Result = Reader.decode(ImageBitMap);
                    ResultReader = new QrReaderResult(image, Result.Text);
                }
                catch (Exception ex)
                {
                    ResultReader = new QrReaderResult(image, $"error al tratar de leer la imagen");
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

            }
            return ResultReader;
        }

        public byte[] GetRgbBytes(Bitmap image)
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