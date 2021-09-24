using System.Text.RegularExpressions;
using static KeyControl.Utilities.Send.SendFlags;

namespace KeyControl.Utilities{
	public static class Utils{
		public static readonly Regex NeverMatchRegex=new("$-^");
		public static Send.SendFlags SetDown(this Send.SendFlags flags,bool? down)=>down.HasValue?(flags&~KeyPress)|(down.Value?KeyDown:KeyUp):flags|KeyPress;
		public static Send.SendFlags SetHidden(this Send.SendFlags flags,bool hidden)=>hidden?flags|Hidden:flags&~Hidden;

		public static int SetBits(int val,int bits,bool one)=>one?val|bits:val&~bits;
	}
}