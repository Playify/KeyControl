using System;
using System.Windows.Forms;
using KeyControl.Interfaces;
using KeyControl.Utilities;

namespace KeyControl.Features;

public static class Spammer{//TODO add indicator

	public static bool Enabled;
	public static SendBuilder Advanced=new("{LButton}");
	public static bool Return;
	private static readonly Timer Timer=new();
	public static int Delay{
		get=>Timer.Interval;
		set=>Timer.Interval=value;
	}

	public static bool Running{
		get=>Timer.Enabled;
		set{
			Console.WriteLine("Spammer: "+(value?"ON":"OFF"));
			Timer.Enabled=value;
		}
	}

	public static void InitConfig(){
		Config.Register(nameof(Spammer)+"."+nameof(Enabled),()=>Enabled,json=>Enabled=json.AsBoolean());
		Config.Register(nameof(Spammer)+"."+nameof(Delay),()=>Delay,json=>Delay=(int) json.AsNumber());
		Config.Register(nameof(Spammer)+"."+nameof(Advanced),()=>Advanced.ToString(),j=>Advanced=new SendBuilder(j.AsString()));
		Config.Register(nameof(Spammer)+"."+nameof(Return),()=>Return,json=>Return=json.AsBoolean());

		Timer.Tick+=Spam;
	}

	private static void Spam(object sender,EventArgs e){
		if(!Enabled){
			Running=false;
			return;
		}
		var send=new Send().Hide();
		Advanced.Build(send);
		if(Return) send.Char('\n');
		send.SendNow();
	}
}