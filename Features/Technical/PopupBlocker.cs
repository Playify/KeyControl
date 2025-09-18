using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Win;
using PlayifyUtility.Windows.Win.Native;

namespace KeyControl.Features.Technical;

[InitOnLoad]
public static class PopupBlocker{
	private static readonly ConfigValue<bool> Enabled=ConfigValue.Create(true,"Technical","PopupBlocker").Listen(b=>Utils.UiThread.Invoke(()=>{
		_hook?.Dispose();
		if(!b) return;
		_hook=GlobalEventHook.Hook(0x8004,_=>FixNow());
		FixNow();
	}));
	private static IDisposable? _hook;

	private static void FixNow(){
		if(!Enabled.Value) return;

		WinWindow window;

		if(WinWindow.FindWindow("#32770","This is an unregistered copy").NonZero(out window)&&
		   //window.Props.TryOverride("KeyControl_Handled",Environment.ProcessId)&&
		   Path.GetFileName(window.ProcessExe)=="sublime_text.exe"){
			window.AsControl.SendKey(Keys.Escape);
			Console.WriteLine("Managed: Sublime Text (Sponsored)");
		}

		if(WinWindow.FindWindow("#32770","Gesponserte Sitzung").NonZero(out window)&&
		   //window.Props.TryOverride("KeyControl_Handled",Environment.ProcessId)&&
		   Path.GetFileName(window.ProcessExe)=="TeamViewer.exe"){
			window.SendMessage(WindowMessage.WM_CLOSE,0,0);
			Console.WriteLine("Managed: TeamViewer (Sponsored)");
		}
		if(WinWindow.FindWindow("CreativeView","TeamViewer").NonZero(out window)&&
		   //window.Props.TryOverride("KeyControl_Handled",Environment.ProcessId)&&
		   Path.GetFileName(window.ProcessExe)=="TeamViewer.exe"){
			window.SendMessage(WindowMessage.WM_CLOSE,0,0);
			Console.WriteLine("Managed: TeamViewer (Ads)");
		}

		if(WinWindow.FindWindow("#32770","kdeconnectd.exe").NonZero(out window)&&
		   //window.Props.TryOverride("KeyControl_Handled",Environment.ProcessId)&&
		   Path.GetFileName(window.ProcessExe)=="WerFault.exe"){
			window.SendMessage(WindowMessage.WM_CLOSE,0,0);
			Console.WriteLine("Managed: KdeConnect (crash)");
		}
	}
}