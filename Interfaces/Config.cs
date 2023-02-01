using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KeyControl.Features;
using KeyControl.Features.Games;
using KeyControl.HotString;
using KeyControl.HotString.Complex;
using KeyControl.HotString.Saveable;
using PlayifyUtils.Jsons;
using PlayifyUtils.Utils;

namespace KeyControl.Interfaces;

public class Config{
	[Flags]
	public enum Restrict{
		None=0,
		File=1,
		Website=2,
		All=3,
	}

	public static readonly Dictionary<string,(Func<Json>,Action<Json>)> ValuesFile=new();
	public static readonly Dictionary<string,(Func<Json>,Action<Json>)> ValuesWebsite=new();


	#region Hotkeys
	public static int TransparencySpeed=15;// Step size for Transparency (values 1,3,5,15,17,51,85,255 work best)
	#endregion

	private static string _path;

	private static readonly object SaveLock=new();

	static Config(){
		//Hotkeys
		Register(nameof(TransparencySpeed),()=>new JsonNumber(TransparencySpeed),j=>TransparencySpeed=(int) j.AsNumber());

		CapsLock.InitConfig();

		//HotStrings
		Register(nameof(HotStrings),()=>HotStringCategory.Master.ToJsonArray(false),j=>HotStringCategory.Master.LoadJson(j.AsArray()),Restrict.File);
		Register(nameof(EmojiTimeout),()=>EmojiTimeout,j=>EmojiTimeout=(long) j.AsNumber());

		SpecialChars.InitConfig();
		//NamingHelper.InitConfig();
		Spammer.InitConfig();
		CrossHair.InitConfig();
		Wasd.InitConfig();
	}

	public static string ConfigPath{
		get{
			if(_path!=null) return _path;
			var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"KeyControl");
			if(!Directory.Exists(path)) Directory.CreateDirectory(path);
			return _path=Path.Combine(path,"KeyControl.json");
		}
		set{
			if(value.EndsWith(".json",StringComparison.OrdinalIgnoreCase)||File.Exists(value)) _path=value;
			else _path=Path.Combine(value,"KeyControl.json");
		}
	}

	public static void Register(string key,Func<Json> get,Action<Json> set,Restrict restrict=Restrict.All){
		if((restrict&Restrict.File)!=0) ValuesFile[key]=(get,set);
		if((restrict&Restrict.Website)!=0) ValuesWebsite[key]=(get,set);
	}

	private static async ValueTask<JsonObject> LoadDefault()=>JsonObject.Parse(await ConfigProvider.GetWebFile("/defaultConfig.json"));

	public static async Task Load(){
		JsonObject json=null;
		try{
			var path=ConfigPath;
			if(File.Exists(path)) json=JsonObject.Parse(File.ReadAllText(path));
			else{
				var oldConfig=Path.Combine(Environment.GetEnvironmentVariable("APPDATA")??".","KeyControl.json");
				if(File.Exists(oldConfig)){
					File.Move(oldConfig,path);
					json=JsonObject.Parse(File.ReadAllText(path));
				} else{
					Console.WriteLine("Error reading Config File, \""+path+"\" does not exist => using default config");
					json=new JsonObject();
				}
			}
		} catch(Exception e){
			Console.WriteLine(e);
		}
		if(json==null){
			Console.WriteLine("Error parsing Config File => using default values");
			return;
		}
		JsonObject @default=null;

		foreach(var (key,(_,set)) in ValuesFile){
			var value=json.Get(key)??(@default??=await LoadDefault()).Get(key);
			if(value!=null) set(value);
		}
	}

	public static void Save(){
		lock(SaveLock){
			var o=new JsonObject();
			foreach(var (key,tuple) in ValuesFile){
				var value=tuple.Item1();
				if(value!=null) o[key]=value;
			}
			Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)??throw new NullReferenceException());
			File.WriteAllText(ConfigPath,o.ToString(4));
		}
	}

	public static class Constant{
		public static bool MinimizeToTray=true;//Not yet decided
		public static bool EnableLogging=false;//Never worked that good. HTML Files are bad for large files. Never really used it


		private static bool _hooking=true;
		public static bool EnableHook{
			get=>_hooking;
			set{
				_hooking=value;
				if(value){
					Program.Keyboard?.Hook();
					Program.Mouse?.Hook();
				} else{
					Program.Keyboard?.Unhook();
					Program.Mouse?.Unhook();
				}
			}
		}
	}

	#region HotStrings
	public static readonly List<HotStringUnSaveable> HotStrings=new(){
		//Complex
		new HotStringComplexCalc(),
		new HotStringComplexFlip(),
		new HotStringComplexUnicode(),
		new HotStringComplexNumberConverter(),

		HotStringCategory.Master,
	};

	public static long EmojiTimeout=1500;
	#endregion
}