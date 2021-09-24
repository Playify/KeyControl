using System.Collections.Generic;
using System.Linq;
using PlayifyUtils.Jsons;
using PlayifyUtils.Utils;

namespace KeyControl.Hotstring.Saveable{
	public class HotStringCategory:HotStringBase{
		private string _category;
		private readonly List<HotStringBase> _childs=new();

		public static HotStringCategory Master=new();
		public static Dictionary<uint,int> Blocked=new();

		private HotStringCategory():base(new JsonObject{{"Id",0}}){
		}

		public HotStringCategory(JsonObject json):base(json){
			_category=json.Get("Category").AsString();
			var array=json.GetA("Children");

			if(array!=null) LoadJson(array);
		}

		public override (int bs,string s)? Replace(string s){
			foreach(var child in _childs){
				if(!child.Enabled)continue;
				lock(Blocked) if(Blocked.ContainsKey(child.Id)) continue;
				var tuple=child.Replace(s);
				if(tuple.TryGet(out var result)) return result;
			}
			return null;
		}

		public override (string from,string to) FromTo()=>(null,null);

		public override JsonObject ToJson()=>ToJson(false);

		public override JsonObject ToJson(bool useId){
			var o=new JsonObject{{"Category",_category}};
			if(_childs.Count!=0) o.Put("Children",ToJsonArray(useId));
			return o;
		}

		public JsonArray ToJsonArray(bool useId){
			return new(_childs.ToArray().Select(hs=>useId?(Json)hs.Id:hs.ToFullJson(false)));
		}

		public void LoadJson(JsonArray json){
			_childs.Clear();
			_childs.AddRange(json.Select(Get));
		}

		public override IEnumerable<(long,Json)> GetRecursive(){
			foreach(var child in _childs)
			foreach(var tuple in child.GetRecursive())
				yield return tuple;
			yield return (Id,Id==0?ToJsonArray(true):ToFullJson(true));
		}

		public bool ReplaceChild(HotStringBase old,HotStringBase @new){
			for(var i=0;i<_childs.Count;i++){
				var child=_childs[i];
				if(child==old){
					_childs[i]=@new;
					return true;
				}
				if(!(child is HotStringCategory category)) continue;
				if(category.ReplaceChild(old,@new)) return true;
			}
			return false;
		}
	}
}