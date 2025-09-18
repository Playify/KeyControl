using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Hotkeys;

[InitOnLoad(1)]
public class Hotkeys{
	static Hotkeys()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		//Exit if misbehaving
		if(e.Key==Keys.Escape){
			if(!Modifiers.Win||!Modifiers.Ctrl) return;
			e.Handled=true;
			Console.WriteLine("Exit triggered");
			Environment.Exit(0);
		}

		//Useful for games that block the mouse from exiting the window
		if(e.Key==Keys.Cancel){//Pause key becomes Cancel when Ctrl is pressed
			if(!Modifiers.Win||!Modifiers.Ctrl) return;
			e.Handled=true;
			Console.WriteLine("Focusing Taskbar");
			WinWindow.Foreground=WinWindow.FindWindow("Shell_TrayWnd",null);
		}

		if(e.Key==Keys.F4){
			if(!Modifiers.Win||!Modifiers.Ctrl) return;
			e.Handled=true;
			var process=WinWindow.Foreground.Process;
			process?.Kill();
		}
	}
}