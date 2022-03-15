using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ApxLabs.FastAndroidCamera;
using QrReaderApp;
using QrReaderApp.Droid.Renders;
using QrReaderApp.Droid.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using ZXing;
using ZXing.Mobile;


namespace QrReaderApp.Droid.Renders
{
    public class CameraPageRender : PageRenderer, TextureView.ISurfaceTextureListener
    {
        global::Android.Hardware.Camera Camera;
        global::Android.Widget.Button TakePhotoButton;
        global::Android.Widget.Button ToggleFlashButton;
        global::Android.Widget.Button SwitchCameraButton;
        global::Android.Views.View View;
        global::Android.Widget.TextView TextView;

        Activity Activity;
        CameraFacing CameraType;
        TextureView TextureView;
        SurfaceTexture SurfaceTexture;
        CameraPage CameraPage;
        readonly QrCallback cameraEventListener;
        Task processingTask;
        DateTime lastPreviewAnalysis = DateTime.UtcNow;
        bool wasScanned;
        Display Display;

        bool FlashOn;

        public CameraPageRender(Context context) : base(context)
        {
            cameraEventListener = new QrCallback();
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            try
            {
                CameraPage = (CameraPage)e.NewElement;
                SetupUserInterface();
                SetupEventHandlers();
                AddView(View);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"			ERROR: ", ex.Message);
            }
        }

        void SetupUserInterface()
        {
            Activity = this.Context as Activity;
            View = Activity.LayoutInflater.Inflate(Resource.Layout.CameraLayout, this, false);
            CameraType = CameraFacing.Back;
            TextureView = View.FindViewById<TextureView>(Resource.Id.textureView);
            TextureView.SurfaceTextureListener = this;
            TextView = View.FindViewById<TextView>(Resource.Id.labelQr);
            TextView.Text = "QR No Detectado";
        }

