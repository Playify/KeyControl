using System.Diagnostics;
using KeyControl.Util;
using PlayifyUtility.Windows.Features.Interact;
using SharpDX.DirectInput;

namespace KeyControl.Features.Games;

public static partial class ControllerAsMouse{
	private static Joystick? _joystick;
	private static long _time;
	private static float _subX,_subY;


	private static readonly ControllerCustomButton ButtonX=new(null,s=>s.Buttons[2]);
	private static readonly ControllerCustomButton[] AllButtons={
		new("A",Keys.LButton,s=>s.Buttons[0]),//A
		new("B",Keys.RButton,s=>s.Buttons[1]),//B
		ButtonX,//X
		new("Y",Keys.MButton,s=>s.Buttons[3]),//Y
		new("LB",Keys.XButton1,s=>s.Buttons[4]),//LB
		new("RB",Keys.XButton2,s=>s.Buttons[5]),//RB
		new("Back","{Ctrl+Win+F}",s=>s.Buttons[6]),//Back
		new("Home","{F1}",s=>s.Buttons[7]),//Home
		new("DPadUp","{Up}",s=>s.PointOfViewControllers[0] is not -1 and (>=31500 or <=4500)),//DPad Up
		new("DPadLeft","{Left}",s=>s.PointOfViewControllers[0] is >=22500 and <=31500),//DPad Left
		new("DPadDown","{Down}",s=>s.PointOfViewControllers[0] is >=13500 and <=22500),//DPad Down
		new("DPadRight","{Right}",s=>s.PointOfViewControllers[0] is >=4500 and <=13500),//DPad Right
	};


	private static void SelectJoystick(Joystick? joystick){
		if(joystick==_joystick) return;
		_joystick=joystick;
		_time=Stopwatch.GetTimestamp();

		if(joystick!=null) return;//Will be handled by the next call to handleActive

		var send=new Send();
		foreach(var button in AllButtons) button.Update(send,false);
		send.SendOn(Utils.UiThread);

		_subX=_subY=0;
	}

	private static void HandleActive(JoystickState state){
		var deltaTime=Stopwatch.GetTimestamp()-_time;
		_time+=deltaTime;

		var send=new Send();

		foreach(var button in AllButtons) button.Update(send,state);


		var xButton=ButtonX.IsDown;
		var factor=(float)deltaTime/TimeSpan.TicksPerSecond/short.MaxValue;
		var speed=(xButton?2000:600f)*factor;
		var speed2=(xButton?350f:150f)*factor;

		_subX+=(state.X-short.MaxValue)*speed+(state.RotationX-short.MaxValue)*speed2;
		_subY+=(state.Y-short.MaxValue)*speed+(state.RotationY-short.MaxValue)*speed2;
		var mx=(int)Math.Round(_subX,MidpointRounding.AwayFromZero);
		var my=(int)Math.Round(_subY,MidpointRounding.AwayFromZero);

		if(mx!=0||my!=0){
			_subX-=mx;
			_subY-=my;
			send.MouseMoveRelative(mx,my);
		}


		var z=(state.Z-short.MaxValue)/700;
		if(z!=0) send.MouseScroll(z);

		send.SendOn(Utils.UiThread);
	}
}