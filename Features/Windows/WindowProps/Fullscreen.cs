using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Windows.WindowProps;

[InitOnLoad]
public static class Fullscreen{
	static Fullscreen()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		if(e.Key!=Keys.F) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;

		var window=Utils.GetCurrentWindow();

		if(Modifiers.Alt){
			window.Fullscreen=false;
			window.Maximized^=true;
		} else window.Fullscreen^=true;
	}
}