using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;

namespace KeyControl.Features.Windows;

[InitOnLoad]
public static partial class MoveWindows{
	private static readonly ConfigValue<bool> EnableF1=ConfigValue.Create(true,"Windows","Move","F1").Listen(_=>_f1Down=false);
	private static readonly ConfigValue<bool> EnableVive=ConfigValue.Create(true,"Windows","Move","Vive");
	private static readonly ConfigValue<bool> Maximize=ConfigValue.Create(true,"Windows","Move","Maximize");
	private static readonly ConfigValue<bool> EnableMouse=ConfigValue.Create(true,"Windows","Move","Mouse");

	static MoveWindows(){//Dynamically adding and removing hooks would be complicated
		GlobalKeyboardHook.KeyDown+=KeyDown;
		GlobalKeyboardHook.KeyUp+=KeyUp;

		GlobalMouseHook.MouseDown+=MouseDown;
		GlobalMouseHook.MouseUp+=MouseUp;
		GlobalMouseHook.MouseMove+=MouseMove;
	}
}