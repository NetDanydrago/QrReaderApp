using QrReaderApp.POCOs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QrReaderApp.Interfaces
{
    public interface IScaning
    {
        Task<QrReaderResult> ScanAsync();
    }
}
