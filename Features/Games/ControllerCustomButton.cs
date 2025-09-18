using KeyControl.Configuration;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Windows.Features.Interact;
using SharpDX.DirectInput;

namespace KeyControl.Features.Games;

public class ControllerCustomButton{
	private readonly Func<string>? _action;
	private readonly Func<JoystickState,bool> _read;
	private Keys? _release;

	public ControllerCustomButton(string name,Keys defaultValue,Func<JoystickState,bool> read):this(name,SendBuilder.GetSingleKey(defaultValue),read){}
	public ControllerCustomButton(string name,string defaultValue,Func<JoystickState,bool> read):this(CreateConfigValue(name,defaultValue),read){}

	public ControllerCustomButton(Func<string>? action,Func<JoystickState,bool> read){
		_action=action;
		_read=read;
	}

	private static Func<string> CreateConfigValue(string name,string defaultValue){
		var cfg=ConfigValue.Create(defaultValue,"Games","ControllerAsMouse","Actions",name);
		return ()=>cfg.Value;
	}

	public bool IsDown{get;private set;}

	public void Update(Send send,JoystickState state)=>Update(send,_read(state));

	public void Update(Send send,bool b){
		if(IsDown==b) return;
		IsDown=b;
		if(!b){
			if(_release.TryGet(out var release)) send.Key(release,false);
			return;
		}
		if(_action==null) return;
		var action=_action();
		if(SendBuilder.IsSingleKey(action,out var key)){
			_release=key;
			send.Key(key,true);
		} else{
			_release=null;
			SendBuilder.Parse(action).ToSend(send);
		}
	}
}