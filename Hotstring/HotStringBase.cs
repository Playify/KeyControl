using System;
using System.Collections.Generic;
using System.Threading;
using KeyControl.Hotstring.Saveable;
using PlayifyUtils.Jsons;

namespace KeyControl.Hotstring{
	public abstract class HotStringBase:HotStringUnsaveable{
		private static int _nextId=0;
		public readonly uint Id;
		public readonly bool Enabled;
		public readonly bool Collapsed;
		
		private static readonly Dictionary<uint,HotStringBase> IdMap=new();

		protected HotStringBase(JsonObject json){
			Id=((uint?)json?.Get("Id")?.AsNumber())??(uint)(Interlocked.Increment(ref _nextId));
			IdMap[Id]=this;
			Enabled=json?.Get("Enabled")?.AsBoolean()??true;
			Collapsed=json?.Get("Collapsed")?.AsBoolean()??false;
		}

		public abstract (string from,string to) FromTo();

		public abstract JsonObject ToJson();
		public virtual JsonObject ToJson(bool useId)=>ToJson();

		public JsonObject ToFullJson(bool useId){
			var o=ToJson(useId);
			if(!Enabled) o.Put("Enabled",false);
			if(Collapsed) o.Put("Collapsed",true);
			if(useId) o.Put("Id",Id);
			return o;
		}


		public static HotStringBase Get(Json json){
			if(json is JsonObject o) return Get(o);
			lock(IdMap)
				if(IdMap.TryGetValue((uint)json.AsNumber(),out var hs))
					return hs;
			throw new ArgumentOutOfRangeException();
		}

		public static HotStringBase Get(JsonObject json){
			try{
				if(json.Has("Category")) return new HotStringCategory(json);
				if(json.Has("Emoji")) return new HotStringEmoji(json);
				if(json.Has("Regex")) return new HotStringRegex(json);
				var from=json.Get("From")?.AsString();
				var to=json.Get("To")?.AsString();
				if(from==null) throw new NullReferenceException("From is null");
				if(to==null) throw new NullReferenceException("To is null");
				if(from.Length!=to.Length) return new HotStringReplace(json);
				var keepCase=json.Get("KeepCase")?.AsBoolean()??true;
				if(keepCase) return new HotStringKeepCase(json);
				return new HotStringReplace(json);
			} catch(Exception e){
				return new HotStringError(json,e);
			}
		}

		public static Json Update(Json value){
			lock(IdMap){
				switch(value){
					case JsonNumber number:
						IdMap.Remove((uint)number.AsNumber());
						return number;
					case JsonArray array:
						HotStringCategory.Master.LoadJson(array);
						return HotStringCategory.Master.ToJsonArray(true);
					case JsonObject obj:
						var id=(uint)obj.Get("Id").AsNumber();
						var b=IdMap.TryGetValue(id,out var old);

						var @new=Get(value);
						if(b) HotStringCategory.Master.ReplaceChild(old,@new);
						return @new.ToFullJson(true);
					default:throw new ArgumentException();
				}
			}
		}

		public virtual IEnumerable<(long,Json)> GetRecursive(){
			yield return (Id,ToFullJson(true));
		}
	}

}