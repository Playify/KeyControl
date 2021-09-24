using System;
using System.Windows.Forms;
using KeyControl.Hooks;
using KeyControl.Interfaces;
using KeyControl.Utilities;

namespace KeyControl.HotKeyHandler{
	public static class MouseHandler{

		private static int _moveX;
		private static int _moveY;
		private static IntPtr? _move;
		private static IntPtr? _resize;

		public static void Move(MouseEvent e){
			if(_move.HasValue){
				Windows.GetWindowRect(_move.Value,out var rect);
				rect.x1+=e.X-_moveX;
				rect.y1+=e.Y-_moveY;
				if(!_resize.HasValue){
					rect.x2+=e.X-_moveX;
					rect.y2+=e.Y-_moveY;
				}
				_moveX=e.X;
				_moveY=e.Y;
				Windows.SetWindowPos(_move.Value,IntPtr.Zero,rect.x1,rect.y1,rect.x2-rect.x1,rect.y2-rect.y1,0);
			} else if(_resize.HasValue){
				Windows.GetWindowRect(_resize.Value,out var rect);
				rect.x2+=e.X-_moveX;
				rect.y2+=e.Y-_moveY;
				_moveX=e.X;
				_moveY=e.Y;
				Windows.SetWindowPos(_resize.Value,IntPtr.Zero,rect.x1,rect.y1,rect.x2-rect.x1,rect.y2-rect.y1,0);
			} else if(_resize.HasValue){
				Windows.GetWindowRect(_resize.Value,out var rect);
				rect.x2+=e.X-_moveX;
				rect.y2+=e.Y-_moveY;
				_moveX=e.X;
				_moveY=e.Y;
				Windows.SetWindowPos(_resize.Value,IntPtr.Zero,rect.x1,rect.y1,rect.x2-rect.x1,rect.y2-rect.y1,0);
			}/*
            if(Config.Playify&&Environment.MachineName=="PLAYIFY"&&e.Y<-5)
            {
	            var x1=-640;
	            var x2=-896;
	            x1+=1;
	            x2-=1;
	            if(e.X<=x1&&e.X>=x2)
	            {
		            var i=e.X-(x1+x2)/2;
		            Windows.SetCursorPos((i<0?e.X+x1-x2:e.X-x1+x2),e.Y);
		            e.Handled=true;
	            }
            }*/
		}

		public static void Scroll(MouseEvent e){
			if(Modifiers.Win&&Modifiers.Ctrl){
				var alpha=Windows.SetAlpha(Windows.GetForegroundWindow(),e.Delta*Config.TransparencySpeed,true);
				Text.ToolTip($"Alpha: {alpha,3:##0}/255");
				e.Handled=true;
			}
		}

		public static void Down(MouseEvent e){
			
			if(KeyboardHandler.KeepDown!=null){
				KeyboardHandler.KeepDown.Add(e.Key);
				KeyboardHandler.ReleaseKeys[e.Key]=Keys.None;
			}
			
			
			if(Modifiers.Win&&Modifiers.Ctrl)
				switch(e.Button){
					case MouseButtons.Left:
						_moveX=e.X;
						_moveY=e.Y;
						_move=Windows.GetCurrentWindow();
						e.Handled=true;
						break;
					case MouseButtons.Right:
						_moveX=e.X;
						_moveY=e.Y;
						_resize=Windows.GetCurrentWindow();
						e.Handled=true;
						break;
					case MouseButtons.Middle:
						var foregroundWindow=Windows.GetCurrentWindow();
						var alpha=Windows.GetAlpha(foregroundWindow)!=255?255:0;
						Windows.SetAlpha(foregroundWindow,alpha,false);
						Text.ToolTip($"Alpha: {alpha,3:##0}/255");
						e.Handled=true;
						break;
				}
			Logger.LogMouse(e.Button);
		}

		public static void Up(MouseEvent e){
			if(KeyboardHandler.KeepDown!=null&&KeyboardHandler.KeepDown.Contains(e.Key))
				KeyboardHandler.KeepDown=null;

			if(KeyboardHandler.ReleaseKeys.ContainsKey(e.Key)){
				var key=KeyboardHandler.ReleaseKeys[e.Key];
				KeyboardHandler.ReleaseKeys.Remove(e.Key);
				if(key==e.Key) return;
				e.Handled=true;
				if(key!=Keys.None){
					new Send().Key(key,false).SendNow();
					//Windows.SendKey(key,false);
				}
				return;
			}
			if(e.Button==MouseButtons.Left&&_move!=null){
				_move=null;
				e.Handled=true;
			}
			if(e.Button==MouseButtons.Right&&_resize!=null){
				_resize=null;
				e.Handled=true;
			}
		}
	}
}