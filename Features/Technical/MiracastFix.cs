using System;
using System.Runtime.InteropServices;
using KeyControl.Interfaces;

namespace KeyControl.Features.Technical;

public static class MiracastFix{
	private static bool _enabled=true;

	public static void InitConfig(){
		Config.Register(nameof(Technical)+"."+nameof(MiracastFix),()=>_enabled,j=>_enabled=j.AsBoolean());
	}


	[DllImport("user32.dll",CharSet=CharSet.Auto,SetLastError=true)]
	private static extern bool SystemParametersInfo(int uiAction,int uiParam,out int pvParam,int fWinIni);

	[DllImport("user32.dll",CharSet=CharSet.Auto,SetLastError=true)]
	private static extern bool SystemParametersInfo(int uiAction,int uiParam,int pvParam,int fWinIni);

	private static int Delay{
		get
			=>!SystemParametersInfo(0x0016,0,out var delay,0)
				  ?throw new Exception("Failed to retrieve keyboard repeat delay.")
				  :delay;
		set{
			if(!SystemParametersInfo(0x0017,value,0,0)) throw new Exception("Failed to set keyboard repeat delay.");
		}
	}

	public static void Run(){
		if(!_enabled) return;

		if(Delay!=1) Delay=1;
	}
}