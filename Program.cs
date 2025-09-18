using System.Diagnostics;
using KeyControl.Configuration;
using KeyControl.Util;

using var curr=Process.GetCurrentProcess();
foreach(var process in Process.GetProcessesByName(curr.ProcessName)){
	if(process.Id!=curr.Id) process.Kill();
	process.Dispose();
}

//*
InitOnLoadAttribute.LoadAssembly();
/*/
typeof(HotStringSaveAble).RunClassConstructor();
Console.WriteLine("Only loading single class!");
//*/


//GlobalMouseHook.Paused=GlobalKeyboardHook.Paused=true;//*/


Config.Load();
ConfigServer.Run();//Only allowed to start after config loaded fully
ConfigWindow.Initialize();

Console.WriteLine("Started");


/*
new Thread(()=>{
	while(true){
		switch(Console.ReadKey(true).Key){
			case ConsoleKey.X:{
				ConfigWindow.ToggleOpen();
				continue;
			}
		}
	}
	// ReSharper disable once FunctionNeverReturns
}){
	Name = "Console",
}.Start();*/


Application.Run();