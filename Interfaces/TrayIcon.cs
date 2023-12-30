using System.Drawing;
using System.Windows.Forms;

namespace KeyControl.Interfaces;

public static class TrayIcon{
	public static NotifyIcon Create(){
		var notifyIcon=new NotifyIcon{
			Icon=new Icon(typeof(Program),"Resources.favicon.ico"),
			Text="KeyControl",
			ContextMenu=new ContextMenu(new[]{
				new MenuItem("Settings",(_,_)=>ConfigWindow.Open()){DefaultItem=true},
				new MenuItem("&Unhide all",(_,_)=>Windows.RestoreAllWindows()),
				new MenuItem("&Exit and unhide all",(_,_)=>{Program.Exit("Menu>Exit and unhide all");}),
			}),
		};
		notifyIcon.Click+=(_,e)=>{
			if(e is MouseEventArgs m&&(m.Button&MouseButtons.Left)==0) return;//don't do anything on right mouse button
			ConfigWindow.Open();
		};
		return notifyIcon;
	}
}