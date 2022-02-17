using System;
using System.Collections.Generic;
using System.Text;

namespace QrReaderApp.POCOs
{
    public sealed class QrReaderResult
    {
        public byte[] Photo { get; }
        public string QRText { get; }

        public QrReaderResult(byte[] photo, string qrText)
        {
            Photo = photo;
            QRText = qrText;
        }
    }
}
