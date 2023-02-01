using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KeyControl.Features.Games;
using KeyControl.Interfaces;
using PlayifyUtils.Utils;
using Clipboard=System.Windows.Clipboard;

namespace KeyControl.Hooks;

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
				case 0x0002://WM_DESTROY
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
				switch(m.WParam.ToInt32()){
					case 0x1:{
						var hwnd=m.LParam;
						var title=Windows.GetTitle(hwnd);
						var proc=Windows.GetProcess(hwnd);
						var exe=Windows.GetExe(proc);
						var file=Path.GetFileName(exe);


						if(title=="This is an unregistered copy"&&file=="sublime_text.exe"){
							Windows.SendKey(hwnd,Keys.Escape);
							Console.WriteLine("Managed: Sublime Text (Sponsored)");
						} else if(@"C:\program files (x86)\avira\antivirus\ipmGui.exe".Equals(exe,StringComparison.OrdinalIgnoreCase)){
							Console.WriteLine("Managed: Avira");
							proc.Kill();
						} else if(@"C:\program files (x86)\avira\antivirus\".Equals(exe,StringComparison.OrdinalIgnoreCase)){
							//old version
							Console.WriteLine("Managed: Avira ");
							proc.Kill();
						} else if(@"C:\Program Files (x86)\Avira\Security\Avira.Spotlight.UI.Application.Messaging.exe".Equals(exe,StringComparison.OrdinalIgnoreCase)){
							//will not work, because window would nee UI Access
							Console.WriteLine("Managed: Avira Security");
							proc.Kill();
						} else if(@"C:\Program Files\Malwarebytes\Anti-Malware\mbamtray.exe".Equals(exe,StringComparison.OrdinalIgnoreCase)){
							Console.WriteLine("Managed: Malwarebytes 0x1");
							proc.Kill();
							Process.Start(exe);//restart tray icon
						}
						break;
					}
					case 0x2:{
						var hwnd=Windows.FindWindow("#32770","This is an unregistered copy");
						if(hwnd!=IntPtr.Zero&&Path.GetFileName(Windows.GetExe(hwnd))=="sublime_text.exe"){
							Windows.SendKey(hwnd,Keys.Escape);
							Console.WriteLine("Managed: Sublime Text (Sponsored)");
						}

						hwnd=Windows.FindWindow("#32770","Gesponserte Sitzung");
						if(hwnd!=IntPtr.Zero&&Path.GetFileName(Windows.GetExe(hwnd))=="TeamViewer.exe"){
							Windows.SendMessage(hwnd,0x10,0,0);//WM_CLOSE
							Console.WriteLine("Managed: TeamViewer (Sponsored)");
						}

						hwnd=Windows.FindWindow("CreativeView","TeamViewer");
						if(hwnd!=IntPtr.Zero&&Path.GetFileName(Windows.GetExe(hwnd))=="TeamViewer.exe"){
							Windows.SendMessage(hwnd,0x10,0,0);//WM_CLOSE
							Console.WriteLine("Managed: TeamViewer (Ads)");
						}

						hwnd=Windows.FindWindow("NUIDialog","Mit Ihrer Office-Lizenz ist ein Problem aufgetreten");
						if(hwnd!=IntPtr.Zero/*&&Path.GetFileName(Windows.GetExe(hwnd))=="WINWORD.EXE"*/){
							Windows.SendMessage(hwnd,0x10,0,0);//WM_CLOSE
							Console.WriteLine("Managed: Office (Licence) 0x2");
						}
						break;
					}
					case 0x6:{
						var hwnd=Windows.FindWindow("NUIDialog","Mit Ihrer Office-Lizenz ist ein Problem aufgetreten");
						if(hwnd!=IntPtr.Zero/*&&Path.GetFileName(Windows.GetExe(hwnd))=="WINWORD.EXE"*/){
							Windows.SendMessage(hwnd,0x10,0,0);//WM_CLOSE
							Console.WriteLine("Managed: Office (Licence) 0x6");
						}
						break;
					}
					case 0x8004:{//Office license check
						var hwnd=Windows.FindWindow("NUIDialog","Mit Ihrer Office-Lizenz ist ein Problem aufgetreten");
						if(hwnd!=IntPtr.Zero/*&&Path.GetFileName(Windows.GetExe(hwnd))=="WINWORD.EXE"*/){
							Windows.SendMessage(hwnd,0x10,0,0);//WM_CLOSE
							Console.WriteLine("Managed: Office (Licence) 0x8004");
						}
						break;
					}
				}

				if(CrossHair.Enabled&&!CrossHair.IsHwndPartOfCrossHair(m.LParam)) CrossHair.UpdateAll();
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
}