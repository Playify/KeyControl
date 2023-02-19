using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using KeyControl.Hooks;
using KeyControl.HotString.Saveable;
using KeyControl.Interfaces;
using KeyControl.Utilities;
using PlayifyUtils.Utils;

namespace KeyControl.HotKeyHandler;

public static class HotStringHandler{
	public const string Ending="[ \n\t]$";
	private static readonly StringBuilder Builder=new();
	public static int Emoji=-1;

	private static readonly Keys[] Valid={
		Keys.Capital,Keys.None,Keys.Packet,Keys.Pause,Keys.Play,Keys.Print,Keys.Scroll,Keys.PrintScreen,Keys.LMenu,Keys.LControlKey,Keys.LShiftKey,Keys.MediaStop,Keys.NoName,Keys.NumLock,Keys.RMenu,Keys.RControlKey,Keys.RShiftKey,Keys.VolumeDown,
		Keys.VolumeMute,Keys.VolumeUp,Keys.MediaNextTrack,Keys.MediaPlayPause,Keys.MediaPreviousTrack,
	};

	private static readonly byte[] KeyboardState=new byte[256];
	public static HotStringEmoji CurrentEmoji;

	public static void Reset(){
		Emoji=-1;
		Builder.Clear();
	}

	private static bool Execute(KeyEvent e,int deleteCount){
		if(deleteCount<0) return false;

		var s=Builder.ToString();

		Console.WriteLine("Possible Hotstring:"+s);

		CurrentEmoji=null;
		try{
			(int bs,string s) replace=(-1,null);
			foreach(var tuple in Config.HotStrings.Select(child=>child.Replace(s))){
				if(!tuple.TryGet(out var tmp)) continue;
				replace=tmp;
				break;
			}
			var currentEmoji=CurrentEmoji;
			if(replace.bs<0||replace.s==null){
				Emoji=-1;
				return false;
			}

			var (bs,replacement)=replace;
			Console.Write("HotString: bs="+bs+" s="+replacement);

			var index=0;
			var max=Math.Min(bs,replacement.Length);
			var length=s.Length-bs;
			while(index!=max&&s[length+index]==replacement[index]) index++;
			if(index!=0){
				bs-=index;
				replacement=replacement.Substring(index);
				Console.WriteLine(" => Optimized: bs="+bs+" s="+replacement);
			} else Console.WriteLine();


			var emoji=Emoji;
			Emoji=-1;

			CancelDeadKeys(e);

			if(bs!=deleteCount||replacement!=""){
				Logger.LogChars(new string('\b',deleteCount),e,0);

				if(deleteCount>bs) s=s.Substring(s.Length-deleteCount,deleteCount-bs);
				else s=new string('\b',bs-deleteCount);
				s+=replacement;

				if(!Logger.LogChars(s,e,-1)) new Send().Hide().Text(s).SendNow();
			}
			Emoji=emoji;
			if(currentEmoji!=null&&Config.EmojiTimeout>=0){
				if(Emoji==-1) Emoji=1;
				else Emoji++;
				Scheduler.RunLater(Config.EmojiTimeout,()=>Emoji=-1);
			} else Emoji=-1;
			return true;
		} finally{
			CurrentEmoji=null;
		}
	}

	public static bool Write(string s,KeyEvent e,int deleteCount=1){
		var nonEmpty=false;
		s=s.Replace('\r','\n');
		foreach(var c in s)
			switch(c){
				case '\t':
				case '\n':
					Builder.Append(c);
					nonEmpty=true;
					break;
				case '\b':
					Delete();
					break;
				case '\x7f'://Delete All = Ctrl+Backspace
					Reset();
					break;
				default:
					if(char.IsControl(c)){
						Reset();
						break;
					}
					Builder.Append(c);
					nonEmpty=true;
					break;
			}
		if(!nonEmpty) return false;
		var exec=Execute(e,deleteCount);
		if(s.EndsWith("\n")) Reset();
		if(s.EndsWith("\t")) Reset();
		return exec;
	}

