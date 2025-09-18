using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Strings.Calculate;

[InitOnLoad]
public static class CalculateNumLock{
	private static readonly ConfigValue<bool> Enabled=ConfigValue.Create(true,"Calculate","NumLock").Listen(b=>{
		if(b&&!Modifiers.IsNumLock)
			new Send().Hide().Key(Keys.NumLock).SendOn(Utils.UiThread);
	});

	static CalculateNumLock()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static CancellationTokenSource _cancel=new();

	private static void KeyDown(KeyEvent e){
		if(e.Handled) return;

		_cancel.Cancel();

		if(e.Key!=Keys.NumLock) return;
		if(!Enabled.Value) return;

		if(Modifiers.Shift||Modifiers.Ctrl||!Modifiers.IsNumLock) return;//dont replace
		e.Handled=true;
		var send=new Send().Hide().Key(Keys.NumLock,false);
		if(Modifiers.IsNumLock) send.Key(Keys.NumLock);
		send.Key(Keys.NumLock,true).SendNow();


		CancellationTokenSource cts;
		_cancel=cts=new CancellationTokenSource(TimeSpan.FromSeconds(1));
		GlobalMouseHook.MouseDown+=MouseDown;

		GlobalClipboardHook.CopyString(cts.Token).Then(s=>{
			if(s==null) Console.WriteLine("Could not copy expression");
			else if(CalculateExpression.Evaluate(ref s).NotNull(out var result))
				if(result!=s) new Send().Text(result).SendOn(Utils.UiThread);
				else Console.WriteLine("Expression is same as result");
			else Console.WriteLine("Could not calculate result from expression \""+s+"\"");
		}).Catch<TaskCanceledException>(_=>{}).Finally(()=>GlobalMouseHook.MouseDown-=MouseDown).Background();
	}

	private static void MouseDown(MouseEvent e){
		if(e.Handled) return;
		_cancel.Cancel();
	}
}