        void SetupEventHandlers()
        {
            TakePhotoButton = View.FindViewById<global::Android.Widget.Button>(Resource.Id.takePhotoButton);
            TakePhotoButton.Click += TakePhotoButtonTapped;


            ToggleFlashButton = View.FindViewById<global::Android.Widget.Button>(Resource.Id.toggleFlashButton);
            ToggleFlashButton.Click += ToggleFlashButtonTapped;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var Msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var Msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            View.Measure(Msw, Msh);
            View.Layout(0, 0, r - l, b - t);
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {

        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            Camera = global::Android.Hardware.Camera.Open((int)CameraType);
            TextureView.LayoutParameters = new FrameLayout.LayoutParams(width, height);
            SurfaceTexture = surface;

            Camera.SetPreviewTexture(surface);
            var previewParameters = Camera.GetParameters();
            var previewSize = previewParameters.PreviewSize;
            var bitsPerPixel = ImageFormat.GetBitsPerPixel(previewParameters.PreviewFormat);



            var bufferSize = (previewSize.Width * previewSize.Height * bitsPerPixel) / 8;
            const int NUM_PREVIEW_BUFFERS = 5;
            for (uint i = 0; i < NUM_PREVIEW_BUFFERS; ++i)
            {
                using (var buffer = new FastJavaByteArray(bufferSize))
                    Camera.AddCallbackBuffer(buffer);
            }


            PrepareAndStartCamera();
            Camera.SetNonMarshalingPreviewCallback(cameraEventListener);
            var currentFocusMode = Camera.GetParameters().FocusMode;
            if (currentFocusMode == Android.Hardware.Camera.Parameters.FocusModeAuto
                || currentFocusMode == Android.Hardware.Camera.Parameters.FocusModeMacro)
                AutoFocus();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            cameraEventListener.OnPreviewFrameReady -= HandleOnPreviewFrameReady;
            Camera.StopPreview();
            Camera.Release();
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            PrepareAndStartCamera();
        }

        void PrepareAndStartCamera()
        {
            Camera.StopPreview();
            cameraEventListener.OnPreviewFrameReady += HandleOnPreviewFrameReady;
            Display = Activity.WindowManager.DefaultDisplay;
            ResumeAnalysis();
            if (Display.Rotation == SurfaceOrientation.Rotation0)
            {
                Camera.SetDisplayOrientation(90);
            }

            if (Display.Rotation == SurfaceOrientation.Rotation270)
            {
                Camera.SetDisplayOrientation(180);
            }

            Camera.StartPreview();
            

        }

        void ToggleFlashButtonTapped(object sender, EventArgs e)
        {
            FlashOn = !FlashOn;
            if (FlashOn)
            {
                if (CameraType == CameraFacing.Back)
                {
                    ToggleFlashButton.SetBackgroundResource(Resource.Drawable.FlashButton);
                    CameraType = CameraFacing.Back;

                    Camera.StopPreview();
                    Camera.Release();
                    Camera = global::Android.Hardware.Camera.Open((int)CameraType);
                    var parameters = Camera.GetParameters();
                    parameters.FlashMode = global::Android.Hardware.Camera.Parameters.FlashModeTorch;
                    Camera.SetParameters(parameters);
                    Camera.SetPreviewTexture(SurfaceTexture);
                    PrepareAndStartCamera();
                }
            }
            else
            {
                ToggleFlashButton.SetBackgroundResource(Resource.Drawable.NoFlashButton);
                Camera.StopPreview();
                Camera.Release();

                Camera = global::Android.Hardware.Camera.Open((int)CameraType);
                var parameters = Camera.GetParameters();
                parameters.FlashMode = global::Android.Hardware.Camera.Parameters.FlashModeOff;
                Camera.SetParameters(parameters);
                Camera.SetPreviewTexture(SurfaceTexture);
                PrepareAndStartCamera();
            }
        }

        void SwitchCameraButtonTapped(object sender, EventArgs e)
        {
            if (CameraType == CameraFacing.Front)
            {
                CameraType = CameraFacing.Back;

                Camera.StopPreview();
                Camera.Release();
                Camera = global::Android.Hardware.Camera.Open((int)CameraType);
                Camera.SetPreviewTexture(SurfaceTexture);
                PrepareAndStartCamera();
            }
            else
            {
                CameraType = CameraFacing.Front;

                Camera.StopPreview();
                Camera.Release();
                Camera = global::Android.Hardware.Camera.Open((int)CameraType);
                Camera.SetPreviewTexture(SurfaceTexture);
                PrepareAndStartCamera();
            }
        }

        async void TakePhotoButtonTapped(object sender, EventArgs e)
        {
            Camera.StopPreview();
            var Image = TextureView.Bitmap;

            try
            {
                var a = CameraPage.QrText;
                byte[] ImageArray = default;
                using (MemoryStream memory = new MemoryStream())
                {
                    await Image.CompressAsync(Bitmap.CompressFormat.Jpeg, 80, memory);
                    ImageArray = memory.ToArray();
                }
                CameraPage.SendPhoto(ImageArray,"");
                //Obtener Stream
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"				", ex.Message);
            }
        }

        #region Zxing

        public bool IsAnalyzing { get; private set; }

        public void PauseAnalysis()
            => IsAnalyzing = false;

        public void ResumeAnalysis()
            => IsAnalyzing = true;

        bool CanAnalyzeFrame
        {
            get
            {
                if (!IsAnalyzing)
                    return false;

                //Check and see if we're still processing a previous frame
                // todo: check if we can run as many as possible or mby run two analyzers at once (Vision + ZXing)
                if (processingTask != null && !processingTask.IsCompleted)
                    return false;

                var elapsedTimeMs = (DateTime.UtcNow - lastPreviewAnalysis).TotalMilliseconds;
                if (elapsedTimeMs < 150)
                    return false;

                // Delay a minimum between scans
                if (wasScanned && elapsedTimeMs < 1000)
                    return false;

                return true;
            }
        }

