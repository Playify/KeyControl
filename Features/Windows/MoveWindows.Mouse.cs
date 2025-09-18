using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Windows;

public static partial class MoveWindows{
	private static int _moveX;
	private static int _moveY;
	private static WinWindow _move;
	private static WinWindow _resize;

	private static void MouseDown(MouseEvent e){
		if(!EnableMouse.Value||!Modifiers.Win||!Modifiers.Ctrl) return;
		switch(e.Button){
			case MouseButtons.Left:
				_moveX=e.X;
				_moveY=e.Y;
				_move=Utils.GetCurrentWindow();
				e.Handled=true;
				break;
			case MouseButtons.Right:
				_moveX=e.X;
				_moveY=e.Y;
				_resize=Utils.GetCurrentWindow();
				e.Handled=true;
				break;
		}
	}

	private static void MouseUp(MouseEvent e){
		switch(e.Button){
			case MouseButtons.Left when _move!=WinWindow.Zero:
				_move=WinWindow.Zero;
				e.Handled=true;
				break;
			case MouseButtons.Right when _resize!=WinWindow.Zero:
				_resize=WinWindow.Zero;
				e.Handled=true;
				break;
		}
	}

	private static void MouseMove(MouseEvent e){
		if(EnableF1.Value&&_f1Down){
			lock(typeof(MoveWindows))
				if(_f1Running) return;
				else _f1Running=true;
			UiThread.BeginInvoke(()=>RunF1(Modifiers.Win&&Modifiers.Ctrl));
		} else _f1Down=false;


		if(_move!=WinWindow.Zero){
			var rect=_move.WindowRect;
			rect.Left+=e.X-_moveX;
			rect.Top+=e.Y-_moveY;
			if(_resize==WinWindow.Zero){
				rect.Right+=e.X-_moveX;
				rect.Bottom+=e.Y-_moveY;
			}
			_moveX=e.X;
			_moveY=e.Y;
			_move.SetWindowPos(0,rect.Left,rect.Top,rect.Right-rect.Left,rect.Bottom-rect.Top,0);
		} else if(_resize!=WinWindow.Zero){
			var rect=_resize.WindowRect;
			rect.Right+=e.X-_moveX;
			rect.Bottom+=e.Y-_moveY;
			_moveX=e.X;
			_moveY=e.Y;
			_resize.SetWindowPos(0,rect.Left,rect.Top,rect.Right-rect.Left,rect.Bottom-rect.Top,0);
		}
	}
}