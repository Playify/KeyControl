using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Technical;

[InitOnLoad]
public static class OfficeClipboard{
	private static readonly ConfigValue<bool> Enabled=ConfigValue.Create(true,"Technical","OfficeClipboard").Listen(b=>Utils.UiThread.Invoke(()=>{
		_hook?.Dispose();
		if(b) _hook=GlobalEventHook.Hook(0x8004,_=>FixNow());
		FixNow();
	}));
	private static IDisposable? _hook;

	private static void FixNow(){
		if(!Enabled.Value) return;

		if(!WinWindow.FindWindow("#32770",null).NonZero(out var window)) return;
		if(!window.Props.TryOverride("KeyControl_Handled",Environment.ProcessId)) return;

		var controls=window.GetControls();
		if(!controls.TryGetValue("Static2",out var textContent)) return;
		if(textContent.Text!="Möchten Sie das letzte Element, das Sie kopiert haben, beibehalten?\n\nWenn ja, kann das Beenden etwas länger dauern.") return;

		if(!controls.TryGetValue("Button1",out var button)) return;
		button.Click();

		Console.WriteLine("Handling Office Clipboard Dialog");
	}
}