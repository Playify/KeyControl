using System.Drawing;
using System.Windows.Forms;

namespace KeyControl.Interfaces;

public static class TrayIcon{
	public static NotifyIcon Create(){
		var notifyIcon=new NotifyIcon{
			Icon=new Icon(typeof(Program),"Resources.favicon.ico"),
			Text="KeyControl",
			ContextMenu=new ContextMenu(new[]{
				new MenuItem("Settings",(s,e)=>ConfigWindow.Open()){DefaultItem=true},
				new MenuItem("&Unhide all",(s,e)=>Windows.RestoreAllWindows()),
				new MenuItem("&Exit and unhide all",(s,e)=>{Program.Exit("Menu>Exit and unhide all");}),
			}),
		};
		notifyIcon.Click+=(s,e)=>{
			if(e is MouseEventArgs m&&(m.Button&MouseButtons.Left)==0) return;//don't do anything on right mouse button
			ConfigWindow.Open();
		};
		return notifyIcon;
	}
}