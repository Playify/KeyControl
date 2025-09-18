using System.Text.RegularExpressions;
using KeyControl.Util;
using PlayifyUtility.Jsons;
using PlayifyUtility.Utils.Extensions;

namespace KeyControl.Features.Strings.HotStrings.SaveAble;

[InitOnLoad]
public class HotStringEmoji:HotStringSaveAble{
	private static readonly int EmojiTimeout=2*SystemInformation.DoubleClickTime;//Should be a good trade off for quicker users and slower users
	private static int _emojiCount;//Allows quick delete and reentering of emojis, while an emoji chain is active
	private static long _emojiChainTimestamp;//milliseconds
	public static int EmojiCount{
		get{
			if(Environment.TickCount64>_emojiChainTimestamp+EmojiTimeout) _emojiCount=-1;
			return _emojiCount;
		}
		set{
			if(value>=0) _emojiChainTimestamp=Environment.TickCount64;
			_emojiCount=value;
		}
	}

	private readonly string _emoji;
	private readonly string _from;
	private readonly Regex _trigger;
	private readonly string _trigger2;

	public HotStringEmoji(JsonObject json):base(json){
		_from=(string)json["From"];
		if(_from.Length==0) throw new ArgumentException("From can't be empty");
		//if(_from.Length!=3) throw new ArgumentException(nameof(_from)+" must have a Length of 3");
		var pattern=json.Get("Regex")?.AsString()??Regex.Escape(_from)+"$";
		var ignoreCase=json.Get("IgnoreCase")?.AsBool()??false;
		_trigger=new Regex(pattern,ignoreCase?RegexOptions.IgnoreCase:RegexOptions.None);
		_emoji=(string)json["Emoji"];
		_trigger2=_from.Substring(_from.Length-1);
	}

	public HotStringEmoji(string from,string emoji,Regex? trigger=null):base(null){
		//if(from.Length!=3) throw new ArgumentException(nameof(from)+" must have a Length of 3");
		_from=from;
		_emoji=emoji;
		_trigger=trigger??new Regex(Regex.Escape(from)+"$");
		_trigger2=from.Substring(from.Length-1);
	}

	public override JsonObject ToJson(){
		var json=new JsonObject{
			{"Emoji",_emoji},
			{"From",_from},
		};
		if((_trigger.Options&RegexOptions.IgnoreCase)!=0) json["IgnoreCase"]=true;
		if(_trigger.ToString()!=Regex.Escape(_from)+"$") json["Regex"]=_trigger.ToString();
		return json;
	}

	public override (int bs,string s)? Replace(string s){
		if(_trigger.Match(s).Push(out var match).Success)
			if(match.Index+match.Length!=s.Length){
				Console.WriteLine("Illegal Regex: \""+_trigger+"\", must match at end of String");
				return null;
			} else
				return (match.Length,_emoji);
		if(EmojiCount!=-1&&s.EndsWith(_trigger2))
			return (_trigger2.Length,_emoji);
		return null;
	}
}