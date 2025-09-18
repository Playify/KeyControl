using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Windows;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using Timer=System.Windows.Forms.Timer;

namespace KeyControl.Features.Hotkeys.Advanced;

[InitOnLoad]
public static partial class Spammer{
	private static readonly bool IndicatorAlwaysVisible=true;
	private static readonly UiThread UiThread=UiThread.Create(nameof(Spammer));
	private static readonly Timer Timer=new();
	private static readonly SpammerIndicatorWindow Indicator=UiThread.Invoke(()=>new SpammerIndicatorWindow());

	private static readonly ConfigValue<bool> Enabled=ConfigValue.Create(false,"Hotkeys","Spammer","Enabled").Listen(b=>{
		if(b&&Modifiers.IsScrollLock)
			new Send().Hide().Key(Keys.Scroll).SendOn(UiThread);

		Running=false;
		UiThread.Invoke(()=>{
			if(b&&IndicatorAlwaysVisible) Indicator.Apply(false);
			else Indicator.Visible=false;
		});
	});
	private static readonly ConfigValue<int> Delay=ConfigValue.Create(100,"Hotkeys","Spammer","Delay").Listen(b=>{Timer.Interval=b;});
	private static readonly ConfigValue<string> Action=ConfigValue.Create("Test","Hotkeys","Spammer","Action");
	private static readonly ConfigValue<bool> Return=ConfigValue.Create(true,"Hotkeys","Spammer","Return");


	private static bool Running{
		get=>Timer.Enabled;
		set{
			if(!Enabled.Value&&value) return;//Can't start spamming if not enabled
			if(Running==value) return;

			Console.WriteLine("Spammer: "+(value?"ON":"OFF"));
			UiThread.Invoke(()=>{
				if(IndicatorAlwaysVisible||value) Indicator.Apply(value);
				else Indicator.Visible=false;

				Timer.Enabled=value;
			});
		}
	}

	static Spammer(){
		Timer.Tick+=(_,_)=>Tick();
		Timer.Interval=Delay.Value;//This line is not needed, but removes the warning that the field is unused

		GlobalKeyboardHook.KeyDown+=e=>{
			if(e.Key==Keys.Scroll){
				e.Handled=true;
				Running^=true;
			}
		};
	}

	private static void Tick(){
		if(!Enabled.Value){
			Running=false;
			return;
		}
		var send=new SendBuilder(Action.Value).ToSend();
		if(Return.Value) send.Text("\n");
		send.SendNow();


	}
}