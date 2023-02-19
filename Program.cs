using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsyncFriendlyStackTrace;
using KeyControl.Features.Games;
using KeyControl.Hooks;
using KeyControl.HotKeyHandler;
using KeyControl.Interfaces;
using KeyControl.Utilities;

namespace KeyControl;

public static class Program{
	public static GlobalKeyboardHook Keyboard;
	public static GlobalMouseHook Mouse;
	public static GlobalShellHook Shell;//TODO
	public static GlobalEventHook Event;

	private static ManualResetEvent _reset;
	private static SynchronizationContext _ctx;
	private static readonly Regex FixPath=new(@"[\\/:*?""<|>]");

	public static string Version{
		get{
			var version=Assembly.GetExecutingAssembly().GetName().Version;
			return version.ToString(version.Build!=0?3:2);
		}
	}

	public static async Task Main(string[] args){
		try{
			var self=Process.GetCurrentProcess();
			foreach(var process in Process.GetProcessesByName(self.ProcessName))
				if(process.Id!=self.Id){
					process.Kill();
					process.WaitForExit(100);
				}

			Console.OutputEncoding=Encoding.Unicode;

			AppDomain.CurrentDomain.UnhandledException+=(_,e)=>{
				var s="Uncaught exception:\n"+
				      $"{nameof(e.IsTerminating)}: {e.IsTerminating}\n"+
				      Convert.ToString(e.ExceptionObject);
				Exit(s);
			};


			var hwnd=Windows.GetConsoleWindow();
			if(hwnd!=IntPtr.Zero)
				if(Windows.GetProcess(hwnd).Id==Process.GetCurrentProcess().Id)
					Windows.HideWindow(hwnd);

			Logger.Init();
			await Config.Load();
			var __=ConfigProvider.Instance;//force load ConfigProvider

			//Init Message Loop
			_reset=new ManualResetEvent(false);
			var thread=new Thread(Init){IsBackground=true};
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			_reset.WaitOne();

			if(hwnd==IntPtr.Zero){
				new AutoResetEvent(false).WaitOne();
				return;//this return is never reached
			}


			while(true){
				var s=Console.ReadLine();
				if(s==null) break;
				ConfigWindow.Open();
				Console.WriteLine(s);
			}
		} catch(Exception e){
			Exit("Error in Main:\n"+e.ToAsyncString(),1);
		}
	}

	private static void Init(){
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Idle+=Initialize;
		TrayIcon.Create().Visible=true;

		CrossHair.InitForms();

		Application.Run(new ConfigWindow());
	}

	public static void BeginInvoke(Action a){
		if(_ctx==null) a();
		else _ctx.Post(_=>a(),null);
	}

	public static T Invoke<T>(Func<T> func){
		T result=default;
		_ctx.Send(_=>result=func(),null);
		return result;
	}

	private static void Initialize(object sender,EventArgs e){
		_ctx=SynchronizationContext.Current;
		Application.Idle-=Initialize;
		_reset.Set();

		if(Config.Constant.EnableHook){
			if(Modifiers.IsCapsLock) new Send().Hide().Key(Keys.CapsLock).SendNow();
			if(Modifiers.IsScrollLock) new Send().Hide().Key(Keys.Scroll).SendNow();
			if(!Modifiers.IsNumLock) new Send().Hide().Key(Keys.NumLock).SendNow();

			Shell=new GlobalShellHook();

			//Event=new GlobalEventHook();


			Keyboard=new GlobalKeyboardHook(Config.Constant.EnableHook);
			Keyboard.KeyDown+=KeyboardHandler.Down;
			Keyboard.KeyUp+=KeyboardHandler.Up;

			Mouse=new GlobalMouseHook(Config.Constant.EnableHook);
			Mouse.MouseMove+=MouseHandler.Move;
			Mouse.MouseScroll+=MouseHandler.Scroll;
			Mouse.KeyDown+=MouseHandler.Down;
			Mouse.KeyUp+=MouseHandler.Up;

			Keyboard.KeyDown+=HotStringHandler.Down;
			Keyboard.KeyUp+=HotStringHandler.Up;
			Mouse.KeyDown+=HotStringHandler.Reset;
			Mouse.KeyUp+=HotStringHandler.Reset;

			Mouse.MouseMove+=Text.CorrectToolTip;
		} else Console.WriteLine("Not Hooking");
	}

	public static void Log(string s){
		var lines=new[]{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss.ffff")+" "+s};
		File.AppendAllLines("./log_"+Environment.MachineName+".txt",lines,Encoding.UTF8);
	}

	public static void Exit(string reason,int exitCode=0){
		try{
			var logPath=Path.GetDirectoryName(Config.ConfigPath)??".";
			Directory.CreateDirectory(logPath);

			logPath=Path.Combine(logPath,"Logs");
			Directory.CreateDirectory(logPath);

			var machineName=FixPath.Replace(Environment.MachineName,"#");
			var path=Path.Combine(logPath,$"{DateTime.Now:yyyy-MM-dd hhmmss}@{machineName}.txt");
			File.WriteAllText(path,reason);
			Console.Error.WriteLine(reason);

		} catch(Exception e){
			Console.WriteLine(e.ToAsyncString());
		} finally{
			Environment.Exit(exitCode);
		}
	}
}