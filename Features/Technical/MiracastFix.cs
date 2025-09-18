using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Technical;

[InitOnLoad]
public static class MiracastFix{
	private static readonly ConfigValue<bool> Enabled=ConfigValue.Create(true,"Technical","MiracastFix").Listen(b=>Utils.UiThread.Invoke(()=>{
		_hook?.Dispose();
		if(b) _hook=GlobalEventHook.Hook(0x8004,_=>FixNow());
		FixNow();
	}));
	private static IDisposable? _hook;

	private static void FixNow(){
		if(!Enabled.Value) return;

		if(WinSystem.KeyboardDelay!=1) WinSystem.KeyboardDelay=1;
	}
}