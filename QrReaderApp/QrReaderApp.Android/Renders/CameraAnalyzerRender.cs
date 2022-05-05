using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using QrReaderApp;
using QrReaderApp.Droid.Analyzer;
using QrReaderApp.Droid.Renders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using ZXing.Mobile;

[assembly: ExportRenderer(typeof(CameraPage), typeof(CameraAnalyzerRender))]
namespace QrReaderApp.Droid.Renders
{
    public class CameraAnalyzerRender : PageRenderer, TextureView.ISurfaceTextureListener, IScannerView, IScannerSessionHost
	{


        global::Android.Widget.Button TakePhotoButton;
        global::Android.Views.View View;
        global::Android.Widget.TextView TextView;

        Activity Activity;
        CameraFacing CameraType;
        TextureView TextureView;
        ImageView HorizontalBorder;
        ImageView SquareBorder;
        SurfaceTexture SurfaceTexture;
        CameraPage CameraPage;
        bool FlashOn;

        public CameraAnalyzerRender(Context context)
    : base(context)
        {
            ScanningOptions = new MobileBarcodeScanningOptions() { CameraResolutionSelector = new MobileBarcodeScanningOptions.CameraResolutionSelectorDelegate(SelectLowestResolutionMatchingDisplayAspectRatio)  };
        }



        public CameraResolution SelectLowestResolutionMatchingDisplayAspectRatio(List<CameraResolution> availableResolutions)
        {
            CameraResolution result = null;
            //a tolerance of 0.1 should not be visible to the user
            double aspectTolerance = 0.1;
            var displayOrientationHeight = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait ? DeviceDisplay.MainDisplayInfo.Height : DeviceDisplay.MainDisplayInfo.Width;
            var displayOrientationWidth = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait ? DeviceDisplay.MainDisplayInfo.Width : DeviceDisplay.MainDisplayInfo.Height;
            //calculatiing our targetRatio
            var targetRatio = displayOrientationHeight / displayOrientationWidth;
            var targetHeight = displayOrientationHeight;
            var minDiff = double.MaxValue;
            //camera API lists all available resolutions from highest to lowest, perfect for us
            //making use of this sorting, following code runs some comparisons to select the lowest resolution that matches the screen aspect ratio and lies within tolerance
            //selecting the lowest makes Qr detection actual faster most of the time
            foreach (var r in availableResolutions.Where(r => Math.Abs(((double)r.Width / r.Height) - targetRatio) < aspectTolerance))
            {
                //slowly going down the list to the lowest matching solution with the correct aspect ratio
                if (Math.Abs(r.Height - targetHeight) < minDiff)
                    minDiff = Math.Abs(r.Height - targetHeight);
                result = r;
            }
            return result;
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
            TextView.SetTextColor(Android.Graphics.Color.ParseColor("#FA0000"));
            switch (CameraPage.CameraBorder)
            {
                case CameraPageBorder.Square :
                    SquareBorder = View.FindViewById<ImageView>(Resource.Id.BorderSquare);
                    SquareBorder.Visibility = ViewStates.Visible;
                break;
                case CameraPageBorder.Horizontal:
                    HorizontalBorder = View.FindViewById<ImageView>(Resource.Id.BorderHorizontal);
                    HorizontalBorder.Visibility = ViewStates.Visible;
                break;
            }

        }

        void SetupEventHandlers()
        {
            TakePhotoButton = View.FindViewById<global::Android.Widget.Button>(Resource.Id.takePhotoButton);
            TakePhotoButton.Click += TakePhotoButtonTapped;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var Msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var Msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            View.Measure(Msw, Msh);
            View.Layout(0, 0, r - l, b - t);
        }


        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
			if (cameraAnalyzer == null)
				cameraAnalyzer = new CameraAnalyzerQR(this,surface,this);

			cameraAnalyzer.ResumeAnalysis();

            StartScanning(null, null);
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            cameraAnalyzer.ShutdownCamera();
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            cameraAnalyzer.RefreshCamera();
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            cameraAnalyzer.RefreshCamera();
        }


		public override bool OnTouchEvent(MotionEvent e)
		{
			var r = base.OnTouchEvent(e);

			switch (e.Action)
			{
				case MotionEventActions.Down:
					return true;
				case MotionEventActions.Up:
					var touchX = e.GetX();
					var touchY = e.GetY();
					AutoFocus((int)touchX, (int)touchY);
					break;
			}

			return r;
		}

		public void AutoFocus()
			=> cameraAnalyzer.AutoFocus();

		public void AutoFocus(int x, int y)
			=> cameraAnalyzer.AutoFocus(x, y);

		public void StartScanning(Action<ZXing.Result> scanResultCallback, MobileBarcodeScanningOptions options = null)
		{
			cameraAnalyzer.SetupCamera();


            cameraAnalyzer.BarcodeFound = (result) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    TextView.Text = result.Text;
                    TextView.SetTextColor(Android.Graphics.Color.ParseColor("#00FA39"));
                });

            };
		}

		public void StopScanning()
			=> cameraAnalyzer.ShutdownCamera();

		public void PauseAnalysis()
			=> cameraAnalyzer.PauseAnalysis();

		public void ResumeAnalysis()
			=> cameraAnalyzer.ResumeAnalysis();

		public void Torch(bool on)
		{
			if (on)
				cameraAnalyzer.Torch.TurnOn();
			else
				cameraAnalyzer.Torch.TurnOff();
		}

		public void ToggleTorch()
			=> cameraAnalyzer.Torch.Toggle();

		public MobileBarcodeScanningOptions ScanningOptions { get; set; }

		public bool IsTorchOn => cameraAnalyzer.Torch.IsEnabled;

		public bool IsAnalyzing => cameraAnalyzer.IsAnalyzing;

		CameraAnalyzerQR cameraAnalyzer;
		bool surfaceCreated;

		public bool HasTorch => cameraAnalyzer.Torch.IsSupported;



        async void TakePhotoButtonTapped(object sender, EventArgs e)
        {
            StopScanning();
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
                CameraPage.SendPhoto(ImageArray, TextView.Text);
                //Obtener Stream
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"				", ex.Message);
            }
        }

    }
}