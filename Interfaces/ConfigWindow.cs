using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KeyControl.Interfaces;

public sealed class ConfigWindow:Form{
	private static readonly bool DarkMode;


	public static ConfigWindow _instance;

	private readonly WebBrowser _browser;
	private bool _allowShowDisplay;

	private FormWindowState _prev=FormWindowState.Normal;

	static ConfigWindow(){
		try{
			var key=Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
			if(((int?) key?.GetValue("AppsUseLightTheme",1)??1)!=0) return;//if light mode, return
		} catch(Exception e){
			Console.WriteLine(e);
			return;//if can't get value, use light mode by default 
		}
		DarkMode=true;
	}

	public ConfigWindow(){
		Visible=false;
		const int size=740;
		MinimumSize=new Size(size,size*9/16);
		TopMost=true;
		Icon=new Icon(typeof(Program),"Resources.favicon.ico");
		Text=$"KeyControl ({Program.Version})";
		_instance=this;
		_browser=new WebBrowser{
			Dock=DockStyle.Fill,
			AllowWebBrowserDrop=true,
			IsWebBrowserContextMenuEnabled=false,
		};

		_browser.PreviewKeyDown+=(s,e)=>{
			switch(e.KeyCode){
				case Keys.F5:
					e.IsInputKey=true;
					return;
				case Keys.Delete:
				case Keys.Tab:


				case Keys.Escape:
				case Keys.Up:
				case Keys.Down:
				case Keys.Left:
				case Keys.Right:
					e.IsInputKey=false;
					return;
			}
			if(e.Control)
				switch(e.KeyCode){
					case Keys.A:
					case Keys.C:
					case Keys.V:
					case Keys.X:
					case Keys.Z:
					case Keys.Y:
					case Keys.Add:
					case Keys.Subtract:
					case Keys.Oemplus:
					case Keys.OemMinus:
						e.IsInputKey=false;
						return;
					default:{
						e.IsInputKey=true;
						return;
					}
				}
			e.IsInputKey=false;

		};
		_browser.DocumentCompleted+=(_,_)=>{
			var success=(bool?) _browser.Document?.InvokeScript("setDarkMode",new object[]{DarkMode});
			if(!success.GetValueOrDefault()) Console.WriteLine("Error setting Dark Mode for Website");
		};
		Controls.Add(_browser);

		_browser.ObjectForScripting=new External(_browser);
		//_browser.Url=new Uri("http://127.2.4.8:5000");
		var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KeyControl)}.Resources.combined.html")!;
		_browser.DocumentStream=stream;

		FormClosing+=(_,args)=>{
			var isShuttingDown=GetSystemMetrics(0x2000)!=0;//SM_SHUTTINGDOWN
			if(isShuttingDown) return;
			args.Cancel=true;
			Visible=false;
		};
	}

	[DllImport("DwmApi")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,int[] attrValue,int attrSize);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int nIndex);

	protected override void OnHandleCreated(EventArgs _){
		if(DarkMode)
			if(DwmSetWindowAttribute(Handle,19,new[]{1},4)!=0)//check if 19 works
				DwmSetWindowAttribute(Handle,20,new[]{1},4);//if not, then use 20
	}

	protected override void SetVisibleCore(bool value){
		var _=Handle;//load handle on startup, but dont show window
		base.SetVisibleCore(_allowShowDisplay&&value);
	}

	protected override void WndProc(ref Message m){
		if(Config.Constant.MinimizeToTray)
			if(m.Msg==0x0112)//WM_SYSCOMMAND
				if((m.WParam.ToInt32()&0xFFF0)==0xf020){//SC_MINIMIZE
					m.Result=IntPtr.Zero;
					Visible=false;
					return;
				}
		base.WndProc(ref m);
	}

	protected override void OnResize(EventArgs e){
		base.OnResize(e);
		if(WindowState==_prev) return;
		switch(WindowState){
			case FormWindowState.Normal:{
				TopMost=true;
				break;
			}
			case FormWindowState.Minimized:{
				_instance.WindowState=_instance._prev;
				Visible=false;
				return;
			}
			case FormWindowState.Maximized:{
				TopMost=false;
				break;
			}
			default:throw new ArgumentOutOfRangeException();
		}
		_prev=WindowState;
	}

	[DllImport("user32.dll")]
	[return:MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWindowVisible(IntPtr hWnd);

	protected override void OnKeyDown(KeyEventArgs e){
		if(e.KeyCode==Keys.Escape) Visible=false;
	}

	public static void Open()
		=>_instance.BeginInvoke((Action) (()=>{
				                                 var b=!IsWindowVisible(_instance.Handle);
				                                 if(b){
					                                 Windows.GetCursorPos(out var pos);
					                                 var screen=Screen.FromPoint(new Point(pos.x,pos.y));
					                                 var rect=screen.WorkingArea;
					                                 var size=_instance.Size;
					                                 var x=rect.X+rect.Width/2-size.Width/2;
					                                 var y=rect.Y+rect.Height/2-size.Height/2;

					                                 _instance._allowShowDisplay=true;
					                                 _instance.Location=new Point(x,y);
				                                 }
				                                 _instance.Visible=b;
				                                 if(b) _instance.Focus();
			                                 }));

	public static void CloseToTray()=>_instance.BeginInvoke((Action) (()=>{_instance.Visible=false;}));
}