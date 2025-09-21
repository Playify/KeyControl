using System.Diagnostics;
using System.Reflection;
using PlayifyUtility.Jsons;
using PlayifyUtility.Utils.Extensions;

namespace KeyControl.Configuration;

public static class Config{
	private static readonly string ConfigPath;
	private static readonly object FileLock=new();
	private static JsonObject? _root;
	private static readonly Dictionary<string[],(Action<Json?> action,bool file)> Listeners=new();

	private static readonly Version Version=Assembly.GetExecutingAssembly().GetName().Version!;
	public static readonly string VersionNumber=Version.ToString(Version.Build!=0?3:2);
	public static readonly string VersionString="KeyControl "+VersionNumber;

	static Config(){
		if(File.Exists("config.json")) ConfigPath="config.json";
		else{
			var configDirectory=Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"KeyControl");
			Directory.CreateDirectory(configDirectory);
			ConfigPath=Path.Combine(configDirectory,"config.json");
		}
	}

	public static void Load(){
		lock(FileLock)
			if(!File.Exists(ConfigPath)){
				Console.WriteLine("Error reading config file => using default values");
				_root=new JsonObject();
			} else if(!JsonObject.TryParse(File.ReadAllText(ConfigPath),out var json)){
				Console.WriteLine("Error parsing config file => using default values");
				_root=new JsonObject();
			} else _root=json;

		foreach(var (key,(action,file)) in Listeners)
			if(file)
				action(key.Aggregate((Json?)_root,(j,s)=>j?.Get(s)));
	}


	private static void Save(){
		if(!_root.NotNull(out var root)) return;
		var copy=(JsonObject)root.DeepCopy();
		lock(FileLock) File.WriteAllText(ConfigPath,copy.ToPrettyString());
	}


	public static void Update(string[] keys,Json value){
		foreach(var (key,(action,_)) in Listeners)
			if(key.SequenceEqual(keys)){
				action(value);
				return;
			}
		throw new Exception("Value doesn't exist: "+keys.Join("."));
	}

	public static void Merge(string[] keys,Json value){
		var json=_root??=new JsonObject();
		foreach(var key in keys.SkipLast(1))
			/*if(json.TryGet(key,out var child))
				json=child as JsonObject??throw new Exception("Can't access value, as sub-key is already another value");*/
			if(json.TryGet(key,out var child)&&child is JsonObject childObj)
				json=childObj;
			else{
				var obj=new JsonObject();
				json[key]=obj;
				json=obj;
			}
		json[keys.Last()]=value;

		Save();
	}


	public static void Listen(string[] keys,Action<Json?> action,bool file){
		Debug.WriteLine("Listening to "+keys.Join("."));
		Listeners[keys]=(action,file);
	}
}