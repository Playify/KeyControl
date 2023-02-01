using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KeyControl.Hooks;
using KeyControl.Utilities;

namespace KeyControl.Interfaces;

public static class Text{
	public static void CopyText(Action<string> a,bool restore=true){
		Program.Shell.NextClipboardValid=DateTime.Now.AddMilliseconds(10000);
		if(!restore) Program.Shell.NextClipboard=a;
		else{
			var image=Clipboard.ContainsImage()?Clipboard.GetImage():null;
			var text=Clipboard.ContainsText()?Clipboard.GetText():null;
			var fileDropList=Clipboard.ContainsFileDropList()?Clipboard.GetFileDropList():null;
			Program.Shell.NextClipboard=s=>{
				try{
					//Restore Clipboard
					if(image!=null) Clipboard.SetImage(image);
					else if(text!=null) Clipboard.SetText(text);
					else if(fileDropList!=null) Clipboard.SetFileDropList(fileDropList);
					else Clipboard.Clear();
				} catch(Exception e){
					Console.WriteLine(e);
				}
				a(s);
			};
		}
		//SendKeys.Send("^c");
		new Send().Hide().Mod(ModifierKeys.Control).Key(Keys.C).SendNow();//needs to be hidden to not activate other hotkeys
		//new Send().Mod(ModifierKeys.None).Mod(ModifierKeys.Control).Wait().Key(Keys.C,true).Wait().Key(Keys.C,false).SendNow();
	}


	[DllImport("user32.dll")]
	private static extern bool PostMessage(IntPtr hWnd,uint msg,IntPtr wParam,IntPtr lParam);

	public static void PrintChar(char c){
		var window=Windows.GetForegroundWindow();
		PostMessage(window,0x102,new IntPtr(c),IntPtr.Zero);
	}

	#region ToolTip DLL Imports
	[DllImport("user32.dll")]
	private static extern IntPtr CreateWindowEx(int dwExStyle,string lpClassName,string lpWindowName,uint dwStyle,int x,int y,int nWidth,int nHeight,IntPtr hWndParent,IntPtr hMenu,IntPtr hInstance,IntPtr lpParam);

	[DllImport("user32.dll",CharSet=CharSet.Unicode)]
	private static extern bool SendMessage(IntPtr hWnd,uint msg,uint wParam,IntPtr lParam);

	[DllImport("user32.dll",CharSet=CharSet.Unicode)]
	private static extern bool SendMessage(IntPtr hWnd,uint msg,uint wParam,ref ToolInfo lParam);
	#endregion

	#region ToolTip
	private static IntPtr _currentToolTip;
	private static ToolInfo _toolInfo;

	private static void ToolTip()=>ToolTip(null);

	public static void ToolTip(string s){
		Console.WriteLine(s==null?"ToolTip Off":"ToolTip:"+s);
		if(s==null)
			if(_toolInfo.lpszText==null) return;
			else{
				_toolInfo.lpszText=null;
				SendMessage(_currentToolTip,0x411,0,ref _toolInfo);//TTM_TRACKACTIVATE
				return;
			}

		if(_currentToolTip==IntPtr.Zero){
			_currentToolTip=CreateWindowEx(0x28,//WS_EX_TRANSPARENT|WS_EX_TOPMOST
			                               "tooltips_class32",//TOOLTIPS_CLASS
			                               null,0x33,//TTS_NOANIMATE|TTS_NOFADE|TTS_NOPREFIX|TTS_ALWAYSTIP
			                               int.MinValue,int.MinValue,int.MinValue,int.MinValue,//CW_USEDEFAULT
			                               IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);

			AppDomain.CurrentDomain.ProcessExit+=(_,_)=>Windows.DestroyWindow(_currentToolTip);


			_toolInfo=new ToolInfo();
			_toolInfo.cbSize=(uint) Marshal.SizeOf(_toolInfo);
			_toolInfo.uFlags=0x120;//TTF_TRACK
			_toolInfo.hwnd=IntPtr.Zero;
			_toolInfo.hInst=IntPtr.Zero;
			_toolInfo.uId=(UIntPtr) 0;
			_toolInfo.lpszText=s;
			_toolInfo.rect=new Windows.Rect();

			SendMessage(_currentToolTip,0x432,0,ref _toolInfo);//TTM_ADDTOOLW

			SendMessage(_currentToolTip,0x418,0,IntPtr.Zero);//TTM_SETMAXTIPWIDTH
		}
		if(_toolInfo.lpszText!=s){
			_toolInfo.lpszText=s;
			SendMessage(_currentToolTip,0x439,0,ref _toolInfo);//TTM_UPDATETIPTEXTW
		}

		CorrectToolTip(null);

		SendMessage(_currentToolTip,0x411,1,ref _toolInfo);//TTM_TRACKACTIVATE
		Windows.SetWindowPos(_currentToolTip,new IntPtr(-1),0,0,0,0,0x13);

		Scheduler.RunLater(1000,ToolTip,true);
	}


	public static void CorrectToolTip(MouseEvent e){//Coordinates can be out of screen, therefore, GetCursorPos is required
		if(_toolInfo.lpszText==null) return;
		if(!Windows.GetCursorPos(out var p)) return;
		p.x+=16;
		p.y+=16;
		SendMessage(_currentToolTip,0x412,0,(IntPtr) (((p.y&0xffff)<<16)|(p.x&0xffff)));//TTM_TRACKPOSITION
	}
	#endregion
}

[StructLayout(LayoutKind.Sequential)]
public struct ToolInfo{
	public uint cbSize;
	public uint uFlags;
	public IntPtr hwnd;
	public UIntPtr uId;
	public Windows.Rect rect;
	public IntPtr hInst;
	[MarshalAs(UnmanagedType.LPWStr)]
	public string lpszText;
	// ReSharper disable once MemberCanBePrivate.Global
	public IntPtr lParam;
}