using System;
using System.IO;
using System.Windows.Forms;
using KeyControl.HotKeyHandler;
using KeyControl.Interfaces;
using KeyControl.Utilities;
using PlayifyUtils.Utils;

namespace KeyControl.Features;

public static class CapsLock{
	public static bool Override=true;
	public static SendBuilder Advanced=new SendBuilder().Key(Keys.Apps);
	public static bool CsGoK=true;

	public static void InitConfig(){
		Config.Register(nameof(CapsLock)+"."+nameof(Override),()=>Override,j=>Override=j.AsBoolean());
		Config.Register(nameof(CapsLock)+"."+nameof(Advanced),()=>Advanced.ToString(),j=>Advanced=new SendBuilder(j.AsString()));
		Config.Register(nameof(CapsLock)+"."+nameof(CsGoK),()=>CsGoK,j=>CsGoK=j.AsBoolean());
	}

	public static bool Execute(){
		if(Modifiers.Win){
			if(!Modifiers.Shift&&!Modifiers.Alt&&!Modifiers.Ctrl) new Send().Hide().Key(Keys.RShiftKey).SendNow();//Cancel Windows key, not best option.
			ConfigWindow.Open();
			return true;
		}
		if(Modifiers.Shift||Modifiers.Ctrl||Modifiers.IsCapsLock) return false;//dont replace

		if(CsGoK){
			var fileName=Path.GetFileName(Windows.GetExe(Windows.GetForegroundWindow()));
			if("csgo.exe".Equals(fileName,StringComparison.OrdinalIgnoreCase)){
				new Send().Key(Keys.K,true).SendNow();
				KeyboardHandler.ReleaseKeys[Keys.CapsLock]=Keys.K;
				return true;
			}
		}
		if(!Override) return false;
		if(Advanced.AsSingleKey().TryGet(out var replacement)){
			new Send().Key(replacement,true).SendNow();
			KeyboardHandler.ReleaseKeys[Keys.CapsLock]=replacement;
		} else Advanced.Build().SendNow();
		return true;
	}
}