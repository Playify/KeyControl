using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KeyControl.Features.Games;
using KeyControl.Interfaces;
using PlayifyUtils.Utils;
using Clipboard=System.Windows.Clipboard;

namespace KeyControl.Hooks{
	public class GlobalShellHook:Form{
		private readonly int _uMsgNotify;
		private IntPtr _hWndNextWindow;

		public Action<string> NextClipboard;
		public DateTime NextClipboardValid;

		public GlobalShellHook(){
			Visible=false;
			WindowState=FormWindowState.Minimized;
			Hide();
			_uMsgNotify=RegisterWindowMessage("SHELLHOOK");
			RegisterShellHookWindow(Handle);
		}

		protected override void WndProc(ref Message m){//TODO https://github.com/magicmanam/windows-clipboard-viewer/blob/master/magicmanam.Windows.ClipboardViewer/ClipboardViewer.cs
			try{
				switch(m.Msg){
					case 0x001://WM_CREATE
						_hWndNextWindow=SetClipboardViewer(Handle);
						break;
					case 0x0002://WM_DESTORY
						ChangeClipboardChain(Handle,_hWndNextWindow);
						break;
					case 0x030D:// WM_CHANGECBCHAIN
						if(m.WParam==_hWndNextWindow) _hWndNextWindow=m.LParam;
						else if(_hWndNextWindow!=IntPtr.Zero) SendMessage(_hWndNextWindow,m.Msg,m.WParam,m.LParam);
						break;
					case 0x0308:// WM_DRAWCLIPBOARD
						var action=NextClipboard;
						if(action!=null){
							NextClipboard=null;
							if(DateTime.Now<=NextClipboardValid){
								if(Clipboard.ContainsText()&&Clipboard.GetText().Push(out var text)!=null) action(text);
								else Console.WriteLine("Error getting selected Text");
							} else Console.WriteLine("getting selected Text timed out");
						}
						SendMessage(_hWndNextWindow,m.Msg,m.WParam,m.LParam);
						break;
				}
				if(m.Msg==_uMsgNotify){
					//Console.WriteLine(Windows.GetExe(m.LParam));
					
					var hwnd=m.LParam;
					var foreground=Windows.GetForegroundWindow();

					if(CrossHair.Enabled&&!CrossHair.IsHwndPartOfCrossHair(hwnd))
						CrossHair.UpdateAll();


					if(hwnd!=foreground) HandleSpam(foreground);
					HandleSpam(hwnd);
					
					//var hwnds=m.LParam==foreground?new[]{foreground}:new[]{m.LParam,foreground};


					static void HandleSpam(IntPtr hwnd){
							var title=Windows.GetTitle(hwnd);
							var proc=Windows.GetProcess(hwnd);
							var exe=Windows.GetExe(proc);
							var file=Path.GetFileName(exe);


							if(title=="This is an unregistered copy"&&file=="sublime_text.exe"){
								Console.WriteLine("Managed: Sublime Text");
								Windows.SendKey(hwnd,Keys.Escape);
							} else if(title=="Gesponserte Sitzung"&&file=="TeamViewer.exe"){
								Console.WriteLine("Managed: TeamViewer");
								
								Windows.SendKey(hwnd,Keys.Return);
							} else if(@"C:\program files (x86)\avira\antivirus\ipmGui.exe".Equals(exe,StringComparison.OrdinalIgnoreCase)){
								Console.WriteLine("Managed: Avira");
								proc.Kill();
								//if current implementation doesn't work:add this, and restart with /connectToHost as Argument, then disable this code for a second or so
							}/*else if(@"C:\Program Files (x86)\Avira\Launcher\Avira.Systray.exe".Equals(exe,StringComparison.OrdinalIgnoreCase)&&foreground)
							{
								Console.WriteLine("Managed: Avira2");
								proc.Kill();
								
								
							}*/
					}
				}
				base.WndProc(ref m);
			} catch(Exception e){
				Console.WriteLine(e);
			}
		}


		#region DLL imports
		[DllImport("user32.dll",EntryPoint="RegisterWindowMessageA")]
		private static extern int RegisterWindowMessage(string lpString);

		[DllImport("user32.dll")]
		private static extern int RegisterShellHookWindow(IntPtr hWnd);


		[DllImport("user32.dll")]
		private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

		[DllImport("user32.dll")]
		[return:MarshalAs(UnmanagedType.Bool)]
		private static extern bool ChangeClipboardChain(IntPtr hWndRemove,IntPtr hWndNewNext);

		[DllImport("user32.dll",SetLastError=true)]
		private static extern int SendMessage(IntPtr hWnd,int msg,IntPtr wParam,IntPtr lParam);
		#endregion

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent(){
			this.SuspendLayout();
			// 
			// GlobalShellHook
			// 
			this.ClientSize=new System.Drawing.Size(284,261);
			this.Name="GlobalShellHook";
			this.ResumeLayout(false);
		}
	}

	public delegate int ShellHookProc(int code,int wParam,long lParam);
}