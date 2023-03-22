using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using KeyControl.Features;
using KeyControl.Features.Games;
using KeyControl.Hooks;
using KeyControl.HotString.Complex;
using KeyControl.Interfaces;
using KeyControl.Utilities;
using PlayifyUtils.Utils;

namespace KeyControl.HotKeyHandler;

public static class KeyboardHandler{
	public static readonly Dictionary<Keys,Keys> ReleaseKeys=new();
	public static HashSet<Keys> KeepDown;
	private static Send.LeftRight _repressWinOnF1;

	public static void Down(object sender,KeyEvent e){
		try{
			if(e.Key==Keys.Escape&&Modifiers.Ctrl&&Modifiers.Win){
				Program.Exit("Ctrl+Win+Esc");
				return;
			}
			if(KeepDown!=null){
				KeepDown.Add(e.Key);
				ReleaseKeys[e.Key]=Keys.None;
			}

			if(Modifiers.Combined==ModifierKeys.AltGr&&SpecialChars.All.TryGetValue(e.Key,out var text)){
				e.Handled=true;
				if(!Logger.LogChars(text,e,text.Length)) new Send().Hide().Text(text).SendNow();
				return;
			}

			switch(e.Key){/*
					case Keys.RControlKey:{
						if(NamingHelper.Execute())
							e.Handled=true;
						return;
					}*/
				case Keys.Packet when MoveWindows.AllowVive&&e.ScanCode=='£'://allow Vive Keyboard to move windows
				case Keys.F1:{
					if(MoveWindows.Execute(ref _repressWinOnF1)){
						MoveWindows.IsF1KeyDown=e.Key==Keys.F1;
						e.Handled=true;
					} else MoveWindows.IsF1KeyDown=false;
					return;
				}
				case Keys.CapsLock:{
					if(CapsLock.Execute()) e.Handled=true;
					return;
				}
				case Keys.NumLock:{
					if(Modifiers.Shift||Modifiers.Ctrl||!Modifiers.IsNumLock) return;//dont replace
					e.Handled=true;
					var send=new Send().Hide().Key(Keys.NumLock,false);
					if(Modifiers.IsNumLock) send.Key(Keys.NumLock);
					send.Key(Keys.NumLock,true).SendNow();

					Text.CopyText(s=>{
						var calculate=HotStringComplexCalc.Calculate(ref s);
						if(calculate!=null)
							if(calculate!=s) new Send().Text(calculate).SendNow();
							else Console.WriteLine("Expression is same as result");
						else Console.WriteLine("Could not calculate result from expression \""+s+"\"");
					});
					return;
				}
				case Keys.V:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					CrossHair.Enabled^=true;
					Text.ToolTip($"CrossHair {(CrossHair.Enabled?"en":"dis")}abled");
					ConfigProvider.SendUpdate(nameof(Features.Games)+"."+nameof(CrossHair)+"."+nameof(CrossHair.Enabled));
					return;
				case Keys.W:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					Wasd.Enabled^=true;
					Text.ToolTip($"Walking Direction {(Wasd.Enabled?"en":"dis")}abled");
					ConfigProvider.SendUpdate(nameof(Features.Games)+"."+nameof(Wasd)+"."+nameof(Wasd.Enabled));
					return;
				case Keys.G:{
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;

					Text.CopyText(s=>{
						if(!(Uri.TryCreate(s,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https")) uri=new Uri("https://google.com/search?q="+Uri.EscapeDataString(s));
						Process.Start(new ProcessStartInfo(uri.ToString()){
							UseShellExecute=true,
						});
						//Process.Start(uri.ToString());
					});
					return;
				}/*
					case Keys.U:
						if(!Modifiers.Win||!Modifiers.Ctrl) return;
						e.Handled=true;
						Text.CopyText(s=>{
							Console.WriteLine("Getting Unicode for "+s);//console output is crap: print better \t \n...
							var repl=HotStringComplexUnicode.GetText(s);
							if(repl!=s) new Send().Text(repl).SendNow();
							else Console.WriteLine("Text is same as Unicode");
						});
						return;*/
				case Keys.Scroll:
					var shouldSpam=!Modifiers.IsScrollLock;
					if(Spammer.Enabled) Spammer.Running=shouldSpam;
					else if(shouldSpam) e.Handled=true;
					return;
				case Keys.T:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					Windows.SetAlwaysOnTop(Windows.GetCurrentWindow().Push(out var hwnd),!Windows.IsAlwaysOnTop(hwnd).Push(out var b));
					Text.ToolTip($"AlwaysOnTop {(b?"dis":"en")}abled");
					return;
				case Keys.C:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					var u=Windows.GetPixelColorUnderMouse();
					var colorString=(u.ToArgb()&0xFFFFFF).ToString("X6");
					if(Modifiers.Alt||Modifiers.Shift){
						Clipboard.SetText(colorString);
						Text.ToolTip("Copied Color: "+colorString);
					} else Text.ToolTip("Color: "+colorString);
					return;
				case Keys.B:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					Windows.SetBorderless(Windows.GetCurrentWindow(),null);
					return;
				case Keys.F:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					if(Modifiers.Alt) Windows.SetFullscreen(Windows.GetCurrentWindow(),null);
					else{
						Windows.SetFullscreen(Windows.GetCurrentWindow(),false);
						Windows.SetMaximized(Windows.GetCurrentWindow(),null);
					}
					return;
				case Keys.X:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					var window=Windows.GetCurrentWindow();
					var color=Windows.GetTransparentColor(window);
					var pixelColorUnderMouse=Windows.GetPixelColorUnderMouse();
					var x=new Windows.ColorRef(pixelColorUnderMouse);
					Windows.SetTransparentColor(window,color=color.HasValue?null:x);
					Text.ToolTip("TransColor: "+(color.HasValue?color.Value.GetRgb().ToString("x6"):"unset"));
					return;
				case Keys.H:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					var bb=Windows.SetClickThrough(Windows.GetCurrentWindow(),null);
					Text.ToolTip($"ClickThrough: {(bb?"en":"dis")}abled");
					return;
				case Keys.Pause:
					e.Handled=true;
					ReleaseKeys[Keys.Pause]=Keys.MediaPlayPause;
					new Send().Key(Keys.MediaPlayPause,true).SendNow();
					return;
				case Keys.M:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					Windows.HideWindow(Windows.GetCurrentWindow());
					return;
				case Keys.N:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					Windows.RestoreWindow();
					return;
				case Keys.K:
					if(!Modifiers.Win||!Modifiers.Ctrl) return;
					e.Handled=true;
					KeepDown=new HashSet<Keys>();
					return;
			}
		} catch(Exception ex){
			Console.WriteLine(ex);
		} finally{
			if(e.Handled&&!ReleaseKeys.ContainsKey(e.Key)) ReleaseKeys[e.Key]=Keys.None;
		}
	}


	public static void Up(object sender,KeyEvent e){
		if(KeepDown!=null&&KeepDown.Contains(e.Key)) KeepDown=null;

		if(e.Key==Keys.F1){
			MoveWindows.IsF1KeyDown=false;
			if(_repressWinOnF1.L||_repressWinOnF1.R){
				var send=new Send();

				if(_repressWinOnF1.L.SetCheck(false)) send.Key(Keys.LWin,true);
				if(_repressWinOnF1.R.SetCheck(false)) send.Key(Keys.RWin,true);

				send.Key(Keys.Escape)//Cancel Windows keys
				    .SendNow();
			}
		}

		if(ReleaseKeys.ContainsKey(e.Key)){
			var key=ReleaseKeys[e.Key];
			ReleaseKeys.Remove(e.Key);
			if(key==e.Key) return;
			e.Handled=true;
			if(key!=Keys.None) new Send().Key(key,false).SendNow();
		}
		//Windows.SendKey(key,false);
	}
}