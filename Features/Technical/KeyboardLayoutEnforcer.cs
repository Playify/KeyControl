using System.Globalization;
using System.Runtime.InteropServices;
using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Technical;

[InitOnLoad]
public static class KeyboardLayoutEnforcer{
	private static readonly ConfigValue<string> Enforce=ConfigValue.Create("","Technical","KeyboardLayoutEnforcer").Listen(s=>Utils.UiThread.Invoke(()=>{
		_hook?.Dispose();
		if(Parse(s)!=null) _hook=GlobalEventHook.Hook(0x8004,_=>FixNow());
		FixNow();
	}));
	private static IDisposable? _hook;

	private static CultureInfo? Parse(string s){
		try{
			var lang=new CultureInfo(s);
			if(lang.IsNeutralCulture) return null;
			if(lang.Equals(CultureInfo.InvariantCulture)) return null;
			return lang;
		} catch(CultureNotFoundException){
			return null;
		}
	}


	private static void FixNow(){
		var lang=Parse(Enforce.Value);
		if(lang==null) return;
		var langId=lang.KeyboardLayoutId&0xFFFF;

		var found=false;
		var removed=0;
		var layouts=GetLayoutsRaw();
		foreach(var hkl in layouts){
			var langIdHkl=(int)((ulong)hkl.ToInt64()&0xFFFF);
			if(langIdHkl==langId)
				found=true;
			else if(UnloadKeyboardLayout(hkl))
				removed++;
		}
		if(!found) TryAdd(lang);

		if(removed!=0){
			Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: Removed {removed} keyboard layouts. Current focused: {WinWindow.Foreground.ProcessExe}");
		}
	}


	[DllImport("user32.dll")]
	private static extern uint GetKeyboardLayoutList(int nBuff,IntPtr[]? lpList);

	[DllImport("user32.dll")]
	private static extern bool UnloadKeyboardLayout(IntPtr hkl);

	[DllImport("user32.dll")]
	private static extern IntPtr LoadKeyboardLayout(string pwszKlid,uint flags);

	/// <summary>
	/// Returns currently loaded keyboard layouts as CultureInfo
	/// </summary>
	public static IReadOnlyList<CultureInfo> GetKeyboardLayouts()
		=>GetLayoutsRaw()
		  .Select(ToCulture)
		  .NonNull()
		  .DistinctBy(c=>c.Name)
		  .ToList();

	/// <summary>
	/// Tries to remove a keyboard layout matching the given culture
	/// </summary>
	public static bool TryRemove(CultureInfo culture){
		var success=false;
		var targetLangId=culture.KeyboardLayoutId&0xFFFF;

		foreach(var hkl in GetLayoutsRaw()){
			var langId=(int)((ulong)hkl.ToInt64()&0xFFFF);
			if(langId==targetLangId&&UnloadKeyboardLayout(hkl))
				success=true;
		}

		return success;
	}

	/// <summary>
	/// Adds the default keyboard layout for the given culture
	/// </summary>
	public static bool TryAdd(CultureInfo culture,bool activate=true){
		try{
			// KLID format: 0000 + LANGID
			var klid=(culture.KeyboardLayoutId&0xFFFF).ToString("X8");
			return LoadKeyboardLayout(klid,activate?1u:0)!=IntPtr.Zero;
		} catch{
			return false;
		}
	}


	private static IntPtr[] GetLayoutsRaw(){
		var count=(int)GetKeyboardLayoutList(0,null);
		var list=new IntPtr[count];
		GetKeyboardLayoutList(list.Length,list);
		return list;
	}

	private static CultureInfo? ToCulture(IntPtr hkl){
		try{
			return new CultureInfo((int)((ulong)hkl.ToInt64()&0xFFFF));
		} catch{
			return null;// some exotic layouts may fail
		}
	}
}