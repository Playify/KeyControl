using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Windows.WindowProps;

[InitOnLoad]
public static class WinHide{
	static WinHide()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		if(e.Key==Keys.M){
			if(!Modifiers.Win||!Modifiers.Ctrl) return;
			e.Handled=true;

			var window=Utils.GetCurrentWindow();

			//Desktop or Taskbar should not be hidden
			if(window.Class is "Progman" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return;

			window.HidePush();

			/*//Was not an issue yet. For now, don't press additional buttons if not really needed
			if(window==WinWindow.Foreground)//Focus another window, hidden window should not have focus
				new Send().Combo(ModifierKeys.Alt,Keys.Escape).SendNow();//*/
		}
		if(e.Key==Keys.N){
			if(!Modifiers.Win||!Modifiers.Ctrl) return;
			e.Handled=true;

			WinWindow.RestoreLast();
		}
	}
}