using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyControl.Hooks{
	public class GlobalKeyboardHook{

		#region Constant, Structure and Delegate Definitions
		public delegate int KeyboardHookProc(int code,int wParam,ref KeyboardHookStruct lParam);

		public struct KeyboardHookStruct{
			public int VkCode;
			public int ScanCode;
			public int Flags;
			public int Time;
			public IntPtr DwExtraInfo;

			public override string ToString()=>$"{nameof(VkCode)}: {VkCode}, {nameof(ScanCode)}: {ScanCode}, {nameof(Flags)}: {Flags}";
		}

		private const int WhKeyboardLl=13;
		private const int WmKeydown=0x100;
		private const int WmKeyup=0x101;
		private const int WmSyskeydown=0x104;
		private const int WmSyskeyup=0x105;
		#endregion

		#region Instance Variables
		private IntPtr _hhook=IntPtr.Zero;
		private KeyboardHookProc _proc;
		#endregion

		#region Events
		public event KeyEventHandler KeyDown;
		public event KeyEventHandler KeyUp;
		#endregion

		#region Constructors and Destructors
		public GlobalKeyboardHook(bool hook){
			if(hook) Hook();
		}

		~GlobalKeyboardHook()=>Unhook();
		#endregion

		#region Public Methods
		public void Hook(){
			if(_hhook!=IntPtr.Zero) return;
			_proc=HookProc;
			_hhook=SetWindowsHookEx(WhKeyboardLl,_proc,GetModuleHandle(IntPtr.Zero),0);
		}

		public void Unhook(){
			if(_hhook==IntPtr.Zero) return;
			UnhookWindowsHookEx(_hhook);
			_hhook=IntPtr.Zero;
		}


		private int HookProc(int code,int wParam,ref KeyboardHookStruct lParam){
			try{
				if(code>=0&&lParam.DwExtraInfo!=Program.Handle){
					var key=(Keys)lParam.VkCode;
					//if(!Config.Constant.BetterCircumflex&&(key==Keys.OemPipe||key==Keys.OemCloseBrackets)) return CallNextHookEx(_hhook,code,wParam,ref lParam);
					var kea=new KeyEvent(key,lParam.VkCode,lParam.ScanCode);
					switch(wParam){
						case WmKeydown:
						case WmSyskeydown:
							KeyDown?.Invoke(this,kea);
							break;
						case WmKeyup:
						case WmSyskeyup:
							KeyUp?.Invoke(this,kea);
							break;
					}
					if(kea.Handled) return 1;
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
		private static extern IntPtr SetWindowsHookEx(int idHook,KeyboardHookProc callback,IntPtr hInstance,uint threadId);

		[DllImport("user32.dll")]
		private static extern bool UnhookWindowsHookEx(IntPtr hInstance);

		[DllImport("user32.dll")]
		private static extern int CallNextHookEx(IntPtr idHook,int nCode,int wParam,ref KeyboardHookStruct lParam);

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetModuleHandle(IntPtr zero);
		#endregion

	}
}