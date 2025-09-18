using System.Text.RegularExpressions;
using KeyControl.Features.Strings.Calculate;
using PlayifyUtility.Utils.Extensions;

namespace KeyControl.Features.Strings.HotStrings.Complex;

public class HotStringComplexCalculate:HotStringInternal{
	private static readonly Regex Regex=new("@=(.*?)"+HotStringsHandler.Ending,RegexOptions.IgnoreCase);

	public override (int bs,string s)? Replace(string s){
		if(!Regex.Match(s).IsSuccess(out var match)) return null;
		var calculate=CalculateExpression.Evaluate(match.Groups[1].Value);
		if(calculate==null) return null;
		return (match.Length,calculate);
	}
}