using System;
using System.Text.RegularExpressions;
using KeyControl.HotKeyHandler;
using PlayifyUtils.Jsons;
using PlayifyUtils.Utils;

namespace KeyControl.Hotstring.Saveable{
	public class HotStringEmoji:HotStringBase{
		private readonly string _emoji;
		private readonly string _from;
		private readonly Regex _trigger;
		private readonly string _trigger2;

		public HotStringEmoji(JsonObject json):base(json){
			_from=json.Get("From").AsString();
			if(_from.Length==0) throw new ArgumentException("From can't be empty");
			//if(_from.Length!=3) throw new ArgumentException(nameof(_from)+" must have a Length of 3");
			var pattern=json.Get("Regex")?.AsString()??Regex.Escape(_from)+"$";
			var ignoreCase=json.Get("IgnoreCase")?.AsBoolean()??false;
			_trigger=new Regex(pattern,ignoreCase?RegexOptions.IgnoreCase:RegexOptions.None);
			_emoji=json.Get("Emoji").AsString();
			_trigger2=_from.Substring(_from.Length-1);
		}

		public HotStringEmoji(string from,string emoji,Regex trigger=null):base(null){
			//if(from.Length!=3) throw new ArgumentException(nameof(from)+" must have a Length of 3");
			_from=from;
			_emoji=emoji;
			_trigger=trigger??new Regex(Regex.Escape(from)+"$");
			_trigger2=from.Substring(from.Length-1);
		}

		public override JsonObject ToJson(){
			var json=new JsonObject{
				{"Emoji",_emoji},
				{"From",_from}
			};
			if((_trigger.Options&RegexOptions.IgnoreCase)!=0)json.Put("IgnoreCase",true);
			if(_trigger.ToString()!=Regex.Escape(_from)+"$") json.Put("Regex",_trigger.ToString());
			return json;
		}

		public override (int bs,string s)? Replace(string s){
			if(_trigger.Match(s).Push(out var match).Success)
				if(match.Index+match.Length!=s.Length){
					Console.WriteLine("Illegal Regex: \""+_trigger+"\", must match at end of String");
					return null;
				} else{
					HotStringHandler.CurrentEmoji=this;
					return (match.Length,_emoji);
				}
			if(HotStringHandler.Emoji!=-1&&s.EndsWith(_trigger2)){
				HotStringHandler.CurrentEmoji=this;
				return (_trigger2.Length,_emoji);
			}
			return null;
		}

		public override (string from,string to) FromTo()=>(_from,_emoji);
	}
}