using CoreGraphics;
using Foundation;
using QrReaderApp.Interfaces;
using QrReaderApp.iOS.Services;
using QrReaderApp.POCOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using ZXing;
using ZXing.Common;

[assembly: Xamarin.Forms.Dependency(typeof(QrReaderiOS))]
namespace QrReaderApp.iOS.Services
{
    public class QrReaderiOS : IQrReader
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
                    //Obtener una representación de la imagen en UIImage a partir del arreglo de bytes.
                    var Data = NSData.FromArray(image);
                    var UIimage = UIImage.LoadFromData(Data);

                    //Tratar la imagen para obtener el arreglo de bytes en RGB
                    byte[] RgbBytes = GetRgbBytes(UIimage);
                    LuminanceSource Source = new RGBLuminanceSource(RgbBytes, (int)UIimage.Size.Width, (int)UIimage.Size.Height);
                    BinaryBitmap ImageBitMap = new BinaryBitmap(new HybridBinarizer(Source));

                    //Leer el QR de la imagen
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
                    ResultReader = new QrReaderResult(image, "Error al tratar de leer la imagen");
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            return ResultReader;

        }

        private byte[] GetRgbBytes(UIImage image)
        {
            var RgbBytes = new List<byte>();

            CGImage ImageRef = image.CGImage;
            int Width = (int)image.Size.Width;
            int Height = (int)image.Size.Height;
            CGColorSpace ColorSpace = CGColorSpace.CreateDeviceRGB();
            byte[] RawData = new byte[Height * Width * 4];
            int BytesPerPixel = 4;
            int BytesPerRow = BytesPerPixel * Width;
            int BitsPerComponent = 8;
            CGContext context = new CGBitmapContext(RawData, Width, Height,
                            BitsPerComponent, BytesPerRow, ColorSpace,
                            CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big);

            context.DrawImage(new CGRect(0, 0, Width, Height), ImageRef);


            nint ByteIndex = 0;
            for (int i = 0; i < Height * Width; i++)
            {
                byte Red = RawData[ByteIndex];
                byte Green = RawData[ByteIndex + 1];
                byte Blue = RawData[ByteIndex + 2];
                ByteIndex += BytesPerPixel;

                RgbBytes.AddRange(new[] { Red, Green, Blue });
            }
            return RgbBytes.ToArray();
        }
    }
}