using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using KeyControl.HotKeyHandler;
using NCalc;
using PlayifyUtils.Utils;

namespace KeyControl.Hotstring.Complex{
	public class HotStringComplexCalc:HotStringUnsaveable{
		private static readonly Regex Regex=new("@=(.*?)"+HotStringHandler.Ending,RegexOptions.IgnoreCase);

		private static readonly Random Random=new();

		public override (int bs,string s)? Replace(string s){
			if(!Regex.Match(s).Push(out var match).Success) return null;
			var calculate=Calculate(match.Groups[1].Value);
			if(calculate==null) return null;
			return (match.Length,calculate);


			/*
			if(s.Length<=2||!s.EndsWith("=")) return null;
			var index=s.Length-1;
			while(true)
			{
				if(index==0) return null;
				index=s.LastIndexOf('=',index-1);
				if(index==-1) return null;
				var expression=s.Substring(index,s.Length-index);
				expression=expression.Trim('=',' ');
				var calculate=Calculate(expression);
				if(calculate==null) continue;
				return (s.Length-index,calculate);
			}*/
		}

		public static string Calculate(string expression)=>Calculate(ref expression);

		public static string Calculate(ref string expression){
			while(true){
				expression=expression.Trim(' ','\t','\r','\n');
				break;
				//if(!expression.StartsWith("=")||!expression.EndsWith("=")) break;
				//expression=expression.Substring(1,expression.Length-2);
			}
			
			var repl=Regex.Replace(expression,"[^()]","");
			while(repl.Length!=0){
				while(repl.Contains("()")) repl=repl.Replace("()","");
				if(repl.StartsWith(")")){
					repl=repl.Substring(1);
					expression="("+expression;
				}
				if(repl.EndsWith("(")){
					repl=repl.Substring(0,repl.Length-1);
					expression+=")";
				}
			}

			object o;
			var textWriter=Console.Error;
			try{
				Console.Write("Calculating:\""+expression+"\"=");
				Console.SetError(TextWriter.Null);
				var ex=new Expression(expression,EvaluateOptions.IgnoreCase);
				ex.EvaluateParameter+=Parameter;
				ex.EvaluateFunction+=Function;
				o=ex.Evaluate();
			} catch(Exception e){
				Console.WriteLine(e.GetType().Name+":"+e.Message);
				return null;
			} finally{
				Console.SetError(textWriter);
			}
			string s;
			switch(o){
				case bool valBool:
					s=valBool?"true":"false";
					break;
				case sbyte valSbyte:
					s=valSbyte.ToString(CultureInfo.InvariantCulture);
					break;
				case byte valByte:
					s=valByte.ToString(CultureInfo.InvariantCulture);
					break;
				case short valShort:
					s=valShort.ToString(CultureInfo.InvariantCulture);
					break;
				case ushort valUshort:
					s=valUshort.ToString(CultureInfo.InvariantCulture);
					break;
				case int valInt:
					s=valInt.ToString(CultureInfo.InvariantCulture);
					break;
				case uint valUint:
					s=valUint.ToString(CultureInfo.InvariantCulture);
					break;
				case long valLong:
					s=valLong.ToString(CultureInfo.InvariantCulture);
					break;
				case ulong valUlong:
					s=valUlong.ToString(CultureInfo.InvariantCulture);
					break;
				case float valFloat:
					s=valFloat.ToString(CultureInfo.InvariantCulture);
					break;
				case double valDouble:
					s=valDouble.ToString(CultureInfo.InvariantCulture);
					break;
				case decimal valDecimal:
					s=valDecimal.ToString(CultureInfo.InvariantCulture);
					break;
				default:
					s=o.ToString();
					break;
			}
			Console.WriteLine(s);
			return s;
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
							functionArgs.Result=Random.Next();
							break;
						case 1:
							var o=functionArgs.Parameters[0].Evaluate();
							if(o is int i) functionArgs.Result=Random.Next(i);
							else functionArgs.Result=Random.NextDouble()*Convert.ToDouble(o);
							break;
						case 2:
							functionArgs.Result=Random.Next((int)functionArgs.Parameters[0].Evaluate(),(int)functionArgs.Parameters[1].Evaluate());
							break;
					}
					break;
				case "randf":
					switch(functionArgs.Parameters.Length){
						case 0:
							functionArgs.Result=Random.Next();
							break;
						case 1:
							var d1=Convert.ToDouble(functionArgs.Parameters[0].Evaluate());
							functionArgs.Result=Random.NextDouble()*d1;
							break;
						case 2:
							d1=Convert.ToDouble(functionArgs.Parameters[0].Evaluate());
							var d2=Convert.ToDouble(functionArgs.Parameters[1].Evaluate());
							functionArgs.Result=Random.NextDouble()*(d2-d1)+d1;
							break;
					}
					break;
			}
		}
	}
}