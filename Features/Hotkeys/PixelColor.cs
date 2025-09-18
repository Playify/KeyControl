using KeyControl.Util;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Windows.Features;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Hotkeys;

[InitOnLoad]
public static class PixelColor{
	static PixelColor()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		if(e.Key!=Keys.C) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;

		if(!WinCursor.GetColorUnderCursor().TryGet(out var colorInstance)) return;
		var color=(colorInstance.ToArgb()&0xFFFFFF).ToString("X6");
		if(!Clipboard.ContainsText()||Clipboard.GetText()!=color)
			Clipboard.SetText(color);
		MouseToolTip.ShowToolTip($"Pixel color: {color}");
	}
}