using System.Drawing;
using System.Windows.Forms;
using KeyControl.Interfaces;
using KeyControl.Utilities;

namespace KeyControl.Features;

public static class MoveWindows{
	public static bool Enabled=true;
	public static bool IsKeyDown=false;
	public static bool Maximize=true;

	public static void InitConfig(){
		Config.Register(nameof(MoveWindows)+"."+nameof(Enabled),()=>Enabled,j=>Enabled=j.AsBoolean());
		Config.Register(nameof(MoveWindows)+"."+nameof(Maximize),()=>Maximize,j=>Maximize=j.AsBoolean());
	}

	public static bool Execute(ref Send.LeftRight repressWinOnF1){
		var mods=Modifiers.Combined;

		//If only windows is pressed, then do normal F1
		if(mods==ModifierKeys.Windows){
			var send=new Send();
			send.Key(Keys.Escape);//Cancel Windows Button
			if(Modifiers.IsKeyDown(Keys.LWin)){
				repressWinOnF1.L=true;
				send.Key(Keys.LWin,false);
			}
			if(Modifiers.IsKeyDown(Keys.RWin)){
				repressWinOnF1.R=true;
				send.Key(Keys.RWin,false);
			}
			send.SendNow();

			return false;
		}
		//Only move window if no additional modifier is pressed
		if(mods!=ModifierKeys.None) return false;

		if(!Enabled) return false;

		Run();
		return true;
	}

	public static void Run(){

		var hwnd=Windows.GetForegroundWindow();

		Windows.GetWindowRect(hwnd,out var rect);
		var rectangle=Rectangle.FromLTRB(rect.Left,rect.Top,rect.Right,rect.Bottom);
		var beforeScreen=Screen.FromRectangle(rectangle);

		Windows.GetCursorPos(out var point);
		var afterScreen=Screen.FromPoint(new Point(point.x,point.y));


		var maximize=false;

		if(Windows.IsFullscreen(hwnd)||Windows.IsMaximized(hwnd)) rectangle=afterScreen.Bounds;
		else{
			rectangle.Location+=new Size(afterScreen.Bounds.Location-new Size(beforeScreen.Bounds.Location));
			rectangle.Size+=afterScreen.Bounds.Size-beforeScreen.Bounds.Size;
			maximize=Maximize&&!Windows.IsMaximized(hwnd);//Only for non fullscreen windows
		}

		Windows.MoveWindow(hwnd,rectangle.X,rectangle.Y,rectangle.Width,rectangle.Height,true);
		if(maximize) Windows.SetMaximized(hwnd,true);

	}
}