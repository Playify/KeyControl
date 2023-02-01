using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using KeyControl.Utilities;

namespace KeyControl.Interfaces;

public static class Windows{
	private static readonly IntPtr NegOne=new(-1);
	private static readonly IntPtr NegTwo=new(-2);

	static Windows()=>AppDomain.CurrentDomain.ProcessExit+=(_,_)=>RestoreAllWindows();

	#region Click Through
	public static bool SetClickThrough(IntPtr hwnd,bool? b){
		var l=GetWindowLong(hwnd,-20);
		l|=0x80000;
		if(b.HasValue)
			if(b.Value) l|=0x20;
			else l&=~0x20;
		else l^=0x20;
		SetWindowLong(hwnd,-20,l);
		return (l&0x20)!=0;
	}
	#endregion

	public static IntPtr GetCurrentWindow()=>Modifiers.Shift?GetWindowUnderCursor():GetForegroundWindow();

	#region DLL Imports
	[DllImport("user32.dll",CharSet=CharSet.Auto)]
	public static extern IntPtr FindWindow(string lpClassName,string lpWindowName);

	[DllImport("user32.dll",CharSet=CharSet.Auto)]
	public static extern IntPtr SendMessage(IntPtr hWnd,uint msg,uint wParam,uint lParam);

	[DllImport("user32.dll")]
	public static extern bool SetWindowPos(IntPtr hWnd,IntPtr hWndInsertAfter,int x,int y,int cx,int cy,uint uFlags);

	[DllImport("user32.dll")]
	public static extern bool GetWindowRect(IntPtr hWnd,out Rect rect);

	[DllImport("user32.dll")]
	public static extern int GetWindowLong(IntPtr hWnd,int nIndex);

	[DllImport("user32.dll")]
	public static extern int SetWindowLong(IntPtr hWnd,int nIndex,int dwNewLong);

	[DllImport("user32.dll")]
	public static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern IntPtr WindowFromPoint(Point lpPoint);

	[DllImport("user32.dll")]
	public static extern bool GetCursorPos(out Point lpPoint);

	[DllImport("user32.dll")]
	public static extern bool SetCursorPos(int x,int y);

	[DllImport("user32.dll",ExactSpelling=true,CharSet=CharSet.Auto)]
	public static extern IntPtr GetParent(IntPtr hWnd);

	public static IntPtr GetWindowUnderCursor()=>GetCursorPos(out var ptCursor)?GetWindowAt(ptCursor):IntPtr.Zero;

	public static IntPtr GetWindowAt(Point pos){
		var hwnd=WindowFromPoint(pos);
		while(true){
			var parent=GetParent(hwnd);
			if(parent==IntPtr.Zero) return hwnd;
			hwnd=parent;
		}
	}

	public static IntPtr GetInvisibleWindowUnderCursor(){
		var hWnd=GetForegroundWindow();

		GetWindowRect(hWnd,out var rect);
		GetCursorPos(out var ptCursor);

		if(rect.Left<=ptCursor.x&&ptCursor.x<=rect.Right&&rect.Top<=ptCursor.y&&ptCursor.y<=rect.Bottom) return hWnd;
		return GetWindowAt(ptCursor);
	}


	[DllImport("user32.dll",CharSet=CharSet.Unicode)]
	private static extern int GetWindowThreadProcessId(IntPtr handle,out uint processId);

	[DllImport("user32.dll",CharSet=CharSet.Auto)]
	private static extern int GetWindowText(IntPtr hWnd,StringBuilder title,int size);

