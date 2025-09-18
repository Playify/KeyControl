using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Jsons;
using PlayifyUtility.Utils.Extensions;

namespace KeyControl.Features.Strings.HotStrings.Complex;

[InitOnLoad]
public class HotStringComplexUnicode:HotStringInternal{
	private static readonly Regex FromUniCode=new(@"^(?:u\+?|\\u)?([0-9a-fA-F]{1,8})$");
	private static readonly Regex FromUniCode2=new(@"\\u([0-9a-fA-F]{4})");
	private static readonly Regex FromUniCode3=new(@"\\U([0-9a-fA-F]{8})");

	private static readonly Regex HotStringRegex=new(@"@[uU]\+?([0-9a-fA-F]+)"+HotStringsHandler.Ending);


	static HotStringComplexUnicode(){
		ConfigServer.Register((ws,json)=>{
			var s=json.AsString();
			ws.Send(new JsonArray("Internal","Unicode","A",s).ToString());
			ws.Send(new JsonArray("Internal","Unicode","B",GetText(s)).ToString());
		},"Internal","Unicode","A");
		ConfigServer.Register((ws,json)=>{
			var s=json.AsString();
			ws.Send(new JsonArray("Internal","Unicode","B",s).ToString());
			ws.Send(new JsonArray("Internal","Unicode","A",GetText(s)).ToString());
		},"Internal","Unicode","B");
	}

	public override (int bs,string s)? Replace(string s){
		if(HotStringRegex.Match(s).Push(out var match).Success)
			return (match.Length,char.ConvertFromUtf32(int.Parse(match.Groups[1].Value,NumberStyles.HexNumber)));
		return null;
	}

	public static string GetText(string text){
		try{
			if(FromUniCode.Match(text).Push(out var match).Success)
				try{
					return char.ConvertFromUtf32(int.Parse(match.Groups[1].Value,NumberStyles.HexNumber));
				} catch(ArgumentOutOfRangeException){
				}
			var s=FromUniCode2.Replace(text,m=>((char)int.Parse(m.Groups[1].Value,NumberStyles.HexNumber)).ToString());
			s=FromUniCode3.Replace(s,m=>char.ConvertFromUtf32(int.Parse(m.Groups[1].Value,NumberStyles.HexNumber)));
			if(s!=text) return s;
			var builder=new StringBuilder();
			for(var i=0;i<text.Length;i++){
				/*var utf32=char.ConvertToUtf32(text,i);
				if(utf32>65536){
					builder.Append("\\U").Append(utf32.ToString("X8"));
					i++;
				} else*/
				var c=text[i];
				if(c is '\t' or '\r' or '\n'||(c>=0x20&&c<0x7f)) builder.Append(text[i]);
				else builder.Append("\\u").Append(((int)c).ToString("X4"));
			}
			return builder.ToString();
		} catch(Exception e){
			Console.WriteLine(e);
			return text;
		}
	}
}