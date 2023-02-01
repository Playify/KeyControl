using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KeyControl.Interfaces;
using KeyControl.Utilities;

namespace KeyControl.Features.Games;

public sealed class CrossHair:Form{
	private static bool _enabled;
	private static bool _autoEnableInCsGo;
	private static Color _color=Color.Lime;
	private static int _lineGap=10;
	private static int _lineLength=20;
	private static int _lineWidth=2;
	private static bool _centerDot=true;

	private static bool _visible;

	private static CrossHair[] _forms;

	private readonly bool? _ud,_lr;

	private bool _allowShowDisplay;

	private CrossHair(bool? ud,bool? lr){
		_ud=ud;
		_lr=lr;

		MinimumSize=Size.Empty;
		FormBorderStyle=FormBorderStyle.None;
		StartPosition=FormStartPosition.Manual;


		BackColor=Color.FromArgb(255,LineColor);
		ForeColor=Color.FromArgb(255,LineColor);
		//Opacity=LineColor.A;
	}

	public new static bool Enabled{
		get=>_enabled;
		set{
			_enabled=value;
			Scheduler.RunOnMainThread(UpdateAll);
		}
	}
	public static bool AutoEnableInCsGo{
		get=>_autoEnableInCsGo;
		set{
			_autoEnableInCsGo=value;
			Scheduler.RunOnMainThread(UpdateAll);
		}
	}
	public static Color LineColor{
		get=>_color;
		set{
			_color=value;
			Scheduler.RunOnMainThread(()=>{
				if(_forms==null) return;
				foreach(var form in _forms){
					form.BackColor=value;
					form.ForeColor=value;
					form.Opacity=value.A;
					form.Invalidate();
				}
			});
		}
	}
	public static int LineGap{
		get=>_lineGap;
		set{
			_lineGap=value;
			Scheduler.RunOnMainThread(UpdateAll);
		}
	}
	public static int LineLength{
		get=>_lineLength;
		set{
			_lineLength=value;
			Scheduler.RunOnMainThread(UpdateAll);
		}
	}
	public static int LineWidth{
		get=>_lineWidth;
		set{
			_lineWidth=value;
			Scheduler.RunOnMainThread(UpdateAll);
		}
	}
	public static bool CenterDot{
		get=>_centerDot;
		set{
			_centerDot=value;
			Scheduler.RunOnMainThread(UpdateAll);
		}
	}

	protected override bool ShowWithoutActivation=>true;

	protected override CreateParams CreateParams{
		get{
			var createParams=base.CreateParams;
			createParams.ExStyle|=0x8000000;//NoActivate
			createParams.ExStyle|=0x80;//ToolWindow
			createParams.ExStyle|=0x8;//TopMost
			return createParams;
		}
	}


	public static void InitConfig(){
		Config.Register(nameof(Games)+"."+nameof(CrossHair)+"."+nameof(Enabled),()=>Enabled,j=>Enabled=j.AsBoolean(),Config.Restrict.Website);
		Config.Register(nameof(Games)+"."+nameof(CrossHair)+"."+nameof(AutoEnableInCsGo),()=>AutoEnableInCsGo,j=>AutoEnableInCsGo=j.AsBoolean());
		Config.Register(nameof(Games)+"."+nameof(CrossHair)+"."+nameof(LineColor),()=>{
			                var u=(uint) LineColor.ToArgb();
			                if((u&0xFF000000)!=0xFF000000) return u.ToString("X8");
			                u&=0xFFFFFF;
			                return u.ToString("X6");
		                },
		                j=>{
			                var u=uint.Parse(j.AsString(),NumberStyles.HexNumber);
			                if((u&0xFF000000)==0) u|=0xFF000000;//If Alpha is 0, set Alpha to 255
			                LineColor=Color.FromArgb((int) u);
		                });
		Config.Register(nameof(Games)+"."+nameof(CrossHair)+"."+nameof(LineGap),()=>LineGap,j=>LineGap=(int) j.AsNumber());
		Config.Register(nameof(Games)+"."+nameof(CrossHair)+"."+nameof(LineLength),()=>LineLength,j=>LineLength=(int) j.AsNumber());
		Config.Register(nameof(Games)+"."+nameof(CrossHair)+"."+nameof(LineWidth),()=>LineWidth,j=>LineWidth=(int) j.AsNumber());
		Config.Register(nameof(Games)+"."+nameof(CrossHair)+"."+nameof(CenterDot),()=>CenterDot,j=>CenterDot=j.AsBoolean());
	}

