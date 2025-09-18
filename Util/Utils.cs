using KeyControl.Features.Strings;
using PlayifyUtility.Windows;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Util;

public static class Utils{
	public static readonly UiThread UiThread=UiThread.Create(nameof(Utils)+"."+nameof(UiThread));
	private static bool _paused;
	
	public static bool Paused{
		get=>_paused;
		set{
			if(_paused==value) return;
			_paused=value;
			GlobalKeyboardHook.Paused=value;
			GlobalMouseHook.Paused=value;
			HotStringsHandler.Reset();
		}
	}

	public static WinWindow GetCurrentWindow()
		=>Modifiers.Shift&&WinCursor.TryGetCursorPos(out var cursorPos)
			  ?WinWindow.GetWindowAt(cursorPos)
			  :WinWindow.Foreground;
}