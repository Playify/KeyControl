using System.Globalization;
using System.Text;
using KeyControl.Features.Strings.HotStrings;
using KeyControl.Features.Strings.HotStrings.Complex;
using KeyControl.Features.Strings.HotStrings.SaveAble;
using KeyControl.Util;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Strings;

[InitOnLoad]
public static partial class HotStringsHandler{
	public const string Ending="[ \n\t]$";
	private static readonly StringBuilder Builder=new();
	private static readonly List<HotStringInternal> HotStrings=[
		//Complex
		new HotStringComplexCalculate(),
		new HotStringComplexFlip(),
		new HotStringComplexUnicode(),
		//new HotStringComplexNumberConverter(),

		HotStringCategory.Master,
	];


	public static void Reset(){
		HotStringEmoji.EmojiCount=-1;
		Builder.Clear();
	}

	private static void Delete(){
		if(HotStringEmoji.EmojiCount>=0) HotStringEmoji.EmojiCount--;
		if(Builder.Length==0) return;
		if(char.GetUnicodeCategory(Builder[^1])==UnicodeCategory.Surrogate) Builder.Length-=2;
		else Builder.Length--;
	}

	private static bool ExecuteNow(int bufferedCount){
		var s=Builder.ToString();
		//Console.WriteLine("Possible Hotstring:"+s);

		HotStringInternal? curr=null;
		if(!HotStrings.Select(hs=>hs.Replace(s,out curr)).FirstOrDefault(t=>t!=null).TryGet(out var replace)||replace.bs<0){
			HotStringEmoji.EmojiCount=-1;
			return false;
		}
		var (bs,replacement)=replace;
		//Console.Write("HotString: bs="+bs+" s="+replacement);


		var index=0;
		var maximumDeletable=Math.Min(bs,replacement.Length);
		var length=s.Length-bs;
		while(index<maximumDeletable&&s[length+index]==replacement[index]) index++;
		if(index!=0){
			bs-=index;
			replacement=replacement[index..];
			//Console.WriteLine(" => Optimized: bs="+bs+" s="+replacement);
		}// else Console.WriteLine();


		var emoji=HotStringEmoji.EmojiCount;
		HotStringEmoji.EmojiCount=-1;

		KeyEvent.CancelDeadKeys();

		if(bs!=bufferedCount||replacement!=""){
			WriteChars(new string('\b',bufferedCount),0);

			if(bufferedCount>bs) s=s.Substring(s.Length-bufferedCount,bufferedCount-bs);
			else s=new string('\b',bs-bufferedCount);
			s+=replacement;

			if(!WriteChars(s,-1)) new Send().Hide().Text(s).SendNow();
		}
		HotStringEmoji.EmojiCount=emoji;
		if(curr is HotStringEmoji)
			if(HotStringEmoji.EmojiCount==-1) HotStringEmoji.EmojiCount=1;
			else HotStringEmoji.EmojiCount++;
		else HotStringEmoji.EmojiCount=-1;
		return true;
	}
}