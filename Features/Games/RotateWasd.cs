using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Games;

[InitOnLoad]
public static class RotateWasd{
	private static bool _enabled;
	private static readonly ConfigValue<int> Direction=ConfigValue.Create(0,"Games","Wasd").Listen(d=>{
		if(_enabled==(d!=0)) return;
		_enabled^=true;
		Utils.UiThread.Invoke(()=>{
			if(_enabled){
				_physical=(Modifiers.IsKeyDown(Keys.W),Modifiers.IsKeyDown(Keys.A),Modifiers.IsKeyDown(Keys.S),Modifiers.IsKeyDown(Keys.D));
				_logical=_physical;
				GlobalKeyboardHook.KeyDown+=KeyDown;
				GlobalKeyboardHook.KeyUp+=KeyUp;
			} else{
				GlobalKeyboardHook.KeyDown-=KeyDown;
				GlobalKeyboardHook.KeyUp-=KeyUp;
			}
			UpdateState();
		});
	});

	private static (bool w,bool a,bool s,bool d) _physical;
	private static (bool w,bool a,bool s,bool d) _logical;

	private static void KeyDown(KeyEvent e){
		if(Direction.Value==0||e.Handled) return;
		switch(e.Key){
			case Keys.W:
				_physical.w=true;
				break;
			case Keys.A:
				_physical.a=true;
				break;
			case Keys.S:
				_physical.s=true;
				break;
			case Keys.D:
				_physical.d=true;
				break;
			default:return;
		}
		e.Handled=true;
		UpdateState();
	}

	private static void KeyUp(KeyEvent e){
		switch(e.Key){
			case Keys.W:
				_physical.w=false;
				break;
			case Keys.A:
				_physical.a=false;
				break;
			case Keys.S:
				_physical.s=false;
				break;
			case Keys.D:
				_physical.d=false;
				break;
			default:return;
		}
		e.Handled=true;
		UpdateState();
	}

	private static void UpdateState(){
		var send=new Send().Hide();

		var modded=Rotate(_physical,Direction.Value);
		if(_logical.w!=modded.w) send.Key(Keys.W,modded.w);
		if(_logical.a!=modded.a) send.Key(Keys.A,modded.a);
		if(_logical.s!=modded.s) send.Key(Keys.S,modded.s);
		if(_logical.d!=modded.d) send.Key(Keys.D,modded.d);

		send.SendNow();
		_logical=modded;
	}

	private static (bool w,bool a,bool s,bool d) Rotate((bool w,bool a,bool s,bool d) wasd,int direction){
		var (w,a,s,d)=wasd;

		if((direction&4)!=0) (w,a,s,d)=(s,d,w,a);
		if((direction&2)!=0) (w,a,s,d)=(a,s,d,w);
		if((direction&1)!=0){
			(w,a,s,d)=(w&&!s,a&&!d,s&&!w,d&&!a);//Cancels out opposed directions
			(w,a,s,d)=(w||a,a||s,s||d,d||w);
			(w,a,s,d)=(w&&!s,a&&!d,s&&!w,d&&!a);
		}

		return (w,a,s,d);
	}
}