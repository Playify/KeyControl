using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyControl.Hooks;

public class GlobalMouseHook{
	#region Constant, Structure and Delegate Definitions
	private delegate int MouseHookProc(int code,int wParam,ref MsLlHookStruct lParam);

	private const int WhMouseLl=14;

	[StructLayout(LayoutKind.Sequential)]
	public struct Point{
		public int x;
		public int y;
	}


	[StructLayout(LayoutKind.Sequential)]
	public struct MsLlHookStruct{
		public Point pt;
		public int mouseData;
		public uint flags;
		public uint time;
		public IntPtr dwExtraInfo;
	}
	#endregion

	#region Instance Variables
	private IntPtr _hook=IntPtr.Zero;
	private MouseHookProc _proc;
	#endregion

	#region Events
	public event MouseEventHandler KeyDown;
	public event MouseEventHandler KeyUp;
	public event MouseEventHandler MouseMove;
	public event MouseEventHandler MouseScroll;
	#endregion

	#region Constructors and Destructors
	public GlobalMouseHook(bool hook){
		if(hook) Hook();
	}

	~GlobalMouseHook()=>Unhook();
	#endregion

	#region Public Methods
	public void Hook(){
		if(_hook!=IntPtr.Zero) return;
		_proc=HookProc;
		_hook=SetWindowsHookEx(WhMouseLl,_proc,GetModuleHandle(IntPtr.Zero),0);
	}

	public void Unhook(){
		if(_hook==IntPtr.Zero) return;
		UnhookWindowsHookEx(_hook);
		_hook=IntPtr.Zero;
	}

	public int HookProc(int code,int wParam,ref MsLlHookStruct lParam){
		try{
			if(code>=0){
				if(wParam==512){
					var mouseEvent=new MouseEvent(lParam.pt.x,lParam.pt.y,MouseButtons.None);
					MouseMove?.Invoke(mouseEvent);
					if(mouseEvent.Handled) return 1;
				} else if(wParam==522){
					var mouseEvent=new MouseEvent(lParam.pt.x,lParam.pt.y,MouseButtons.None,Math.Sign(lParam.mouseData));
					MouseScroll?.Invoke(mouseEvent);
					if(mouseEvent.Handled) return 1;
				} else{
					var (button,down)=
						wParam switch{
							513=>(MouseButtons.Left,true),
							514=>(MouseButtons.Left,false),
							516=>(MouseButtons.Right,true),
							517=>(MouseButtons.Right,false),
							519=>(MouseButtons.Middle,true),
							520=>(MouseButtons.Middle,false),
							523=>(lParam.mouseData==0x10000?MouseButtons.XButton1:MouseButtons.XButton2,true),
							524=>(lParam.mouseData==0x10000?MouseButtons.XButton1:MouseButtons.XButton2,false),
							_=>(MouseButtons.None,false),
						};
					var mouseEvent=new MouseEvent(lParam.pt.x,lParam.pt.y,button);
					(down?KeyDown:KeyUp)?.Invoke(mouseEvent);
					if(mouseEvent.Handled) return 1;
				}
			}
		} catch(Exception e){
			Console.WriteLine("Error in KeyboardHook");
			Console.WriteLine(e);
		}
		return CallNextHookEx(_hook,code,wParam,ref lParam);
	}
	#endregion

	#region DLL imports
	[DllImport("user32.dll")]
	private static extern IntPtr SetWindowsHookEx(int idHook,MouseHookProc callback,IntPtr hInstance,uint threadId);

	[DllImport("user32.dll")]
	private static extern bool UnhookWindowsHookEx(IntPtr hInstance);

	[DllImport("user32.dll")]
	private static extern int CallNextHookEx(IntPtr idHook,int nCode,int wParam,ref MsLlHookStruct lParam);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetModuleHandle(IntPtr zero);
	#endregion
}