        void HandleOnPreviewFrameReady(object sender, FastJavaByteArray fastArray)
        {
            if (!CanAnalyzeFrame)
                return;

            wasScanned = false;
            lastPreviewAnalysis = DateTime.UtcNow;

            processingTask = Task.Run(() =>
            {
                try
                {
                    DecodeFrame(fastArray);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Android.Util.Log.Debug("scanner", "DecodeFrame exception occurs");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        void DecodeFrame(FastJavaByteArray fastArray)
        {
            var Parameter = Camera.GetParameters();
            Parameter.SetPictureSize(720, 480);

            var width = Parameter.PictureSize.Width;
            var height = Parameter.PictureSize.Height;

            var rotate = false;
            var newWidth = width;
            var newHeight = height;

            // use last value for performance gain
            var cDegrees = GetCameraDisplayOrientation();

            if (cDegrees == 90 || cDegrees == 270)
            {
                rotate = true;
                newWidth = height;
                newHeight = width;
            }

            var start = PerformanceCounter.Start();

            LuminanceSource fast = new FastJavaByteArrayYUVLuminanceSource(fastArray, width, height, 0, 0, width, height); // _area.Left, _area.Top, _area.Width, _area.Height);
            if (rotate)
                fast = fast.rotateCounterClockwise();
            var Reader = new BarcodeReaderGeneric();
            Reader.Options.TryHarder = true;
            var Result = Reader.Decode(fast);


            fastArray.Dispose();
            fastArray = null;

            PerformanceCounter.Stop(start,
                "Decode Time: {0} ms (width: " + width + ", height: " + height + ", degrees: " + cDegrees + ", rotate: " +
                rotate + ")");

            if (Result != null)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Barcode Found");
                System.Diagnostics.Debug.WriteLine($"{Result.Text}");
                wasScanned = true;
                return;
            }
        }


        int GetCameraDisplayOrientation()
        {
            int degrees;
            var display = Display;
            var rotation = display.Rotation;

            switch (rotation)
            {
                case SurfaceOrientation.Rotation0:
                    degrees = 0;
                    break;
                case SurfaceOrientation.Rotation90:
                    degrees = 90;
                    break;
                case SurfaceOrientation.Rotation180:
                    degrees = 180;
                    break;
                case SurfaceOrientation.Rotation270:
                    degrees = 270;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var info = new Android.Hardware.Camera.CameraInfo();
            Android.Hardware.Camera.GetCameraInfo((int)CameraType, info);

            int correctedDegrees;
            if (info.Facing == CameraFacing.Front)
            {
                correctedDegrees = (info.Orientation + degrees) % 360;
                correctedDegrees = (360 - correctedDegrees) % 360; // compensate the mirror
            }
            else
            {
                // back-facing
                correctedDegrees = (info.Orientation - degrees + 360) % 360;
            }

            return correctedDegrees;
        }

        public void AutoFocus()
        {
            AutoFocus(0, 0, false);
        }

        void AutoFocus(int x, int y, bool useCoordinates)
        {
            if (Camera == null) return;


            var cameraParams = Camera.GetParameters();

            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Requested");

            // Cancel any previous requests
            Camera.CancelAutoFocus();

            try
            {
                // If we want to use coordinates
                // Also only if our camera supports Auto focus mode
                // Since FocusAreas only really work with FocusModeAuto set
                if (useCoordinates
                    && cameraParams.SupportedFocusModes.Contains(Android.Hardware.Camera.Parameters.FocusModeAuto))
                {
                    // Let's give the touched area a 20 x 20 minimum size rect to focus on
                    // So we'll offset -10 from the center of the touch and then 
                    // make a rect of 20 to give an area to focus on based on the center of the touch
                    x = x - 10;
                    y = y - 10;

                    // Ensure we don't go over the -1000 to 1000 limit of focus area
                    if (x >= 1000)
                        x = 980;
                    if (x < -1000)
                        x = -1000;
                    if (y >= 1000)
                        y = 980;
                    if (y < -1000)
                        y = -1000;

                    // Explicitly set FocusModeAuto since Focus areas only work with this setting
                    cameraParams.FocusMode = Android.Hardware.Camera.Parameters.FocusModeAuto;
                    // Add our focus area
                    cameraParams.FocusAreas = new List<Android.Hardware.Camera.Area>
                    {
                        new Android.Hardware.Camera.Area(new Android.Graphics.Rect(x, y, x + 20, y + 20), 1000)
                    };
                    Camera.SetParameters(cameraParams);
                }

                // Finally autofocus (weather we used focus areas or not)
                Camera.AutoFocus(cameraEventListener);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Failed: {0}", ex);
            }
        }

        #endregion  
    }
}