using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Windows.Forms.Keys;

namespace KeyControl.Utilities;

public static class Modifiers{
	[DllImport("USER32.dll")]
	private static extern short GetKeyState(Keys key);

	public static bool Shift=>(GetKeyState(LShiftKey)&128)!=0||(GetKeyState(RShiftKey)&128)!=0;
	public static bool Alt=>(GetKeyState(LMenu)&128)!=0;

	public static bool AltGr=>Ctrl&&(GetKeyState(RMenu)&128)!=0;
	public static bool Ctrl=>(GetKeyState(LControlKey)&128)!=0||(GetKeyState(RControlKey)&128)!=0;
	public static bool Win=>(GetKeyState(LWin)&128)!=0||(GetKeyState(RWin)&128)!=0;

	public static ModifierKeys Combined
		=>(Shift?ModifierKeys.Shift:ModifierKeys.None)|
		  (Win?ModifierKeys.Windows:ModifierKeys.None)|
		  (AltGr
		   ?ModifierKeys.AltGr
		   :(Alt?ModifierKeys.Alt:ModifierKeys.None)|
		    (Ctrl?ModifierKeys.Control:ModifierKeys.None));

	public static bool IsNumLock=>(GetKeyState(NumLock)&1)!=0;
	public static bool IsCapsLock=>(GetKeyState(CapsLock)&1)!=0;
	public static bool IsScrollLock=>(GetKeyState(Scroll)&1)!=0;

	public static bool IsKeyDown(Keys keys)=>(GetKeyState(keys)&128)!=0;
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