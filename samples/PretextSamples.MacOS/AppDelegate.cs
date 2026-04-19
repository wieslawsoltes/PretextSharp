namespace PretextSamples.MacOS;

[Register ("AppDelegate")]
public class AppDelegate : NSApplicationDelegate {
	public override void DidFinishLaunching (NSNotification notification)
	{
		PretextLayout.SetTextMeasurerFactory(new CoreTextTextMeasurerFactory());
	}

	public override void WillTerminate (NSNotification notification)
	{
		// Insert code here to tear down your application
	}
}
