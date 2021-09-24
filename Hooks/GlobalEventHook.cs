using System;
using System.Runtime.InteropServices;
using KeyControl.Features.Games;

namespace KeyControl.Hooks{
	public class GlobalEventHook{
		private IntPtr _hook;
		private WinEventProc _proc;


		public GlobalEventHook()=>Hook();

		[DllImport("user32.dll")]
		private static extern IntPtr SetWinEventHook(uint eventMin,uint eventMax,IntPtr hmodWinEventProc,WinEventProc lpfnWinEventProc,int idProcess,int idThread,uint dwflags);

		[DllImport("user32.dll")]
		private static extern int UnhookWinEvent(IntPtr hWinEventHook);


		~GlobalEventHook()=>UnhookWinEvent(_hook);

		private void Hook(){
			_proc=HookProc;
			_hook=SetWinEventHook(0x1000,0x8FFF,IntPtr.Zero,_proc,0,0,2);
		}

		private void HookProc(IntPtr hwineventhook,int ievent,IntPtr hwnd,int idobject,int idchild,int dweventthread,int dwmseventtime){
			if(ievent>=0x4000&&ievent<=0x4fff) return;
			if(ievent<=0x1000) return;
			switch(ievent){
				/*case 0x4001://Console
				case 0x4002:
				case 0x4003:
				case 0x4004:
					return;*/
				case 0x8001:
				case 0x800c:
				case 0x800e:return;
				case 0x800b when hwnd==IntPtr.Zero:return;
			}
			if(ievent==0x800b){
				if(hwnd==IntPtr.Zero) return;
				
				
				if(CrossHair.Enabled&&!CrossHair.IsHwndPartOfCrossHair(hwnd)){
					CrossHair.UpdateAll();
				}
				/*
				var placement=new WindowPlacement();
				placement.length=Marshal.SizeOf(placement);
				GetWindowPlacement(hwnd,ref placement);
				if(placement.showCmd==ShowWindowCommands.Maximized){
					Windows.GetWindowRect(hwnd,out var rect);
					var rect2=new Rectangle(rect.x1+8,rect.y1+8,rect.x2-rect.x1-16,rect.y2-rect.y1-16);
					foreach(var screen in Screen.AllScreens)
						if(screen.WorkingArea==rect2){
							var bounds=screen.Bounds;
							var b=Windows.SetWindowPos(hwnd,(IntPtr)(-1),bounds.Left-8,bounds.Top-8,bounds.Width+16,bounds.Height+16,0);
							Console.WriteLine(screen+" "+b);
							return;
						}
				}
				Console.WriteLine($"MOVE:{hwnd} {placement.showCmd.ToString()}");*/

			}
		}

		private delegate void WinEventProc(IntPtr hWinEventHook,int iEvent,IntPtr hWnd,int idObject,int idChild,int dwEventThread,int dwmsEventTime);

	}
}