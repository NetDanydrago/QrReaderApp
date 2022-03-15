using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ApxLabs.FastAndroidCamera;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QrReaderApp.Droid.Services
{
    public class QrCallback : Java.Lang.Object, INonMarshalingPreviewCallback, Camera.IAutoFocusCallback
    {
        public event EventHandler<FastJavaByteArray> OnPreviewFrameReady;

        public void OnAutoFocus(bool success, Camera camera)
        {
            Android.Util.Log.Debug("Scanner", "AutoFocus {0}", success ? "Succeeded" : "Failed");
        }

        public void OnPreviewFrame(IntPtr data, Camera camera)
        {
            if (data != null && data != IntPtr.Zero)
            {
                using (var fastArray = new FastJavaByteArray(data))
                {
                    OnPreviewFrameReady?.Invoke(this, fastArray);

                    camera.AddCallbackBuffer(fastArray);
                }
            }
        }
    }
}