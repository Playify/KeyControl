using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Windows.WindowProps;

[InitOnLoad]
public static class Borderless{
	static Borderless()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		if(e.Key!=Keys.B) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;

		var window=Utils.GetCurrentWindow();
		window.Borderless^=true;
	}
}