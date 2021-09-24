using System.Windows.Forms;
using KeyControl.Interfaces;
using KeyControl.Utilities;
using static KeyControl.Utilities.Send.SendFlags;

namespace KeyControl.Features{
	public static class NamingHelper{

		public static bool Enabled=false;
		public static long Index=1;
		public static string Pre="R",Post="";
		public static bool SelectAll,Return=true;

		public static void InitConfig(){
			Config.Register(nameof(NamingHelper)+"."+nameof(Enabled),()=>Enabled,json=>Enabled=json.AsBoolean());
			Config.Register(nameof(NamingHelper)+"."+nameof(Index),()=>Index,json=>Index=(long)json.AsNumber());
			Config.Register(nameof(NamingHelper)+"."+nameof(Pre),()=>Pre,json=>Pre=json.AsString());
			Config.Register(nameof(NamingHelper)+"."+nameof(Post),()=>Post,json=>Post=json.AsString());
			Config.Register(nameof(NamingHelper)+"."+nameof(SelectAll),()=>SelectAll,json=>SelectAll=json.AsBoolean());
			Config.Register(nameof(NamingHelper)+"."+nameof(Return),()=>Return,json=>Return=json.AsBoolean());
		}

		public static bool Execute(){
			if(!Enabled) return false;
			
			var send=new Send();
			send.Key(Keys.RControlKey,KeyUp|Hidden);
			if(SelectAll) send.Mod(ModifierKeys.Control,KeyDown|Hidden).Key(Keys.A);
			send.Mod(ModifierKeys.Control,KeyUp|Hidden);
			send.Text(Pre).Text((Index++).ToString()).Text(Post);
			if(Return) send.Char('\n');
			send.SendNow();
			ConfigProvider.SendUpdate(nameof(NamingHelper)+"."+nameof(Index));
			return true;
		}
	}
}