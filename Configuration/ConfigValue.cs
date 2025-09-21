using PlayifyUtility.Jsons;

namespace KeyControl.Configuration;

public static class ConfigValue{
	public static ConfigValue<bool> CreateTemp(bool def,params string[] keys)=>new(def,j=>j.AsBool(),b=>b,false,keys);
	public static ConfigValue<bool> Create(bool def,params string[] keys)=>new(def,j=>j.AsBool(),b=>b,true,keys);
	public static ConfigValue<string> Create(string def,params string[] keys)=>new(def,j=>j.AsString(),b=>b,true,keys);
	public static ConfigValue<int> Create(int def,params string[] keys)=>new(def,j=>(int)j.AsDouble(),b=>b,true,keys);
	public static ConfigValue<uint> Create(uint def,params string[] keys)=>new(def,j=>(uint)j.AsDouble(),b=>b,true,keys);
}

public sealed class ConfigValue<T>{
	private bool _initialized;
	private T _value;
	private readonly Func<T,Json> _toJson;
	private readonly string[] _keys;

	public ConfigValue(T def,Func<Json,T> fromJson,Func<T,Json> toJson,bool file,params string[] keys){
		_toJson=toJson;
		_keys=keys;

		_value=def;

		Config.Listen(keys,json=>Value=json==null?def:fromJson(json),file);
		ConfigServer.OnConnect+=ws=>ws.Send(new JsonArray(keys){_toJson(Value)}.ToString());
	}

	public T Value{
		get=>_value;
		set{
			var equals=EqualityComparer<T>.Default.Equals(_value,value);
			if(equals&&_initialized) return;
			_value=value;
			var json=_toJson(value);
			if(!equals&&_initialized) Config.Merge(_keys,json);
			ConfigServer.Broadcast(new JsonArray(_keys){json}.ToString());
			OnChange?.Invoke(value);
			_initialized=true;
		}
	}


	public event Action<T>? OnChange;

	public ConfigValue<T> Listen(Action<T> onChange){
		OnChange+=onChange;
		return this;
	}
}