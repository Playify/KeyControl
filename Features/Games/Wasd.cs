using System;
using System.Windows.Forms;
using KeyControl.Hooks;
using KeyControl.Interfaces;
using KeyControl.Utilities;

namespace KeyControl.Features.Games;

public static class Wasd{
	private static (bool w,bool a,bool s,bool d) _physical;
	private static (bool w,bool a,bool s,bool d) _logical;
	private static bool _enabled;

	public static int Direction;
	public static bool Enabled{
		get=>_enabled;
		set{
			if(_enabled==value) return;
			_enabled=value;

			Scheduler.RunOnMainThread(()=>{
				if(value){
					_physical=(Modifiers.IsKeyDown(Keys.W),Modifiers.IsKeyDown(Keys.A),Modifiers.IsKeyDown(Keys.S),Modifiers.IsKeyDown(Keys.D));
					_logical=_physical;
					Program.Keyboard.KeyDown+=KeyDown;
					Program.Keyboard.KeyUp+=KeyUp;

					UpdateState();
				} else{
					Program.Keyboard.KeyDown-=KeyDown;
					Program.Keyboard.KeyUp-=KeyUp;

					UpdateState();
				}
			});
		}
	}

	private static void KeyDown(object sender,KeyEvent e){
		if(!Enabled) return;
		if(e.Handled) return;
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

	private static void KeyUp(object sender,KeyEvent e){
		if(!Enabled) return;
		if(e.Handled) return;
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

		var modded=Enabled?Rotate(_physical,Direction):_physical;

		if(_logical.w!=modded.w) send.Key(Keys.W,modded.w);
		if(_logical.a!=modded.a) send.Key(Keys.A,modded.a);
		if(_logical.s!=modded.s) send.Key(Keys.S,modded.s);
		if(_logical.d!=modded.d) send.Key(Keys.D,modded.d);

		send.SendNow();
		_logical=modded;
	}

	public static void InitConfig(){
		Config.Register(nameof(Games)+"."+nameof(Wasd)+"."+nameof(Enabled),()=>Enabled,j=>Enabled=j.AsBoolean(),Config.Restrict.Website);
		Config.Register(nameof(Games)+"."+nameof(Wasd)+"."+nameof(Direction),()=>Direction,j=>Direction=(int) j.AsNumber());
	}

	public static (bool w,bool a,bool s,bool d) Rotate((bool w,bool a,bool s,bool d) wasd,int direction){
		var (w,a,s,d)=wasd;

		if(w&&s) (w,s)=(false,false);
		if(a&&d) (a,d)=(false,false);

		if((direction&4)!=0) (w,a,s,d)=(s,d,w,a);
		if((direction&2)!=0) (w,a,s,d)=(a,s,d,w);
		if((direction&1)!=0) (w,a,s,d)=(w||a,a||s,s||d,d||w);

		if(w&&s) (w,s)=(false,false);
		if(a&&d) (a,d)=(false,false);

		return (w,a,s,d);
	}

	public static void Test(){
		var wasd=(true,false,false,false);
		for(var i=0;i<=8;i++){
			Console.WriteLine(i+" "+wasd);
			wasd=Rotate(wasd,1);
		}
	}
}