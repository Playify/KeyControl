using System;
using PlayifyUtils.Jsons;
using static System.StringComparison;

namespace KeyControl.HotString.Saveable;

public class HotStringReplace:HotStringBase{
	private readonly string _from;
	private readonly string _to;
	private readonly bool _ignoreCase;

	public HotStringReplace(JsonObject json):base(json){
		_from=(string) json.Get("From");
		if(string.IsNullOrEmpty(_from)) throw new ArgumentException("From can't be empty");
		_to=(string) json.Get("To");
		_ignoreCase=json.Get("IgnoreCase")?.AsBoolean()??false;
	}

	public HotStringReplace(string from,string to,bool ignoreCase=false):base(null){
		_from=from;
		_to=to;
		_ignoreCase=ignoreCase;
	}

	public override (int bs,string s)? Replace(string s)=>s.EndsWith(_from,_ignoreCase?OrdinalIgnoreCase:Ordinal)?(_from.Length,_to):null;

	public override (string from,string to) FromTo()=>(_from,_to);

	public override JsonObject ToJson(){
		var json=new JsonObject{
			{"From",_from},
			{"To",_to},
		};
		if(_ignoreCase) json.Put("IgnoreCase",true);
		if(_from.Length==_to.Length) json.Put("KeepCase",false);
		return json;
	}
}