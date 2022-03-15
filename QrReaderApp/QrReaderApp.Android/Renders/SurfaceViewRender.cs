using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using QrReaderApp;
using QrReaderApp.Droid.Renders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using ZXing.Mobile;
using ZXing.Mobile.CameraAccess;
using ZXing.Net.Mobile.Android;


namespace QrReaderApp.Droid.Renders
{
    public class SurfaceViewRender : SurfaceView, ISurfaceHolderCallback, IScannerView, IScannerSessionHost
	{
		public SurfaceViewRender(Context context, MobileBarcodeScanningOptions options)
			: base(context)
		{
			ScanningOptions = options ?? new MobileBarcodeScanningOptions();
			Init();
		}

		protected SurfaceViewRender(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer) => Init();

		bool addedHolderCallback = false;

		void Init()
		{
			if (cameraAnalyzer == null)
				cameraAnalyzer = new CameraAnalyzer(this, this);

			cameraAnalyzer.ResumeAnalysis();

			if (!addedHolderCallback)
			{
				Holder.AddCallback(this);
				Holder.SetType(SurfaceType.PushBuffers);
				addedHolderCallback = true;
			}
		}

		public async void SurfaceCreated(ISurfaceHolder holder)
		{
			await PermissionsHandler.RequestPermissionsAsync();

			cameraAnalyzer.SetupCamera();

			surfaceCreated = true;
		}

		public async void SurfaceChanged(ISurfaceHolder holder, Format format, int wx, int hx)
			=> cameraAnalyzer.RefreshCamera();

		public async void SurfaceDestroyed(ISurfaceHolder holder)
		{
			try
			{
				if (addedHolderCallback)
				{
					Holder.RemoveCallback(this);
					addedHolderCallback = false;
				}
			}
			catch { }

			cameraAnalyzer.ShutdownCamera();
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
			ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

			//cameraAnalyzer.BarcodeFound = (result) =>
			//{
			//	scanResultCallback?.Invoke(result);
			//};
			cameraAnalyzer.ResumeAnalysis();
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

		CameraAnalyzer cameraAnalyzer;
		bool surfaceCreated;

		public bool HasTorch => cameraAnalyzer.Torch.IsSupported;

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			// Reinit things
			Init();
		}

		protected override void OnWindowVisibilityChanged(ViewStates visibility)
		{
			base.OnWindowVisibilityChanged(visibility);
			if (visibility == ViewStates.Visible)
				Init();
		}

		public override async void OnWindowFocusChanged(bool hasWindowFocus)
		{
			base.OnWindowFocusChanged(hasWindowFocus);

			if (!hasWindowFocus)
				return;

			//only refresh the camera if the surface has already been created. Fixed #569
			if (surfaceCreated)
				cameraAnalyzer.RefreshCamera();
		}


    }
}