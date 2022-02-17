using QrReaderApp.POCOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace QrReaderApp.Interfaces
{
    public interface IQrReader
    {
        QrReaderResult GetQRValue(byte[] image);
    }
}
