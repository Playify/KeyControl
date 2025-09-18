using KeyControl.Configuration;
using KeyControl.Features.Strings.HotStrings.SaveAble;
using PlayifyUtility.Jsons;

namespace KeyControl.Features.Strings.HotStrings;

public abstract class HotStringSaveAble:HotStringInternal{
	private static int _nextId;
	public readonly uint Id;
	public readonly bool Enabled;
	public readonly bool Collapsed;

	private static readonly Dictionary<uint,HotStringSaveAble> IdMap=new();


	protected HotStringSaveAble(JsonObject? json){
		Id=(uint?)json?.Get("Id")?.AsDouble()??(uint)Interlocked.Increment(ref _nextId);
		IdMap[Id]=this;
		Enabled=json?.Get("Enabled")?.AsBool()??true;
		Collapsed=json?.Get("Collapsed")?.AsBool()??true;
	}

	public abstract JsonObject ToJson();
	public virtual JsonObject ToJson(bool useId)=>ToJson();

	public JsonObject ToFullJson(bool useId){
		var o=ToJson(useId);
		if(!Enabled) o["Enabled"]=false;
		if(!Collapsed) o["Collapsed"]=false;
		if(useId) o["Id"]=Id;
		return o;
	}

	public static HotStringSaveAble FromJson(Json json){
		if(json is JsonObject o) return FromJson(o);
		lock(IdMap)
			if(IdMap.TryGetValue((uint)json.AsDouble(),out var hs))
				return hs;
		throw new ArgumentOutOfRangeException();
	}

	public static HotStringSaveAble FromJson(JsonObject json){
		try{
			if(json.Has("Category")) return new HotStringCategory(json);
			if(json.Has("Emoji")) return new HotStringEmoji(json);
			if(json.Has("Regex")) return new HotStringRegex(json);
			var from=json.Get("From")?.AsString();
			var to=json.Get("To")?.AsString();
			if(from==null) throw new NullReferenceException("From is null");
			if(to==null) throw new NullReferenceException("To is null");
			if(from.Length!=to.Length) return new HotStringReplace(json);
			var keepCase=json.Get("KeepCase")?.AsBool()??true;
			if(keepCase) return new HotStringKeepCase(json);
			return new HotStringReplace(json);
		} catch(Exception e){
			return new HotStringError(json,e);
		}
	}


	public static void Update(Json value){
		Json json;
		lock(IdMap)
			switch(value){
				case JsonNumber number:
					IdMap.Remove((uint)number.AsDouble());
					json=number;
					break;
				case JsonArray array:
					HotStringCategory.Master.LoadJson(array);
					HotStringCategory.SaveNow();
					json=new JsonArray(HotStringCategory.ConfigKeys){HotStringCategory.Master.ToJsonArray(true)};
					break;
				case JsonObject obj:
					var id=(uint)(obj.Get("Id")??throw new Exception("Error getting 'Id'")).AsDouble();
					var b=IdMap.TryGetValue(id,out var old);

					var @new=FromJson(value);
					if(b) HotStringCategory.Master.ReplaceChild(old,@new);
					HotStringCategory.SaveNow();
					json=@new.ToFullJson(true);
					break;
				default:throw new ArgumentException();
			}
		ConfigServer.Broadcast(json.ToString());
	}
}