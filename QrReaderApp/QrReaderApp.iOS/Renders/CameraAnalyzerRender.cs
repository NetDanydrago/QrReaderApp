using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using QrReaderApp;
using QrReaderApp.iOS.Analyzer;
using QrReaderApp.iOS.Renders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using ZXing.Mobile;

[assembly: ExportRenderer(typeof(CameraPage), typeof(CameraAnalyzerRender))]
namespace QrReaderApp.iOS.Renders
{
    public class CameraAnalyzerRender :  PageRenderer, IScannerViewController
    {
		Analyzer.ZXingScannerView scannerView;

		public event Action<ZXing.Result> OnScannedResult;

		public MobileBarcodeScanningOptions ScanningOptions { get; set; }
		public MobileBarcodeScanner Scanner { get; set; }
		public bool ContinuousScanning { get; set; }


		UIActivityIndicatorView loadingView;
		UIView loadingBg;

		public UIView CustomLoadingView { get; set; }

		public CameraAnalyzerRender()
		{
			var appFrame = UIScreen.MainScreen.ApplicationFrame;

			View.Frame = new CGRect(0, 0, appFrame.Width, appFrame.Height);
			View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
				ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
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

		protected override void OnElementChanged(VisualElementChangedEventArgs e)
		{
			base.OnElementChanged(e);

			if (e.OldElement != null || Element == null)
			{
				return;
			}

			try
			{
				//SetupUserInterface();
				//SetupEventHandlers();
				//SetupLiveCameraStream();
				//AuthorizeCameraUse();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"\t\t\tERROR: {ex.Message}");
			}
		}

		public UIViewController AsViewController()
			=> this;

		public void Cancel()
			=> InvokeOnMainThread(scannerView.StopScanning);

		UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

		public override void ViewDidLoad()
		{
			loadingBg = new UIView(View.Frame) { BackgroundColor = UIColor.Black, AutoresizingMask = UIViewAutoresizing.FlexibleDimensions };
			loadingView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge)
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleMargins
			};
			loadingView.Frame = new CGRect((View.Frame.Width - loadingView.Frame.Width) / 2,
				(View.Frame.Height - loadingView.Frame.Height) / 2,
				loadingView.Frame.Width,
				loadingView.Frame.Height);

			loadingBg.AddSubview(loadingView);
			View.AddSubview(loadingBg);
			loadingView.StartAnimating();

			scannerView = new Analyzer.ZXingScannerView(new CGRect(0, 0, View.Frame.Width, View.Frame.Height))
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
				//Codigo de Interfaz
				UseCustomOverlayView = Scanner.UseCustomOverlay,
				CustomOverlayView = Scanner.CustomOverlay,
				TopText = Scanner.TopText,
				BottomText = Scanner.BottomText,
				CancelButtonText = Scanner.CancelButtonText,
				FlashButtonText = Scanner.FlashButtonText
			};
			scannerView.OnCancelButtonPressed += delegate
			{
				Scanner.Cancel();
			};

			//this.View.AddSubview(scannerView);
			View.InsertSubviewBelow(scannerView, loadingView);

			View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
			{
				if (UIApplication.SharedApplication.KeyWindow != null)
					OverrideUserInterfaceStyle = UIApplication.SharedApplication.KeyWindow.RootViewController.OverrideUserInterfaceStyle;
			}
		}

		public void Torch(bool on)
			=> scannerView?.Torch(on);

		public void ToggleTorch()
			=> scannerView?.ToggleTorch();

		public void PauseAnalysis()
			=> scannerView?.PauseAnalysis();

		public void ResumeAnalysis()
			=> scannerView?.ResumeAnalysis();

		public bool IsTorchOn
			=> scannerView?.IsTorchOn ?? false;

		public override void ViewDidAppear(bool animated)
		{
			scannerView.OnScannerSetupComplete += HandleOnScannerSetupComplete;

			originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;

			if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
			{
				UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.Default;
				SetNeedsStatusBarAppearanceUpdate();
			}
			else
				UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackTranslucent, false);

			Console.WriteLine("Starting to scan...");

			Task.Factory.StartNew(() =>
			{
				BeginInvokeOnMainThread(() => scannerView.StartScanning(result =>
				{

					if (!ContinuousScanning)
					{
						Console.WriteLine("Stopping scan...");
						scannerView.StopScanning();
					}

					OnScannedResult?.Invoke(result);

				}, ScanningOptions));
			});
		}

		public override void ViewDidDisappear(bool animated)
		{
			scannerView?.StopScanning();

			scannerView.OnScannerSetupComplete -= HandleOnScannerSetupComplete;
		}

		public override void ViewWillDisappear(bool animated)
			=> UIApplication.SharedApplication.SetStatusBarStyle(originalStatusBarStyle, false);

		public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
			=> scannerView?.DidRotate(this.InterfaceOrientation);

		public override bool ShouldAutorotate()
			=> ScanningOptions?.AutoRotate ?? false;

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
			=> UIInterfaceOrientationMask.All;

		[Obsolete("Deprecated in iOS6. Replace it with both GetSupportedInterfaceOrientations and PreferredInterfaceOrientationForPresentation")]
		public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
			=> ScanningOptions?.AutoRotate ?? false;

		void HandleOnScannerSetupComplete()
			=> BeginInvokeOnMainThread(() =>
			{
				if (loadingView != null && loadingBg != null && loadingView.IsAnimating)
				{
					loadingView.StopAnimating();

					UIView.BeginAnimations("zoomout");

					UIView.SetAnimationDuration(2.0f);
					UIView.SetAnimationCurve(UIViewAnimationCurve.EaseOut);

					loadingBg.Transform = CGAffineTransform.MakeScale(2.0f, 2.0f);
					loadingBg.Alpha = 0.0f;

					UIView.CommitAnimations();


					loadingBg.RemoveFromSuperview();
				}
			});

	}
}