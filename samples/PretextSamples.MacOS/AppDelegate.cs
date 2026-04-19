namespace PretextSamples.MacOS;

[Register ("AppDelegate")]
public class AppDelegate : NSApplicationDelegate {
	private NSWindow? _mainWindow;

	public override void DidFinishLaunching (NSNotification notification)
	{
		PretextLayout.SetTextMeasurerFactory(new CoreTextTextMeasurerFactory());

		var application = NSApplication.SharedApplication;
		application.ActivationPolicy = NSApplicationActivationPolicy.Regular;

		if (application.DangerousWindows.Count > 0)
		{
			_mainWindow = application.DangerousWindows[0];
			ConfigureMainWindow(_mainWindow);
		}
		else
		{
			_mainWindow = CreateMainWindow();
		}

		_mainWindow.MakeKeyAndOrderFront(this);
		NSRunningApplication.CurrentApplication.Activate(NSApplicationActivationOptions.ActivateIgnoringOtherWindows);
	}

	public override void WillTerminate (NSNotification notification)
	{
		// Insert code here to tear down your application
	}

	public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
	{
		return true;
	}

	public override bool SupportsSecureRestorableState (NSApplication app)
	{
		return true;
	}

	private static NSWindow CreateMainWindow ()
	{
		var frame = new CGRect(0, 0, 1440, 960);
		var window = new NSWindow(
			frame,
			NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable | NSWindowStyle.Resizable,
			NSBackingStore.Buffered,
			false)
		{
			Title = "PretextSamples.MacOS",
			MinSize = new CGSize(1080, 720),
		};

		ConfigureMainWindow(window);
		return window;
	}

	private static void ConfigureMainWindow (NSWindow window)
	{
		var frame = new CGRect(0, 0, 1440, 960);
		window.Title = "PretextSamples.MacOS";
		window.MinSize = new CGSize(1080, 720);
		window.SetFrame(frame, true);

		var contentView = new SampleShellView
		{
			Frame = new CGRect(CGPoint.Empty, frame.Size),
			AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable,
		};

		window.ContentView = contentView;
		window.Center();
	}
}
