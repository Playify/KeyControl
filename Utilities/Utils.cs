using System.Text.RegularExpressions;

namespace KeyControl.Utilities;

public static class Utils{
	public static readonly Regex NeverMatchRegex=new("$-^");

	public static int SetBits(int val,int bits,bool one)=>one?val|bits:val&~bits;
}