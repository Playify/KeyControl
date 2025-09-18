using KeyControl.Util;
using PlayifyUtility.Windows.Features;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Windows.WindowProps;

[InitOnLoad]
public static class AlwaysOnTop{
	static AlwaysOnTop()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		if(e.Key!=Keys.T) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;

		var window=Utils.GetCurrentWindow();
		var b=window.AlwaysOnTop^=true;
		MouseToolTip.ShowToolTip($"AlwaysOnTop {(b?"en":"dis")}abled");
	}
}