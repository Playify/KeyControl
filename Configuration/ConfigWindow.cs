using KeyControl.Features;
using KeyControl.Util;
using PlayifyUtility.Windows;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Configuration;

[InitOnLoad]
public sealed partial class ConfigWindow:Form{
	private static readonly Icon ProgramIcon/*=Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)!*/;
	private static ConfigWindow? _instance;
	private static ConfigWindow Instance=>_instance??=UiThread.Create(nameof(ConfigWindow)).Invoke(()=>new ConfigWindow());

	static ConfigWindow(){
		using (var stream=ConfigServer.OpenFile("/favicon.ico")) ProgramIcon=new Icon(stream!);


		var strip=new ContextMenuStrip{
			Items={
				{"Settings",null,(_,_)=>ToggleOpen()},
				{"&Unhide all",null,(_,_)=>WinWindow.RestoreAll()},
				{"&Exit and unhide all",null,(_,_)=>Environment.Exit(0)},
			},
		};
		ToolStripItem pause=null!;
		pause=strip.Items.Add("Pause",null,(_,_)=>{
			var paused=Utils.Paused^=true;
			// ReSharper disable once AccessToModifiedClosure
			pause.Text=paused?"Resume":"Pause";
		});

		//Only show quick install option, if not running in portable mode. For now, install is still available in menu, but it isn't in the way there
		if(!KeyControlSettings.Install.Value&&!File.Exists("config.json"))
			strip.Items.Add("Install",null,(_,_)=>KeyControlSettings.InstallSelf(false));


		var notifyIcon=new NotifyIcon{
			Icon=ProgramIcon,
			Text=Config.VersionString,
			ContextMenuStrip=strip,
		};
		var first=notifyIcon.ContextMenuStrip.Items[0];
		first.Font=new Font(first.Font,first.Font.Style|FontStyle.Bold);

		notifyIcon.Click+=(_,e)=>{
			if(e is MouseEventArgs m&&(m.Button&MouseButtons.Left)==0) return;//don't do anything on right mouse button
			ToggleOpen();
		};

		notifyIcon.Visible=true;
		AppDomain.CurrentDomain.ProcessExit+=(_,_)=>notifyIcon.Visible=false;
	}


	private FormWindowState _prevWindowState;
	private readonly WebBrowser _browser;

	public ConfigWindow(){
		const int size=740;
		MinimumSize=new Size(size,size*9/16);
		TopMost=true;
		DoubleBuffered=true;
		Icon=ProgramIcon;
		Text=Config.VersionString;

		_browser=new WebBrowser{
			Dock=DockStyle.Fill,
			IsWebBrowserContextMenuEnabled=true,
			ObjectForScripting=new ConfigScriptingObject(),
		};
		_browser.PreviewKeyDown+=BrowserPreviewKeyDown;
		_browser.DocumentCompleted+=BrowserDocumentCompleted;
		_browser.Url=new Uri("http://127.2.4.8:5001/");
		Controls.Add(_browser);

		_darkMode=WinSystem.DarkMode;
		new WinWindow(Handle).SetDarkMode(_darkMode);
	}

	protected override bool ShowWithoutActivation=>true;

	protected override void OnFormClosing(FormClosingEventArgs e){
		if(!WinSystem.IsSystemShuttingDown){
			e.Cancel=true;
			Visible=false;
		}
		base.OnFormClosing(e);
	}

	//Minimize to tray
	protected override void WndProc(ref Message m){
		if(m.Msg==0x112&&(WinWindow.SysCommand)(m.LParam.ToInt32()&0xFFF0)==WinWindow.SysCommand.Minimize){//Wm_SysCommand
			m.Result=IntPtr.Zero;
			Visible=false;
			return;
		}
		base.WndProc(ref m);
	}


	//Only TopMost if not maximized
	protected override void OnResize(EventArgs e){
		base.OnResize(e);
		if(WindowState==_prevWindowState) return;
		switch(WindowState){
			case FormWindowState.Normal:{
				TopMost=true;
				break;
			}
			case FormWindowState.Minimized:{
				WindowState=_prevWindowState;
				Visible=false;
				return;
			}
			case FormWindowState.Maximized:{
				TopMost=false;
				break;
			}
			default:throw new ArgumentOutOfRangeException();
		}
		_prevWindowState=WindowState;
	}

	protected override void OnKeyDown(KeyEventArgs e){
		if(e.KeyCode==Keys.Escape){
			Console.WriteLine("Escape");
			Visible=false;
			e.Handled=true;
		}
		base.OnKeyDown(e);
	}


	private void _ToggleOpen(){
		if(Visible){
			if(WinWindow.Foreground.Hwnd==Handle){
				new Send().Combo(PlayifyUtility.Windows.Features.Interact.ModifierKeys.Alt,Keys.Escape).SendNow();
				if(Modifiers.Win) KeyEvent.CancelWindowsKey();//Pressing Alt+Escape represses windows key
			}

			Visible=false;
			return;
		}

		var rect=Screen.FromPoint(WinCursor.CursorPos).WorkingArea;
		var size=Size;


		Location=new Point(
			rect.X+(rect.Width-size.Width)/2,
			rect.Y+(rect.Height-size.Height)/2
		);
		Visible=true;
		Focus();
		new WinWindow(Handle).SetForeground();

		if(WindowState==FormWindowState.Normal)
			TopMost=true;
	}

	public static void ToggleOpen()=>Instance.Invoke(()=>Instance._ToggleOpen());
	public static void Initialize()=>_=Instance;
}