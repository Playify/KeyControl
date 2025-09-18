using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Strings;

public static partial class HotStringsHandler{
	private static readonly Keys[] Valid=[
		Keys.Capital,Keys.None,Keys.Packet,Keys.Pause,Keys.Play,Keys.Print,Keys.Scroll,Keys.PrintScreen,Keys.LMenu,Keys.LControlKey,Keys.LShiftKey,Keys.MediaStop,Keys.NoName,Keys.NumLock,Keys.RMenu,Keys.RControlKey,Keys.RShiftKey,Keys.VolumeDown,
		Keys.VolumeMute,Keys.VolumeUp,Keys.MediaNextTrack,Keys.MediaPlayPause,Keys.MediaPreviousTrack,
	];

	static HotStringsHandler(){
		InitOnLoadAttribute.OnAfter(1,()=>{
			GlobalMouseHook.MouseDown+=_=>Reset();
			GlobalKeyboardHook.KeyDown+=KeyDown;
		});
	}

	private static void KeyDown(KeyEvent e){
		if(e.Handled) return;


		if(e.Key==Keys.Escape||Modifiers.Win){
			Reset();
			return;
		}

		var s=e.GetUnicode(out var i);
		if(i==0&&!Valid.Contains(e.Key)) Reset();
		else if(WriteChars(s,i)) e.Handled=true;
	}

	public static bool WriteChars(string appending,int bufferedCount){
		var empty=true;
		appending=appending.Replace('\r','\n');
		foreach(var c in appending)
			switch(c){
				case '\t':
				case '\n':
					goto default;
				case '\b':
					Delete();
					break;
				case '\x7f'://Delete All = Ctrl+Backspace
				case{} when char.IsControl(c):
					Reset();
					break;
				default:
					Builder.Append(c);
					empty=false;
					break;
			}
		if(empty||bufferedCount<0) return false;

		var result=ExecuteNow(bufferedCount);

		if(appending.EndsWith('\n')||appending.EndsWith('\t')) Reset();

		return result;
	}
}