	[DllImport("user32.dll",CharSet=CharSet.Auto)]
	private static extern int GetWindowTextLength(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern bool ShowWindow(IntPtr hWnd,ShowWindowCommands nCmdShow);

	[DllImport("user32.dll")]
	private static extern bool SetLayeredWindowAttributes(IntPtr hWnd,ColorRef color,byte alpha,int dwFlags);

	[DllImport("user32.dll")]
	private static extern bool GetLayeredWindowAttributes(IntPtr hWnd,out ColorRef color,out byte alpha,out int dwFlags);

	[StructLayout(LayoutKind.Sequential)]
	public struct ColorRef{
		private readonly byte R;
		private readonly byte G;
		private readonly byte B;
		private readonly byte A;

		public ColorRef(Color color){
			R=color.R;
			G=color.G;
			B=color.B;
			A=0;
		}

		public uint GetRgb()=>(uint) ((R<<16)|(G<<8)|B);
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Point{
		public int x;
		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct AnimationInfo{
		public uint cbSize;
		public int iMinAnimate;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Rect{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public override string ToString()=>$"({Left},{Top})->({Right},{Bottom})";
	}

	[DllImport("User32.dll")]
	private static extern bool SystemParametersInfo(uint uiAction,uint uiParam,ref AnimationInfo pvParam,uint fWinIni);


	[DllImport("gdi32.dll",CharSet=CharSet.Auto,SetLastError=true,ExactSpelling=true)]
	public static extern int BitBlt(IntPtr hDC,int x,int y,int nWidth,int nHeight,IntPtr hSrcDC,int xSrc,int ySrc,int dwRop);


	[DllImport("user32.dll",SetLastError=true,CharSet=CharSet.Auto)]
	private static extern bool PostMessage(IntPtr hWnd,uint msg,int wParam,int lParam);

	[DllImport("user32.dll")]
	private static extern bool GetClientRect(IntPtr hWnd,out Rect lpRect);

	[DllImport("kernel32.dll")]
	public static extern IntPtr GetConsoleWindow();

	[DllImport("user32.dll")]
	public static extern bool DestroyWindow(IntPtr hwnd);

	[DllImport("user32.dll")]
	[return:MarshalAs(UnmanagedType.Bool)]
	public static extern bool IsWindow(IntPtr hWnd);


	[DllImport("user32.dll")]
	private static extern bool GetWindowPlacement(IntPtr hwnd,ref WindowPlacement lpwndpl);


	[Serializable,StructLayout(LayoutKind.Sequential)]
	internal struct WindowPlacement{
		public int length;
		public int flags;
		public ShowWindowCommands showCmd;
		public Point ptMinPosition;
		public Point ptMaxPosition;
		public Rectangle rcNormalPosition;
	}

	public enum ShowWindowCommands{
		Hide=0,
		Normal=1,
		Minimized=2,
		Maximized=3,
		Show=5,
	}
	#endregion

	#region AlwaysOnTop
	public static bool IsAlwaysOnTop(IntPtr hwnd)=>(GetWindowLong(hwnd,-20)&8)==8;

	public static void SetAlwaysOnTop(IntPtr hwnd,bool b)=>SetWindowPos(hwnd,b?NegOne:NegTwo,0,0,0,0,3);
	#endregion


	#region Hide Windows
	private static readonly Stack<IntPtr> Hidden=new();

	public static void HideWindow(IntPtr hwnd,bool store=true){
		if(store) Hidden.Push(hwnd);
		ShowWindow(hwnd,ShowWindowCommands.Hide);
	}

	public static void RestoreWindow(){
		if(Hidden.Count==0) return;
		var hwnd=Hidden.Pop();
		ShowWindow(hwnd,ShowWindowCommands.Show);
	}

	public static void RestoreAllWindows(){
		while(Hidden.Count!=0) RestoreWindow();
	}
	#endregion

	#region Transparency
	public static byte GetAlpha(IntPtr hwnd){
		GetLayeredWindowAttributes(hwnd,out _,out var alpha,out var dw);
		return (dw&2)!=0?alpha:(byte) 255;
	}

	public static ColorRef? GetTransparentColor(IntPtr hwnd){
		GetLayeredWindowAttributes(hwnd,out var clr,out _,out var dw);
		return (dw&1)!=0?clr:null;
	}

	public static byte SetAlpha(IntPtr hwnd,int val,bool delta){
		var l=GetWindowLong(hwnd,-20);
		if((l&0x80000)==0) SetWindowLong(hwnd,-20,l|0x80000);

		GetLayeredWindowAttributes(hwnd,out var color,out var alpha,out var dw);

		if((dw&2)==0) alpha=255;

		if(delta) val+=alpha;

		SetLayeredWindowAttributes(hwnd,color,alpha=(byte) (val<0?0:val>255?255:val),dw|2);

		return alpha;
	}

	public static void SetTransparentColor(IntPtr hwnd,ColorRef? color){
		var l=GetWindowLong(hwnd,-20);
		if((l&0x80000)==0) SetWindowLong(hwnd,-20,l|0x80000);

		GetLayeredWindowAttributes(hwnd,out var c,out var alpha,out var dw);

		SetLayeredWindowAttributes(hwnd,color??c,alpha,color.HasValue?dw|1:dw&2);
	}
	#endregion

	#region Get Color
	public static Color GetPixelColorUnderMouse(){
		Point p;
		GetCursorPos(out p);
		return GetColorAt(p);
	}

	private static readonly Bitmap screenPixel=new(1,1,PixelFormat.Format32bppArgb);

	public static Color GetColorAt(Point location){
		using(var gdest=Graphics.FromImage(screenPixel))
		using(var gsrc=Graphics.FromHwnd(IntPtr.Zero)){
			var hSrcDC=gsrc.GetHdc();
			var hDC=gdest.GetHdc();
			var retval=BitBlt(hDC,0,0,1,1,hSrcDC,location.x,location.y,(int) CopyPixelOperation.SourceCopy);
			gdest.ReleaseHdc();
			gsrc.ReleaseHdc();
		}

		return screenPixel.GetPixel(0,0);
	}
	#endregion

	#region WindowInfo & SendKey
	public static Process GetProcess(IntPtr hwnd){
		try{
			GetWindowThreadProcessId(hwnd,out var pid);
			return Process.GetProcessById((int) pid);
		} catch(Exception){
			return null;
		}
	}

	public static string GetExe(IntPtr hwnd)=>GetExe(GetProcess(hwnd));

	private static readonly Dictionary<int,string> _exe=new();

	public static string GetExe(Process process){
		try{
			var id=process.Id;
			if(id==0) return "";
			if(_exe.TryGetValue(id,out var exe)) return exe;
			return _exe[id]=process?.MainModule?.FileName??"";
		} catch(Exception){
			return "";
		}
	}

	public static string GetTitle(IntPtr hwnd){
		try{
			if(!IsWindow(hwnd)) return null;
			var length=GetWindowTextLength(hwnd)+1;
			var title=new StringBuilder(length);
			GetWindowText(hwnd,title,length);
			return title.ToString();
		} catch(AccessViolationException e){
			Console.WriteLine(e);
			return null;
		}
	}

	public static void SendKey(IntPtr hwnd,Keys keys){
		PostMessage(hwnd,0x100,(int) keys,0);//WM_KEYDOWN
		PostMessage(hwnd,0x101,(int) keys,0);//WM_KEYUP
	}
	#endregion

	#region Borderless
	public static bool SetBorderless(IntPtr hwnd,bool? b){
		var l=GetWindowLong(hwnd,-16);//-16=GWL_STYLE
		var ret=!(b??(l&0xC40000)!=0);
		l=Utils.SetBits(l,0xc40000,ret);//0xC40000=WS_CAPTION|WS_THICKFRAME
		SetWindowLong(hwnd,-16,l);
		SetWindowPos(hwnd,IntPtr.Zero,0,0,0,0,0x27);
		GetClientRect(hwnd,out var rect);
		PostMessage(hwnd,5,0,((rect.Bottom-rect.Top)<<16)|((rect.Right-rect.Left)&0xffff));
		return ret;
	}

	private static readonly Dictionary<IntPtr,Rect> FullScreened=new();

	public static void SetFullscreen(IntPtr hwnd,bool? b){
		if(b!=null&&b.Value==FullScreened.ContainsKey(hwnd)) return;
		if(FullScreened.TryGetValue(hwnd,out var rect)){
			var l=GetWindowLong(hwnd,-16);//-16=GWL_STYLE
			l|=0xc40000;//0xC40000=WS_CAPTION|WS_THICKFRAME
			SetWindowLong(hwnd,-16,l);
			SetWindowPos(hwnd,IntPtr.Zero,rect.Left,rect.Top,rect.Right-rect.Left,rect.Bottom-rect.Top,0x4);
			GetClientRect(hwnd,out rect);
			PostMessage(hwnd,5,0,((rect.Bottom-rect.Top)<<16)|((rect.Right-rect.Left)&0xffff));
			FullScreened.Remove(hwnd);
		} else{
			GetWindowRect(hwnd,out rect);
			var screen=Screen.FromRectangle(new Rectangle(rect.Left,rect.Top,rect.Right-rect.Left,rect.Bottom-rect.Top));
			var bnd=screen.Bounds;

			var l=GetWindowLong(hwnd,-16);//-16=GWL_STYLE
			l&=~0xc40000;//0xC40000=WS_CAPTION|WS_THICKFRAME
			SetWindowLong(hwnd,-16,l);

			FullScreened.Add(hwnd,rect);
			SetWindowPos(hwnd,IntPtr.Zero,bnd.X,bnd.Y,bnd.Width,bnd.Height,0);
			GetClientRect(hwnd,out rect);
			PostMessage(hwnd,5,0,((rect.Bottom-rect.Top)<<16)|((rect.Right-rect.Left)&0xffff));
		}
	}

	public static unsafe bool IsMaximized(IntPtr hwnd){
		var placement=new WindowPlacement{length=sizeof(WindowPlacement)};

		GetWindowPlacement(hwnd,ref placement);

		return placement.showCmd==ShowWindowCommands.Maximized;
	}

	public static void SetMaximized(IntPtr hwnd,bool? b)=>ShowWindow(hwnd,b??!IsMaximized(hwnd)?ShowWindowCommands.Maximized:ShowWindowCommands.Normal);
	#endregion
}