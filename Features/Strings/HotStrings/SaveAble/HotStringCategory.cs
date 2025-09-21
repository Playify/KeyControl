using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Jsons;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Web;

namespace KeyControl.Features.Strings.HotStrings.SaveAble;

[InitOnLoad]
public class HotStringCategory:HotStringSaveAble{
	public static readonly HotStringCategory Master=new();
	public static readonly string[] ConfigKeys=["HotStrings","List"];
	private static readonly Dictionary<WebSocket,uint> Blocked=new();
	private readonly string _category;
	private readonly List<HotStringSaveAble> _children=[];

	static HotStringCategory(){
		var loaded=false;
		Config.Listen(ConfigKeys,json=>{
			if(loaded) Update(json!);
			else{
				loaded=true;
				Master.LoadJson(json as JsonArray??JsonArray.ParseOrNull(HotStringsHandler.Defaults)??throw new Exception("Error parsing hotstrings"));
			}
		},true);
		ConfigServer.OnConnect+=ws=>{
			foreach(var json in Master.Jsons())
				ws.Send(json.ToString());

			JsonArray send;
			lock(Blocked) send=new JsonArray("Internal","HotStrings","Block",new JsonArray(Blocked.Values.Distinct().Select(u=>new JsonNumber(u))));
			ws.Send(send.ToString());
		};
		ConfigServer.OnDisconnect+=ws=>{
			JsonArray send;
			lock(Blocked){
				if(!Blocked.Remove(ws))
					return;
				send=new JsonArray("Internal","HotStrings","Block",new JsonArray(Blocked.Values.Distinct().Select(u=>new JsonNumber(u))));
			}
			ConfigServer.Broadcast(send.ToString());
		};
		ConfigServer.Register((ws,json)=>{
			var val=(uint)json.AsDouble();
			JsonArray send;
			lock(Blocked){
				if(val==0) Blocked.Remove(ws);
				else Blocked[ws]=val;
				send=new JsonArray("Internal","HotStrings","Block",new JsonArray(Blocked.Values.Distinct().Select(u=>new JsonNumber(u))));
			}
			ConfigServer.Broadcast(send.ToString());
		},"Internal","HotStrings","Block");
	}

	public static void SaveNow()=>Config.Merge(ConfigKeys,Master.ToJsonArray(false));

	private HotStringCategory():base(new JsonObject{{"Id",0}})=>_category="ROOT";

	public HotStringCategory(JsonObject json):base(json){
		_category=(string)json["Category"];
		if(json.TryGetArray("Children",out var array)) LoadJson(array);
	}

	public override (int bs,string s)? Replace(string s)=>Replace(s,out _);

	public override (int bs,string s)? Replace(string s,out HotStringInternal curr){
		curr=this;
		foreach(var child in _children){
			if(!child.Enabled) continue;
			lock(Blocked)
				if(Blocked.ContainsValue(child.Id))
					continue;
			var tuple=child.Replace(s,out curr);
			if(tuple.TryGet(out var result)) return result;
		}
		return null;
	}

	public override JsonObject ToJson()=>ToJson(false);

	public override JsonObject ToJson(bool useId){
		var o=new JsonObject{{"Category",_category}};
		if(_children.Count!=0) o["Children"]=ToJsonArray(useId);
		return o;
	}

	public JsonArray ToJsonArray(bool useId)=>new(_children.ToArray().Select(hs=>useId?(Json)hs.Id:hs.ToFullJson(false)));

	public void LoadJson(JsonArray json){
		_children.Clear();
		_children.AddRange(json.Select(FromJson));
	}

	public IEnumerable<Json> Jsons(){
		foreach(var child in _children)
			if(child is HotStringCategory cat)
				foreach(var json in cat.Jsons())
					yield return json;
			else yield return child.ToFullJson(true);
		if(Id==0) yield return new JsonArray(ConfigKeys){ToJsonArray(true)};
		else yield return ToFullJson(true);
	}

	public bool ReplaceChild(HotStringSaveAble? old,HotStringSaveAble @new){
		for(var i=0;i<_children.Count;i++){
			var child=_children[i];
			if(child==old){
				_children[i]=@new;
				return true;
			}
			if(child is not HotStringCategory category) continue;
			if(category.ReplaceChild(old,@new)) return true;
		}
		return false;
	}
}