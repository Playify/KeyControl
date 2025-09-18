using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Hotkeys.Advanced;

[InitOnLoad]
public static class CustomAction{
	private static readonly ConfigValue<string> Action=ConfigValue.Create("{F13}","Hotkeys","CustomAction","Action");
	private static readonly ConfigValue<bool> RawText=ConfigValue.Create(false,"Hotkeys","CustomAction","RawText");

	static CustomAction()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		if(e.Key!=Keys.V) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;

		if(RawText.Value) new Send().Text(Action.Value).SendNow();
		else new SendBuilder(Action.Value).SendNow();
	}
}