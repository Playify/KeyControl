using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Windows;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;
using PlayifyUtility.Windows.Win;
using Timer=System.Windows.Forms.Timer;

namespace KeyControl.Features.Games;

[InitOnLoad]
public static partial class CrossHair{
	private static readonly UiThread UiThread=UiThread.Create(nameof(CrossHair));
	//Timer is only needed for edge cases, most of the work is done by the GlobalEventHook
	private static readonly Timer Timer=new(){Interval=100};
	private static readonly CrossHairWindow Instance=UiThread.Invoke(()=>new CrossHairWindow());
	private static IDisposable? _hook;


	private static readonly ConfigValue<bool> Enabled=ConfigValue.Create(false,"Games","CrossHair","Enabled").Listen(b=>{
		_hook?.Dispose();
		UiThread.BeginInvoke(()=>{
			if(b) _hook=GlobalEventHook.Hook(UiThread,0x800b,_=>OnTick());
			Timer.Enabled=b;
			OnTick();
		});
	});
	private static readonly ConfigValue<uint> LineColor=ConfigValue.Create(0xFF0000u,"Games","CrossHair","Color");
	private static readonly ConfigValue<int> Length=ConfigValue.Create(20,"Games","CrossHair","Length");
	private static readonly ConfigValue<int> Thickness=ConfigValue.Create(2,"Games","CrossHair","Thickness");
	private static readonly ConfigValue<int> Gap=ConfigValue.Create(10,"Games","CrossHair","Gap");
	private static readonly ConfigValue<bool> Dot=ConfigValue.Create(true,"Games","CrossHair","Dot");


	static CrossHair(){
		Timer.Tick+=(_,_)=>OnTick();


		GlobalKeyboardHook.KeyDown+=e=>{
			if(e.Key==Keys.S&&Modifiers.Ctrl&&Modifiers.Win){
				e.Handled=true;
				Enabled.Value^=true;
			}
		};
	}

	private static void OnTick(){
		if(!Timer.Enabled){
			Instance.Visible=false;
			return;
		}

		var foreground=WinWindow.Foreground;

		var toCenter=Length.Value+Gap.Value;
		var toFar=toCenter+Thickness.Value+Gap.Value;
		var size=toFar+Length.Value;

		var rect=foreground.WindowRect;
		var x=(rect.Left+rect.Right-size)/2;
		var y=(rect.Top+rect.Bottom-size)/2;

		var region=new Region();
		region.MakeEmpty();
		region.Union(new Rectangle(0,toCenter,Length.Value,Thickness.Value));//left
		region.Union(new Rectangle(toCenter,0,Thickness.Value,Length.Value));//top
		region.Union(new Rectangle(toFar,toCenter,Length.Value,Thickness.Value));//right
		region.Union(new Rectangle(toCenter,toFar,Thickness.Value,Length.Value));//bottom
		if(Dot.Value) region.Union(new Rectangle(toCenter,toCenter,Thickness.Value,Thickness.Value));//dot

		var alpha=(byte)(LineColor.Value>> 24);
		if(alpha==0) alpha=255;//If user specified an alpha value, then allow it, otherwise set to 255

		Instance.Apply(x,y,size,region,Color.FromArgb((int)(0xFF000000|LineColor.Value)),alpha);
	}
}