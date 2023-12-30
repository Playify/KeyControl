using System;
using KeyControl.Interfaces;
using PlayifyUtils.Utils;

namespace KeyControl.Features.Technical;

public static class OfficeClipboard{//TODO run on timer?
	public static void Run(){
		var b=Windows.EnumWindows((eHwnd,_)=>{
			if(Windows.GetClass(eHwnd)!="#32770") return true;
			if(!Windows.IsWindow(eHwnd)) return true;
			IntPtr? maybeButton=null;

			return Windows.EnumChildWindows(eHwnd,(cHwnd,_)=>{
				if(Windows.GetClass(cHwnd)=="Button") maybeButton??=cHwnd;
				if(Windows.GetClass(cHwnd)=="Static"&&
				   Windows.GetText(cHwnd).StartsWith("Möchten Sie das letzte Element, das Sie kopiert haben, beibehalten?")){
					if(maybeButton.TryGet(out var button)){
						Windows.SendMessage(button,0xF5,0,0);//0xF5=BM_CLICK
						Windows.SendMessage(button,0xF5,0,0);//0xF5=BM_CLICK
					}
					return false;
				}
				return true;
			},IntPtr.Zero);
		},IntPtr.Zero);

		Console.WriteLine("Office: "+!b);
	}
}