	public static void UpdateAll(){
		if(_forms==null) return;

		var window=Windows.GetForegroundWindow();

		if(IsHwndPartOfCrossHair(window)) return;
		if(ConfigWindow._instance?.Handle==window) return;

		var shouldBeVisible=Enabled;
		if(!shouldBeVisible&&AutoEnableInCsGo)//Handle CSGO
			shouldBeVisible="csgo.exe".Equals(Path.GetFileName(Windows.GetExe(window)),StringComparison.OrdinalIgnoreCase);

		if(!_visible&&!shouldBeVisible){
			EventHook.Unhook();
			return;//even if both are visible, update position, could be a new window to focus on
		}

		_visible=shouldBeVisible;
		if(shouldBeVisible){
			Windows.GetWindowRect(window,out var rect);
			var x=(rect.Left+rect.Right)/2;
			var y=(rect.Top+rect.Bottom)/2;
			foreach(var form in _forms) form.UpdateSingle(x,y);
			EventHook.Hook();
		} else{
			foreach(var form in _forms) form.Hide();
			EventHook.Unhook();
		}
	}

	public static void InitForms(){
		_forms=new CrossHair[]{new(true,null),new(false,null),new(null,true),new(null,false),new(null,null)};
		UpdateAll();
	}

	protected override void SetVisibleCore(bool value){
		var _=Handle;//load handle on startup, but don't show window
		value&=_allowShowDisplay;
		base.SetVisibleCore(value);
		if(value) Windows.SetClickThrough(Handle,true);
	}

	private void UpdateSingle(int x,int y){

		var length=LineLength;
		var width=LineWidth;

		Visible=true;
		if(length<2) length=2;
		if(width<2) width=2;

		var bounds=new Rectangle();
		switch(_ud){
			case true:
				bounds.Y=y-LineGap-length-width/2;
				bounds.Height=length;
				break;
			case false:
				bounds.Y=y+LineGap+width/2;
				bounds.Height=length;
				break;
			case null:
				bounds.Y=y-width/2;
				bounds.Height=width;
				break;
		}
		switch(_lr){
			case true:
				bounds.X=x-LineGap-length-width/2;
				bounds.Width=length;
				break;
			case false:
				bounds.X=x+LineGap+width/2;
				bounds.Width=length;
				break;
			case null:
				bounds.X=x-width/2;
				bounds.Width=width;
				break;
		}

		if(bounds.Width==0||bounds.Height==0){
			Visible=false;
			return;
		}


		if(Bounds!=bounds) Bounds=bounds;

		_allowShowDisplay=true;
		if(!_lr.HasValue&&!_ud.HasValue) Visible=CenterDot;
		else Visible=true;

		//Windows.SetWindowPos(Handle,IntPtr.Zero,bounds.X,bounds.Y,bounds.Width,bounds.Height,0x10|0x4);//NoActivate|NoZorder
	}

	public static bool IsHwndPartOfCrossHair(IntPtr hwnd)=>_forms!=null&&_forms.Any(form=>form.Handle==hwnd);

	public static class EventHook{
		private static IntPtr _hook;
		private static WinEventProc _proc;


		[DllImport("user32.dll")]
		private static extern IntPtr SetWinEventHook(uint eventMin,uint eventMax,IntPtr hmodWinEventProc,WinEventProc lpfnWinEventProc,int idProcess,int idThread,uint dwflags);

		[DllImport("user32.dll")]
		private static extern int UnhookWinEvent(IntPtr hWinEventHook);

		public static void Hook(){
			if(_proc!=null) return;
			_proc=HookProc;
			_hook=SetWinEventHook(0x800b,0x800b,IntPtr.Zero,_proc,0,0,2);
		}

		public static void Unhook(){
			if(_proc==null) return;
			UnhookWinEvent(_hook);
			_proc=null;
			_hook=IntPtr.Zero;
		}


		private static void HookProc(IntPtr hwineventhook,int ievent,IntPtr hwnd,int idobject,int idchild,int dweventthread,int dwmseventtime){
			if(ievent!=0x800b) return;
			if(hwnd==IntPtr.Zero) return;

			Scheduler.RunLater(0,()=>{
				if(Enabled&&!IsHwndPartOfCrossHair(hwnd)) UpdateAll();
			},true);
			//if(Enabled&&!IsHwndPartOfCrossHair(hwnd)) UpdateAll();
		}

		private delegate void WinEventProc(IntPtr hWinEventHook,int iEvent,IntPtr hWnd,int idObject,int idChild,int dwEventThread,int dwmsEventTime);
	}
}