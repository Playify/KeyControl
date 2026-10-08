using System.ComponentModel;
using PlayifyUtility.Windows;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;
using PlayifyUtility.Windows.Win.Native;

namespace KeyControl.Features.Windows;

public static partial class MoveWindows{
	private static readonly UiThread UiThread=UiThread.Create(nameof(MoveWindows));
	private static bool _lWin;
	private static bool _rWin;
	private static bool _f1Down;
	private static bool _f1Running;

	private static void KeyDown(KeyEvent e){
		if(e.Key!=Keys.F1&&!(e.Key==Keys.Packet&&EnableVive.Value&&e.ScanCode=='£')) return;
		var mods=Modifiers.Combined;
		
		_f1Down=e.Key==Keys.F1&&
		        (((mods&ModifierKeys.Windows)!=0&&(mods&ModifierKeys.Control)!=0)||mods==ModifierKeys.None);
		if(!EnableF1.Value) return;


		//If only windows is pressed, then do normal F1
		if(mods==ModifierKeys.Windows){
			KeyEvent.CancelWindowsKey();
			var send=new Send();
			if(Modifiers.IsKeyDown(Keys.LWin)){
				_lWin=true;
				send.Key(Keys.LWin,false);
			}
			if(Modifiers.IsKeyDown(Keys.RWin)){
				_rWin=true;
				send.Key(Keys.RWin,false);
			}
			send.SendNow();
		} else if((mods&ModifierKeys.Windows)!=0&&(mods&ModifierKeys.Control)!=0){
			lock(typeof(MoveWindows))
				if(_f1Running) return;
				else _f1Running=true;
			e.Handled=RunF1(true);
		} else if(mods==ModifierKeys.None){
			lock(typeof(MoveWindows))
				if(_f1Running) return;
				else _f1Running=true;
			e.Handled=RunF1();
		}
	}

	private static void KeyUp(KeyEvent e){
		if(e.Key!=Keys.F1) return;
		_f1Down=false;
		if(_lWin||_rWin){
			var send=new Send();
			if(_lWin) send.Key(Keys.LWin,true);
			if(_rWin) send.Key(Keys.RWin,true);
			send.SendNow();
			KeyEvent.CancelWindowsKey();
			_lWin=_rWin=false;
		}
	}

	public static bool RunF1(bool swap=false){
		var window=WinWindow.Foreground;
		var handled=true;
		if(!WinCursor.TryGetCursorPos(out var cursorPos)||!IsValidWindow(window,cursorPos,out handled)){
			lock(typeof(MoveWindows))
				_f1Running=false;
			return handled;
		}
		var other=swap?WinWindow.GetWindowAt(cursorPos):WinWindow.Zero;

		UiThread.BeginInvoke(()=>{
			if(IsValidWindow(other,cursorPos,out _)) SwapWindows(window,other);
			else MoveWindow(window,cursorPos);

			lock(typeof(MoveWindows))
				_f1Running=false;
		});
		return true;
	}

	private static bool IsValidWindow(WinWindow window,Point cursorPos,out bool handled){
		handled=true;
		if(window==WinWindow.Zero) return false;

		//Desktop or Taskbar should not be moved
		if(window.Class is "Progman" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "WorkerW") return false;
		
		//Some windows should not be moved, as they cause trouble otherwise
		if((window.Title?.EndsWith(" - Administratorzugriff - Getscreen.me - Google Chrome")??false)
		   &&((Rectangle)window.WindowRect).Contains(cursorPos)) return handled=false;
		var processExe=Path.GetFileName(window.ProcessExe);
		if(processExe=="paintdotnet.exe"&&(window.ExStyle&ExStyle.ToolWindow)!=0) return false;
		if(processExe=="PowerToys.MouseWithoutBordersHelper.exe") return handled=false;

		return true;
	}

	private static void MoveWindow(WinWindow window,Point pos){
		try{
			Rectangle rectangle=window.WindowRect;
			var beforeScreen=Screen.FromRectangle(rectangle);
			var afterScreen=Screen.FromPoint(pos);


			window.SetTransitionsForceDisabled(true);


			var maximize=false;
			var isWin11=Environment.OSVersion.Version.Build>22000;//Win11 doesn't allow MoveWindow on already maximized windows, therefore a workaround is needed
			if(isWin11&&beforeScreen.Bounds!=afterScreen.Bounds&&window.Maximized){
				maximize=true;
				window.Maximized=false;
				rectangle=window.WindowRect;
			}

			if(window.Fullscreen||window.Maximized) rectangle=afterScreen.Bounds;
			else{
				rectangle.Location+=new Size(afterScreen.Bounds.Location-new Size(beforeScreen.Bounds.Location));
				rectangle.Size+=afterScreen.Bounds.Size-beforeScreen.Bounds.Size;
				if(!maximize)
					maximize=Maximize.Value&&!window.Maximized;//Only for non fullscreen windows
			}


			window.MoveWindow(rectangle,!maximize);
			if(maximize) window.Maximized=true;//maximizing redraws, therefore MoveWindow doesn't need to //but it does anyway, in hopes of fixing redraw bugs

			window.SetTransitionsForceDisabled(false);
			window.Redraw();
		} catch(Win32Exception e){
			Console.WriteLine(e);
		}
	}

	private static void SwapWindows(WinWindow win1,WinWindow win2){
		if(win1==win2) return;

		Rectangle rec1=win1.WindowRect;
		Rectangle rec2=win2.WindowRect;
		var screen1=Screen.FromRectangle(rec1);
		var screen2=Screen.FromRectangle(rec2);


		win1.SetTransitionsForceDisabled(true);
		win2.SetTransitionsForceDisabled(true);

		var max1=false;
		var max2=false;
		var isWin11=Environment.OSVersion.Version.Build>22000;//Win11 doesn't allow MoveWindow on already maximized windows, therefore a workaround is needed

		if(isWin11&&screen1.Bounds!=screen2.Bounds){
			if(win1.Maximized){
				max2=true;
				win1.Maximized=false;
				rec1=win1.WindowRect;
			}
			if(win2.Maximized){
				max1=true;
				win2.Maximized=false;
				rec2=win2.WindowRect;
			}
		}


		if(win1.Fullscreen||win1.Maximized) rec1=screen2.Bounds;
		else{
			rec1.Location+=new Size(screen2.Bounds.Location-new Size(screen1.Bounds.Location));
			rec1.Size+=screen2.Bounds.Size-screen1.Bounds.Size;
			if(!max2) max2=Maximize.Value&&!win1.Maximized;
		}
		if(win2.Fullscreen||win2.Maximized) rec2=screen1.Bounds;
		else{
			rec2.Location+=new Size(screen1.Bounds.Location-new Size(screen2.Bounds.Location));
			rec2.Size+=screen1.Bounds.Size-screen2.Bounds.Size;
			if(!max1) max1=Maximize.Value&&!win2.Maximized;
		}

		win1.MoveWindow(rec1,!max1);
		win2.MoveWindow(rec2,!max2);
		if(max1) win1.Maximized=true;
		if(max2) win2.Maximized=true;

		win1.SetTransitionsForceDisabled(false);
		win2.SetTransitionsForceDisabled(false);

		win1.Redraw();
		win2.Redraw();
	}
}