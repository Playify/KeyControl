using System.Globalization;
using System.Text.RegularExpressions;
using KeyControl.Configuration;
using KeyControl.Util;
using NCalc;
using PlayifyUtility.Jsons;
using PlayifyUtility.Utils.Extensions;
using Expression=NCalc.Expression;

namespace KeyControl.Features.Strings.Calculate;

[InitOnLoad]
public static class CalculateExpression{
	static CalculateExpression(){
		ConfigServer.Register((ws,json)=>{ws.Send(new JsonArray("Internal","Calculate","Result",Evaluate(json.AsString())??"").ToString());},"Internal","Calculate","Expression");
		ConfigServer.Register((_,_)=>{},"Internal","Calculate","Result");
	}

	public static string? Evaluate(string s)=>Evaluate(ref s);

	public static string? Evaluate(ref string s){
		var old=s;
		if(Evaluate(ref s,false).NotNull(out var result)) return result;
		s=old;
		return Evaluate(ref s,true);
	}

	private static string? Evaluate(ref string s,bool germanCulture){
		//Trim excess whitespaces
		s=s.Trim(' ','\t','\r','\n');
		if(s=="") return null;

		//Fix brackets
		var brackets=Regex.Replace(s,"[^()]","");
		while(brackets.Length!=0){
			while(brackets.Contains("()")) brackets=brackets.Replace("()","");
			if(brackets.StartsWith(')')) (brackets,s)=(brackets[1..],'('+s);
			if(brackets.EndsWith('(')) (brackets,s)=(brackets[..^1],s+')');
		}

		if(germanCulture) s=s.Replace(',','.');

		try{
			var ex=new Expression(s,EvaluateOptions.IgnoreCase|EvaluateOptions.RoundAwayFromZero);
			ex.EvaluateParameter+=Parameter;
			ex.EvaluateFunction+=Function;
			var o=ex.Evaluate();
			return o switch{
				bool valBool=>valBool?"true":"false",
				IConvertible valNumber=>valNumber.ToString(germanCulture?CultureInfo.GetCultureInfo("de_DE"):CultureInfo.InvariantCulture),
				_=>o.ToString(),
			};
		} catch(Exception e){
			Console.WriteLine("Error calculating expression: "+e.GetType().Name+":"+e.Message);
			return null;
		}
	}


	private static void Parameter(string name,ParameterArgs args){
		switch(name.ToLowerInvariant()){
			case "e":
				args.Result=Math.E;
				break;
			case "π":
			case "pi":
				args.Result=Math.PI;
				break;
			case "φ":
			case "phi":
				args.Result=(1+Math.Sqrt(5))/2;
				break;
		}
	}

	private static void Function(string name,FunctionArgs functionArgs){
		switch(name.ToLowerInvariant()){
			case "root":
				var p1=Convert.ToDouble(functionArgs.Parameters[0].Evaluate());
				switch(functionArgs.Parameters.Length){
					case 1:
						functionArgs.Result=Math.Sqrt(p1);
						break;
					case 2:
						var p2=Convert.ToDouble(functionArgs.Parameters[1].Evaluate());
						functionArgs.Result=Math.Pow(p1,1d/p2);
						break;
				}
				break;
			case "int":
				if(functionArgs.Parameters.Length==1) functionArgs.Result=Convert.ToInt64(functionArgs.Parameters[0].Evaluate());
				break;
			case "rand":
			case "random":
				switch(functionArgs.Parameters.Length){
					case 0:
						functionArgs.Result=Random.Shared.Next();
						break;
					case 1:
						var o=functionArgs.Parameters[0].Evaluate();
						if(o is int i) functionArgs.Result=Random.Shared.Next(i);
						else functionArgs.Result=Random.Shared.NextDouble()*Convert.ToDouble(o);
						break;
					case 2:
						functionArgs.Result=Random.Shared.Next((int)functionArgs.Parameters[0].Evaluate(),(int)functionArgs.Parameters[1].Evaluate());
						break;
				}
				break;
			case "randf":
				switch(functionArgs.Parameters.Length){
					case 0:
						functionArgs.Result=Random.Shared.Next();
						break;
					case 1:
						var d1=Convert.ToDouble(functionArgs.Parameters[0].Evaluate());
						functionArgs.Result=Random.Shared.NextDouble()*d1;
						break;
					case 2:
						d1=Convert.ToDouble(functionArgs.Parameters[0].Evaluate());
						var d2=Convert.ToDouble(functionArgs.Parameters[1].Evaluate());
						functionArgs.Result=Random.Shared.NextDouble()*(d2-d1)+d1;
						break;
				}
				break;
		}
	}
}