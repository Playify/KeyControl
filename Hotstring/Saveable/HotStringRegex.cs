using System;
using System.Text.RegularExpressions;
using PlayifyUtils.Jsons;
using static System.Text.RegularExpressions.RegexOptions;

namespace KeyControl.HotString.Saveable;

public class HotStringRegex:HotStringBase{
	private readonly Regex _from;
	private readonly string _to;

	public HotStringRegex(JsonObject json):base(json){
		_from=new Regex(json.Get("Regex").AsString(),json.Get("IgnoreCase")?.AsBoolean()??false?CultureInvariant|IgnoreCase:CultureInvariant);
		if(_from.IsMatch("")) throw new ArgumentException("Regex can't match an empty string");
		_to=json.Get("Replacement").AsString();
	}

	public HotStringRegex(string regex,string to,bool ignoreCase=true):base(null){
		_from=new Regex(regex,ignoreCase?CultureInvariant|IgnoreCase:CultureInvariant);
		if(_from.IsMatch("")) throw new ArgumentException("Regex can't match an empty string");
		_to=to;
	}

	public override JsonObject ToJson()
		=>new(){
			{"Regex",_from.ToString()},
			{"IgnoreCase",(_from.Options&IgnoreCase)!=0},
			{"Replacement",_to},
		};

	public override (int bs,string s)? Replace(string s){
		var s2=_from.Replace(s,_to);
		if(s!=s2) return (s.Length,s2);
		return null;


		/*//Only matches once
		var match=_from.Match(s);
		if(!match.Success) return null;/*
		if(match.Index+match.Length!=s.Length){//allow in middle of text, because RegEx beginners won't know all the Syntax yet
			Console.WriteLine("Illegal Regex: "+_from+" matching in the middle of the text is not allowed");
			return null;
		}#1#
		return (s.Length-match.Index,match.Result(_to)+s.Substring(match.Index+match.Length));*/
	}

	public override (string from,string to) FromTo()=>("/"+_from+((_from.Options&IgnoreCase)!=0?"/i":"/"),_to);
}