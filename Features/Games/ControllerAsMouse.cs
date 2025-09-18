using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.HelperClasses;
using PlayifyUtility.Windows;
using SharpDX;
using SharpDX.DirectInput;
using Timer=System.Windows.Forms.Timer;

namespace KeyControl.Features.Games;

[InitOnLoad]
public static partial class ControllerAsMouse{
	private static readonly UiThread TimerThread=UiThread.Create(nameof(CrossHair));
	private static readonly Timer Timer=new(){Interval=1000};
	private static readonly ConfigValue<bool> Enabled=ConfigValue.Create(true,"Games","ControllerAsMouse","Enabled").Listen(b=>{
		TimerThread.Invoke(()=>{
			Timer.Enabled=b;
			CheckControllers();
		});
	});
	private static readonly Dictionary<Guid,ReferenceTo<bool>> Running=new();
	private static readonly DirectInput DirectInput=new();

	static ControllerAsMouse()=>Timer.Tick+=(_,_)=>CheckControllers();


	private static void CheckControllers(){
		if(!Enabled.Value){
			lock(Running) Running.Clear();
			lock(DirectInput)
				if(_joystick!=null){
					Console.WriteLine("Disabled Controller as Mouse");
					SelectJoystick(null);
				}
			return;
		}
		var list=DirectInput.GetDevices(DeviceType.Gamepad,DeviceEnumerationFlags.AttachedOnly);

		lock(Running){
			var remove=new List<Guid>();
			foreach(var (key,value) in Running){
				if(list.Any(d=>d.InstanceGuid==key))
					continue;
				value.Value=false;
				remove.Add(key);
			}
			remove.ForEach(g=>Running.Remove(g));


			foreach(var device in list){
				ReferenceTo<bool> r;
				lock(Running){
					if(Running.ContainsKey(device.InstanceGuid)) return;
					Running.Add(device.InstanceGuid,r=new ReferenceTo<bool>(true));
				}
				new Thread(()=>Loop(device,r)){Name="ControllerAsMouse: "+device.InstanceName,IsBackground=true}.Start();
			}
		}
	}


	private static void Loop(DeviceInstance device,ReferenceTo<bool> valid){
		Joystick joystick;
		try{
			joystick=new Joystick(DirectInput,device.InstanceGuid);
			joystick.Acquire();
		} catch(SharpDXException){
			return;
		}
		Console.WriteLine("[ControllerAsMouse] Connected: \""+device.InstanceName+"\"");

		var bothDown=false;
		var state=new JoystickState();
		try{
			while(valid.Value){
				joystick.GetCurrentState(ref state);

				if(!state.Buttons[8]||!state.Buttons[9]) bothDown=false;
				else if(!bothDown){
					bothDown=true;
					lock(DirectInput)
						if(_joystick==joystick){
							Console.WriteLine("Disabled Controller as Mouse");
							SelectJoystick(null);
						} else{
							Console.WriteLine("Enabled Controller as Mouse: \""+device.InstanceName+"\"");
							SelectJoystick(joystick);
						}
				}

				bool active;
				lock(DirectInput) active=_joystick==joystick;
				if(active) HandleActive(state);

				Thread.Sleep(8);
			}
			Console.WriteLine("[ControllerAsMouse] Disconnected: \""+device.InstanceName+"\"");
		} catch(SharpDXException e) when(e.Descriptor.Code==unchecked((int)0x8007001e)){//0x8007001E=InputLost
			Console.WriteLine("[ControllerAsMouse] Disconnected: \""+device.InstanceName+"\"");
		} catch(Exception e){
			Console.WriteLine("[ControllerAsMouse] Disconnected: \""+device.InstanceName+"\" due to "+e);
		} finally{
			lock(DirectInput)
				if(_joystick==joystick){
					Console.WriteLine("Disabled Controller as Mouse");
					SelectJoystick(null);
				}

			valid.Value=false;
			lock(Running){
				if(Running.Remove(device.InstanceGuid,out var found)&&found!=valid)
					Running.Add(device.InstanceGuid,found);//If found other, then put back
			}

			joystick.Dispose();
		}
	}
}