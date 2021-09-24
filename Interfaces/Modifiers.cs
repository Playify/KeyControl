using System;
using System.Windows.Input;

namespace KeyControl.Interfaces{
	public class Modifiers{
		public static bool Shift=>Keyboard.IsKeyDown(Key.LeftShift)||Keyboard.IsKeyDown(Key.RightShift);
		public static bool Alt=>Keyboard.IsKeyDown(Key.LeftAlt);

		public static bool AltGr=>Ctrl&&Keyboard.IsKeyDown(Key.RightAlt);
		public static bool Ctrl=>Keyboard.IsKeyDown(Key.LeftCtrl)||Keyboard.IsKeyDown(Key.RightCtrl);
		public static bool Win=>Keyboard.IsKeyDown(Key.LWin)||Keyboard.IsKeyDown(Key.RWin);

		public static ModifierKeys Combined
			=>(Shift?ModifierKeys.Shift:ModifierKeys.None)|
			  (Win?ModifierKeys.Windows:ModifierKeys.None)|
			  (AltGr
			   ?ModifierKeys.AltGr
			   :(Alt?ModifierKeys.Alt:ModifierKeys.None)|
			    (Shift?ModifierKeys.Shift:ModifierKeys.None));
	}

	[Flags]
	public enum ModifierKeys{
		None=0,
		Alt=1,
		Control=2,
		Shift=4,
		Windows=8,
		AltGr=Alt|Control|16,
		//All=Alt|Control|Shift|Windows
	}
}