using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Hotkeys;

[InitOnLoad]
public static class KeepDown{
	private static HashSet<Keys>? _set;

	static KeepDown(){
		GlobalKeyboardHook.KeyDown+=KeyDown;
		GlobalKeyboardHook.KeyUp+=KeyUp;
		GlobalMouseHook.MouseDown+=MouseDown;
		GlobalMouseHook.MouseUp+=MouseUp;
	}

	private static void KeyDown(KeyEvent e){
		if(_set!=null){
			_set.Add(e.Key);
			GlobalKeyboardHook.OnRelease[e.Key]=null;
		}

		if(e.Key!=Keys.K) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;
		_set??=new HashSet<Keys>();
	}

	private static void MouseDown(MouseEvent e){
		if(_set!=null){
			_set.Add(e.Key);
			GlobalKeyboardHook.OnRelease[e.Key]=null;
		}

		if(e.Key!=Keys.K) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;
		_set??=new HashSet<Keys>();
	}

	private static void KeyUp(KeyEvent e){
		if(_set?.Contains(e.Key)??false)
			_set=null;
	}

	private static void MouseUp(MouseEvent e){
		if(_set?.Contains(e.Key)??false)
			_set=null;
	}
}