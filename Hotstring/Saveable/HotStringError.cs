using System;
using PlayifyUtils.Jsons;

namespace KeyControl.HotString.Saveable;

public class HotStringError:HotStringBase{
	private readonly JsonObject _json;
	private readonly Exception _exception;

	public HotStringError(JsonObject json,Exception exception):base(json){
		_json=(JsonObject) json.DeepCopy();
		_exception=exception;
		_json.Put("Error",_exception.ToString());
	}

	public override (int bs,string s)? Replace(string s)=>null;

	public override (string from,string to) FromTo()=>("ERROR",_exception.GetType().Name+":"+_exception.Message);

	public override JsonObject ToJson()=>_json;
}