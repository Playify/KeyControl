using System.Diagnostics;
using System.Runtime.InteropServices;
using KeyControl.Configuration;
using KeyControl.Util;
using Microsoft.Win32;

namespace KeyControl.Features;

[InitOnLoad]
public static partial class KeyControlSettings{
	private static readonly ConfigValue<bool> RunOnBoot=InitRunOnBoot();
	public static readonly ConfigValue<bool> Install=InitInstall();
	private const string AutorunKey=@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
	private const string UninstallKey=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\KeyControl";
	
	private static string OwnExe=>Process.GetCurrentProcess().MainModule?.FileName
	                              ??Path.Combine(AppContext.BaseDirectory,"KeyControl.exe");

	private static string AppDataDir=>Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"KeyControl");

	private static ConfigValue<bool> InitRunOnBoot(){
		const string runAppName="KeyControl";
		var appCommand=$"\"{OwnExe}\" --autostart";

		var run=Registry.CurrentUser.OpenSubKey(AutorunKey,true)
		        ??Registry.CurrentUser.CreateSubKey(AutorunKey,true);

		var current=run.GetValue(runAppName);
		var isCorrect=string.Equals(current?.ToString(),appCommand,StringComparison.OrdinalIgnoreCase);

		// If registry contains something else, correct it immediately
		if(current!=null&&!isCorrect){
			run.SetValue(runAppName,appCommand);
			isCorrect=true;
		}

		return ConfigValue.CreateTemp(
			isCorrect,
			"KeyControl","RunOnBoot"
		).Listen(b=>{
			if(b) run.SetValue(runAppName,appCommand);
			else run.DeleteValue(runAppName,false);
		});
	}

	private static ConfigValue<bool> InitInstall(){
		var installedPath=Path.Combine(AppDataDir,"KeyControl.exe");

		var args=Environment.GetCommandLineArgs();

		// Case: called with --installed-by → cleanup old exe
		var idx=Array.IndexOf(args,"--installed-by");
		if(idx>=0&&idx+1<args.Length){
			try{
				var oldExe=args[idx+1];
				if(File.Exists(oldExe))
					File.Delete(oldExe);
			} catch{/* ignore, best-effort cleanup */
			}
		}

		var isInstalled=string.Equals(OwnExe,installedPath,StringComparison.OrdinalIgnoreCase);

		return ConfigValue.CreateTemp(
			isInstalled,
			"KeyControl","Install"
		).Listen(installed=>{
			if(!installed||isInstalled) return;
			InstallSelf(true);
		});
	}

	private static void CreateUninstallEntry(string installedPath){
		using var key=Registry.CurrentUser.CreateSubKey(UninstallKey,true);

		key.SetValue("DisplayName","KeyControl");
		key.SetValue("Publisher","Playify");
		key.SetValue("DisplayVersion",Config.VersionNumber);
		key.SetValue("InstallDate",DateTime.Now.ToString("yyyyMMdd"));
		key.SetValue("InstallLocation",Path.GetDirectoryName(installedPath)??installedPath);
		key.SetValue("HelpLink", "https://github.com/Playify/KeyControl");
		key.SetValue("URLInfoAbout", "https://github.com/Playify/KeyControl");
		key.SetValue("URLUpdateInfo", "https://github.com/Playify/KeyControl/releases");
		key.SetValue("UninstallString",$"\"{installedPath}\" --uninstall");
		key.SetValue("DisplayIcon",installedPath);
		key.SetValue("NoModify",1,RegistryValueKind.DWord);
		key.SetValue("NoRepair",1,RegistryValueKind.DWord);
	}

	public static void InstallSelf(bool openConfig){
		RunOnBoot.Value=true;
		var installedPath=Path.Combine(AppDataDir,"KeyControl.exe");

		// Copy exe
		Directory.CreateDirectory(AppDataDir);
		if(File.Exists(installedPath)) File.Delete(installedPath);
		File.Copy(OwnExe,installedPath,true);

		// Create Start Menu shortcut
		var startMenu=Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
		var shortcutPath=Path.Combine(startMenu,"Programs","KeyControl.lnk");
		if(!File.Exists(shortcutPath)) CreateShortcut(shortcutPath,installedPath);

		//Register in Windows Settings Apps & Features
		CreateUninstallEntry(installedPath);

		// Launch new exe and exit
		var info=new ProcessStartInfo{
			FileName=installedPath,
			ArgumentList={
				"--installed-by",
				OwnExe,
			},
			UseShellExecute=false,
		};
		if(openConfig)
			info.ArgumentList.Add("--open-config");//Only open config again, when installed using GUI
		Process.Start(info);
		Environment.Exit(0);
	}

	public static void UninstallSelf(){
		RunOnBoot.Value=false;

		// Remove Start Menu shortcut
		var startMenu=Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
		var shortcutPath=Path.Combine(startMenu,"Programs","KeyControl.lnk");
		if(File.Exists(shortcutPath)) File.Delete(shortcutPath);

		// Remove uninstall registry entry
		Registry.CurrentUser.DeleteSubKeyTree(UninstallKey,false);

		//Remove own exe file
		if(File.Exists(OwnExe)){
			Process.Start(new ProcessStartInfo{
				FileName="cmd.exe",
				Arguments=$"/C timeout /t 1 /nobreak >nul & del \"{OwnExe}\"",
				CreateNoWindow=true,
				UseShellExecute=false,
			});
		}
	}
}