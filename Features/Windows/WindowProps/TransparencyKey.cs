using KeyControl.Util;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Windows.Features;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Windows.WindowProps;

[InitOnLoad]
public static class TransparencyKey{
	static TransparencyKey()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		if(e.Key!=Keys.X) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;

		var window=Utils.GetCurrentWindow();
		var c=window.TransparentColor=window.TransparentColor==null?WinCursor.GetColorUnderCursor():null;
		MouseToolTip.ShowToolTip($"Transparent Color:  {
			(c.TryGet(out var color)?color.GetRgb().ToString("X6"):"disabled")}");
	}
}