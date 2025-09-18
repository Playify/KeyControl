using KeyControl.Util;
using PlayifyUtility.Windows.Features;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Windows.WindowProps;

[InitOnLoad]
public static class ClickThrough{
	static ClickThrough()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		if(e.Key!=Keys.H) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;

		var window=Utils.GetCurrentWindow();
		var b=window.ClickThrough^=true;
		MouseToolTip.ShowToolTip($"ClickThrough {(b?"en":"dis")}abled");
	}
}