using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Android.Hardware;

namespace ZXing.Net.Mobile.Android
{
    internal class ZxingCameraPicture : Java.Lang.Object, Camera.IPictureCallback
    {
        public byte[] ImageArray { get; set; }

        public void OnPictureTaken(byte[] data, Camera camera)
        {
            ImageArray = data;
        }
    }
}

