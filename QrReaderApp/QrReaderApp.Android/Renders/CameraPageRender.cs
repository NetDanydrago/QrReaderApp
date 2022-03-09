using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using QrReaderApp;
using QrReaderApp.Droid.Renders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CameraPage), typeof(CameraPageRender))]
namespace QrReaderApp.Droid.Renders
{
    public class CameraPageRender : PageRenderer, TextureView.ISurfaceTextureListener
    {
        global::Android.Hardware.Camera Camera;
        global::Android.Widget.Button TakePhotoButton;
        global::Android.Widget.Button ToggleFlashButton;
        global::Android.Widget.Button SwitchCameraButton;
        global::Android.Views.View View;

        Activity Activity;
        CameraFacing CameraType;
        TextureView TextureView;
        SurfaceTexture SurfaceTexture;
        CameraPage CameraPage;

        bool FlashOn;

        public CameraPageRender(Context context) : base(context)
        {
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
            var TextQr = View.FindViewById<TextView>(Resource.Id.labelQr);
            TextQr.Text = CameraPage.QrText;
        }

        void SetupEventHandlers()
        {
            TakePhotoButton = View.FindViewById<global::Android.Widget.Button>(Resource.Id.takePhotoButton);
            TakePhotoButton.Click += TakePhotoButtonTapped;

            SwitchCameraButton = View.FindViewById<global::Android.Widget.Button>(Resource.Id.switchCameraButton);
            SwitchCameraButton.Click += SwitchCameraButtonTapped;

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
            PrepareAndStartCamera();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
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

            var display = Activity.WindowManager.DefaultDisplay;
            if (display.Rotation == SurfaceOrientation.Rotation0)
            {
                Camera.SetDisplayOrientation(90);
            }

            if (display.Rotation == SurfaceOrientation.Rotation270)
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
                CameraPage.SendPhoto(ImageArray);
                //Obtener Stream
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"				", ex.Message);
            }
        }
    }
}