using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyControl.Hooks{
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
		private IntPtr _hhook=IntPtr.Zero;
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
			if(_hhook!=IntPtr.Zero) return;
			_proc=HookProc;
			_hhook=SetWindowsHookEx(WhMouseLl,_proc,GetModuleHandle(IntPtr.Zero),0);
		}

		public void Unhook(){
			if(_hhook==IntPtr.Zero) return;
			UnhookWindowsHookEx(_hhook);
			_hhook=IntPtr.Zero;
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
						MouseButtons button;
						bool down;
						switch(wParam){
							case 513:
								(button,down)=(MouseButtons.Left,true);
								break;
							case 514:
								(button,down)=(MouseButtons.Left,false);
								break;
							case 516:
								(button,down)=(MouseButtons.Right,true);
								break;
							case 517:
								(button,down)=(MouseButtons.Right,false);
								break;
							case 519:
								(button,down)=(MouseButtons.Middle,true);
								break;
							case 520:
								(button,down)=(MouseButtons.Middle,false);
								break;
							case 523:
								(button,down)=(lParam.mouseData==0x10000?MouseButtons.XButton1:MouseButtons.XButton2,true);
								break;
							case 524:
								(button,down)=(lParam.mouseData==0x10000?MouseButtons.XButton1:MouseButtons.XButton2,false);
								break;
							default:
								(button,down)=(MouseButtons.None,false);
								break;
						}
						var mouseEvent=new MouseEvent(lParam.pt.x,lParam.pt.y,button);
						(down?KeyDown:KeyUp)?.Invoke(mouseEvent);
						if(mouseEvent.Handled) return 1;
					}
				}
			} catch(Exception e){
				Console.WriteLine("Error in KeyboardHook");
				Console.WriteLine(e);
			}
			return CallNextHookEx(_hhook,code,wParam,ref lParam);
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
}