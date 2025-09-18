using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Hotkeys.Advanced;

[InitOnLoad]
public static class CapsLock{
	private static readonly ConfigValue<bool> Enabled=ConfigValue.Create(true,"Hotkeys","CapsLock","Enabled").Listen(b=>{
		if(b&&Modifiers.IsCapsLock)
			new Send().Hide().Key(Keys.CapsLock).SendOn(Utils.UiThread);
	});
	private static readonly ConfigValue<bool> CsGoT=ConfigValue.Create(true,"Hotkeys","CapsLock","CsGoT");
	private static readonly ConfigValue<string> Action=ConfigValue.Create("{Apps}","Hotkeys","CapsLock","Action");

	static CapsLock()=>GlobalKeyboardHook.KeyDown+=e=>e.Handled=KeyDown(e);

	private static bool KeyDown(KeyEvent e){
		if(e.Handled) return true;
		if(e.Key!=Keys.CapsLock) return false;

		if(Modifiers.Win){
			if(!Modifiers.Shift&&!Modifiers.Alt&&!Modifiers.Ctrl) KeyEvent.CancelWindowsKey();
			ConfigWindow.ToggleOpen();
			return true;
		}
		if(Modifiers.Shift||Modifiers.Ctrl||Modifiers.IsCapsLock) return false;//dont replace

		if(CsGoT.Value){
			if("csgo.exe".Equals(Path.GetFileName(WinWindow.Foreground.ProcessExe),StringComparison.OrdinalIgnoreCase)){
				new Send().Key(Keys.T,true).SendNow();
				GlobalKeyboardHook.OnRelease[Keys.CapsLock]=Keys.T;
				return true;
			}
		}


		if(!Enabled.Value) return false;

		var action=Action.Value;
		if(SendBuilder.IsSingleKey(action,out var key)){
			if(key==Keys.CapsLock) return false;
			new Send().Key(key,true).SendNow();
			GlobalKeyboardHook.OnRelease[Keys.CapsLock]=key;
		} else new SendBuilder(action).SendNow();

		return true;
	}
}