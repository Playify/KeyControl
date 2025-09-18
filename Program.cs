using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Utils;

SingleInstance.ByName("KeyControl by Playify");
InitOnLoadAttribute.LoadAssembly();


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