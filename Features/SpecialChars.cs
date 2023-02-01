using System.Collections.Generic;
using System.Windows.Forms;
using KeyControl.Interfaces;
using KeyControl.Utilities;
using PlayifyUtils.Jsons;
using PlayifyUtils.Utils;

namespace KeyControl.Features;

public static class SpecialChars{
	public static readonly Dictionary<Keys,string> All=new();

	public static void InitConfig()=>Config.Register(nameof(SpecialChars),ToJson,j=>LoadJson(j.AsObject()));

	private static void LoadJson(JsonObject o){
		foreach(var (key,value) in o) All[SendBuilder.StringToKey(key)]=value.AsString();
	}

	private static JsonObject ToJson(){
		var o=new JsonObject();
		foreach(var (key,value) in All) o[SendBuilder.KeyToString(key)]=value;
		return o;
	}
}