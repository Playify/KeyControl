using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Utils;
using PlayifyUtility.Windows.Features;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Windows.WindowProps;

[InitOnLoad]
public static class Alpha{
	private static readonly ConfigValue<int> AlphaSpeed=ConfigValue.Create(15,"Windows","Alpha");

	static Alpha(){
		GlobalMouseHook.MouseDown+=MouseDown;
		GlobalMouseHook.MouseScroll+=MouseScroll;
	}

	private static void MouseScroll(MouseEvent e){
		if(AlphaSpeed.Value==0) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;

		var window=Utils.GetCurrentWindow();
		var alpha=MathsUtils.ClampByte(window.Alpha+e.Delta*AlphaSpeed.Value);
		window.Alpha=alpha;
		MouseToolTip.ShowToolTip($"Alpha: {alpha,3}/255");
	}

	private static void MouseDown(MouseEvent e){
		if(AlphaSpeed.Value==0) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		if(e.Button!=MouseButtons.Middle) return;
		e.Handled=true;

		var window=Utils.GetCurrentWindow();
		var alpha=(byte)(window.Alpha!=255?255:0);
		window.Alpha=alpha;
		MouseToolTip.ShowToolTip($"Alpha: {alpha,3}/255");
	}
}