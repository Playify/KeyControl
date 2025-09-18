using System.Diagnostics;
using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Hotkeys;

[InitOnLoad]
public static class Google{
	private static readonly ConfigValue<string> SearchEngine=ConfigValue.Create("https://google.com/search?q=","Hotkeys","SearchEngine");

	static Google(){
		GlobalKeyboardHook.KeyDown+=KeyDown;
	}

	private static void KeyDown(KeyEvent e){
		if(e.Key!=Keys.G) return;
		if(!Modifiers.Win||!Modifiers.Ctrl) return;
		e.Handled=true;

		GlobalClipboardHook.CopyString().Then(s=>{
			if(s==null) Console.WriteLine("Could not copy link/text");
			else{
				if(!(Uri.TryCreate(s,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https")) uri=new Uri(SearchEngine.Value+Uri.EscapeDataString(s));
				Process.Start(new ProcessStartInfo(uri.ToString()){
					UseShellExecute=true,
				});
			}
		}).Catch<TaskCanceledException>(_=>{}).Background();
	}
}