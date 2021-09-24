using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using KeyControl.HotKeyHandler;
using KeyControl.Interfaces;
using PlayifyUtils.Utils;

namespace KeyControl.Hotstring.Complex{
	public class HotStringComplexNumberConverter:HotStringUnsaveable{

		private static readonly Regex[] Numbers={
			new("@b[dnh]([01]+)"+HotStringHandler.Ending),
			new("@h[dnb]([0-9a-fA-F]+)"+HotStringHandler.Ending),
			new("@[dn][bh]([0-9]+)"+HotStringHandler.Ending)
		};

		public override (int bs,string s)? Replace(string s){
			foreach(var number in Numbers){
				var match=number.Match(s);
				if(match.Success){
					var value=match.Value;
					var replacement=ConvertNumber(match.Groups[1].Value,Radix(value[1]),Radix(value[2]));
					if(Modifiers.Shift) replacement=replacement.ToUpperInvariant();
					return (value.Length,replacement);
				}
			}
			return null;
		}

		public static string ConvertNumber(string input,int baseIn,int baseOut){
			if(string.IsNullOrWhiteSpace(input)) return "";

			if(input.Contains("\n")){
				var strings=input.Split('\n');
				return strings.Select(s=>ConvertNumber(s,baseIn,baseOut)).Join("\n");
			}
			input=input.Trim().ToLowerInvariant();

			if(baseIn==baseOut) return input;

			//Check correct digits
			const string digits="0123456789abcdef";
			if(baseIn switch{
				2=>input.Any(c=>c is not ('0' or '1')),
				10=>input.Any(c=>c is <'0' or >'9'),
				16=>input.Any(c=>!digits.Contains(c)),
				_=>false
			}) throw new FormatException("Illegal digits");

			if(baseIn==2){//from binary
				if(input.Length<64) return Convert.ToString(Convert.ToInt64(input,2),baseOut);

				if(baseOut==16){
					var newLength=(input.Length+63)&(~63);//add 63 to get at least correct value, clear last 63 bits to make multiple of 64
					input=input.PadLeft(newLength,'0');
					return Enumerable
					       .Range(0,newLength/64)
					       .Select(i=>input.Substring(i*64,64))
					       .Select(sub=>Convert.ToString(Convert.ToInt64(sub,2),16))
					       .Join("")
					       .TrimStart('0');
				}
			}
			if(baseIn==16){//from binary
				if(input.Length<16) return Convert.ToString(Convert.ToInt64(input,16),baseOut);

				if(baseOut==2){
					var newLength=(input.Length+15)&(~15);//add 15 to get at least correct value, clear last 15 bits to make multiple of 16
					input=input.PadLeft(newLength,'0');
					return Enumerable
					       .Range(0,newLength/16)
					       .Select(i=>input.Substring(i*16,16))
					       .Select(sub=>Convert.ToString(Convert.ToInt64(sub,16),2))
					       .Join("")
					       .TrimStart('0');
				}
			}

			// 2->10, 16->10, 10->2, 10->16, ?->?
			var big=Parse(input,digits.Substring(0,baseIn));
			return ToString(big,digits.Substring(0,baseOut));
		}

		public static BigInteger Parse(string value,string digits){
			if(string.IsNullOrEmpty(value)) return BigInteger.Zero;

			var ret=new BigInteger(0);
			var radixInt=digits.Length;
			var radixBig=new BigInteger(radixInt);
			foreach(var c in value){
				ret*=radixBig;
				var i=digits.IndexOf(c);
				if(i<0||i>radixInt) throw new ArgumentException();
				ret+=i;
			}
			return ret;
		}

		public static string ToString(BigInteger value,string digits){
			var str=new StringBuilder();
			var radix=new BigInteger(digits.Length);
			while(true){
				value=BigInteger.DivRem(value,radix,out var remainder);

				str.Insert(0,digits[(int)remainder]);
				if(value.Sign==0) return str.ToString();
			}
		}

		private static int Radix(char c)
			=>c switch{
				'b'=>2,
				'h'=>16,
				'd'=>10,
				'n'=>10,
				_=>0
			};
	}
}