	private static void Delete(){
		if(Emoji>=0) Emoji--;
		if(Builder.Length==0) return;
		if(char.GetUnicodeCategory(Builder[Builder.Length-1])==UnicodeCategory.Surrogate) Builder.Length-=2;
		else Builder.Length--;
	}

	public static void Reset(MouseEvent e)=>Reset();

	public static void Down(object sender,KeyEvent e){
		if(e.Handled) return;
		if(e.Key==Keys.Packet){
			Logger.LogPacket(e);
			return;
		}


		//GetKeyboardState(KeyboardState);

		KeyboardState[(int) Keys.LControlKey]=(byte) (Modifiers.IsKeyDown(Keys.LControlKey)?0x80:0);
		KeyboardState[(int) Keys.RControlKey]=(byte) (Modifiers.IsKeyDown(Keys.RControlKey)?0x80:0);
		KeyboardState[(int) Keys.ControlKey]=(byte) (KeyboardState[(int) Keys.LControlKey]|KeyboardState[(int) Keys.RControlKey]);
		KeyboardState[(int) Keys.LMenu]=(byte) (Modifiers.IsKeyDown(Keys.LMenu)?0x80:0);
		KeyboardState[(int) Keys.RMenu]=(byte) (Modifiers.IsKeyDown(Keys.RMenu)?0x80:0);
		KeyboardState[(int) Keys.Menu]=(byte) (KeyboardState[(int) Keys.LMenu]|KeyboardState[(int) Keys.RMenu]);//*/

		/*if(KeyboardState[(int)Keys.ControlKey]!=0&&KeyboardState[(int)Keys.RMenu]==0&&e.Key!=Keys.Back){
			if(e.Key!=Keys.LControlKey&&e.Key!=Keys.RControlKey&&
			   e.Key!=Keys.LMenu&&e.Key!=Keys.RMenu&&
			   e.Key!=Keys.LShiftKey&&e.Key!=Keys.RShiftKey)
			//to do log once
				Logger.LogControl(e.Key);

			return;
		}*/
		if(e.Key==Keys.Escape||Modifiers.Win){
			Reset();
			return;
		}
		KeyboardState[(int) Keys.LShiftKey]=(byte) (Modifiers.IsKeyDown(Keys.LShiftKey)?0x80:0);
		KeyboardState[(int) Keys.RShiftKey]=(byte) (Modifiers.IsKeyDown(Keys.RShiftKey)?0x80:0);
		KeyboardState[(int) Keys.ShiftKey]=(byte) (KeyboardState[(int) Keys.LShiftKey]|KeyboardState[(int) Keys.RShiftKey]);
		KeyboardState[(int) Keys.CapsLock]=(byte) (Modifiers.IsCapsLock?0x80:0);
		KeyboardState[(int) Keys.NumLock]=(byte) (Modifiers.IsNumLock?0x80:0);//*/

		var str=new StringBuilder(10);
		var i=ToUnicode(e.VkCode,e.ScanCode,KeyboardState,str,str.Capacity,4);
		if(i>0){
			if(Logger.LogChars(str.ToString(),e,i)) e.Handled=true;
		} else if(i==0&&Array.IndexOf(Valid,e.Key)==-1) Reset();
	}

	public static void Up(object sender,KeyEvent e){
		//TO DO single Log Modifiers

	}


	#region DLL Imports
	[DllImport("user32.dll")]
	private static extern int ToUnicode(int wVirtKey,int wScanCode,byte[] lpKeyState,[MarshalAs(UnmanagedType.LPWStr)]StringBuilder str,int capacity,int flags);

	/*[DllImport("user32.dll")]
	private static extern bool GetKeyboardState(byte[] lpKeyState);*/
	#endregion

	private static void CancelDeadKeys(KeyEvent e){
		var str=new StringBuilder(10);
		//flag 0 to consume all dead keys
		ToUnicode(e.VkCode,e.ScanCode,KeyboardState,str,str.Capacity,0);
	}
}