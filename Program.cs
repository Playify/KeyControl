using System.Diagnostics;
using KeyControl.Configuration;
using KeyControl.Features;
using KeyControl.Util;
using PlayifyUtility.Utils;

SingleInstance.ByName("KeyControl by Playify");


if(args.Contains("--uninstall")){
	if(MessageBox.Show("Uninstall KeyControl?",Config.VersionString,
		   MessageBoxButtons.YesNo,MessageBoxIcon.Question,
		   MessageBoxDefaultButton.Button2,MessageBoxOptions.DefaultDesktopOnly)==DialogResult.Yes)
		KeyControlSettings.UninstallSelf();
	Environment.Exit(0);
	return;
}
if(args.Contains("--exit")){
	Environment.Exit(0);
	return;
}
if(args.Contains("--install")){
	KeyControlSettings.InstallSelf(false);
	Environment.Exit(0);
	return;
}

Process.GetCurrentProcess().PriorityClass=ProcessPriorityClass.AboveNormal;

InitOnLoadAttribute.LoadAssembly();


if(args.Contains("--paused"))
	Utils.Paused=true;


Config.Load();
ConfigServer.Run();//Only allowed to start after config loaded fully
ConfigWindow.Initialize();

if(args.Contains("--open-config"))
	ConfigWindow.ToggleOpen();

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