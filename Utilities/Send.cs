using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using static System.Windows.Forms.Keys;
using static KeyControl.Utilities.Send.SendFlags;
using ModifierKeys=KeyControl.Interfaces.ModifierKeys;

namespace KeyControl.Utilities;

public class Send{
	public static IntPtr ProcessHandle=Process.GetCurrentProcess().Handle;
	public static SendFlags SetDown(SendFlags flags,bool down)=>(flags&~KeyPress)|(down?KeyDown:KeyUp);
	public static SendFlags SetHidden(SendFlags flags,bool hidden)=>hidden?flags|Hidden:flags&~Hidden;

	#region Variables
	[Flags]
	public enum SendFlags{
		KeyDown=1,
		KeyUp=2,
		KeyPress=3,
		Hidden=4,
	}

	private static readonly Keys[] Extended={
		RMenu,RControlKey,Insert,Delete,Home,End,PageDown,PageUp,
		Up,Down,Left,Right,NumLock,Pause,PrintScreen,Divide,Enter,
		Apps,LWin,RWin,
		BrowserBack,BrowserFavorites,BrowserForward,BrowserForward,BrowserHome,BrowserRefresh,BrowserSearch,BrowserStop,
		VolumeDown,VolumeMute,VolumeUp,
		MediaStop,MediaNextTrack,MediaPlayPause,MediaPreviousTrack,
		LaunchApplication1,LaunchApplication2,LaunchMail,
	};

	private static readonly Keys[] MouseButtons={
		LButton,RButton,MButton,XButton1,XButton2,
	};

	private bool _hidden;
	#endregion

	#region Constructor,Restore
	private bool _hasMods;
	private Mods _startingMods;
	private Mods _currentMods;

	public Send Hide(bool hide=true){
		_hidden=hide;
		return this;
	}

	private void EnsureModsAreLoaded(){
		if(_hasMods) return;
		_hasMods=true;
		_startingMods=_currentMods=new Mods{
			Shift={
				L=Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift),
				R=Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift),
			},
			Ctrl={
				L=Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl),
				R=Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl),
			},
			Alt={
				L=Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt),
				R=Keyboard.IsKeyDown(System.Windows.Input.Key.RightAlt),
			},
			Win={
				L=Keyboard.IsKeyDown(System.Windows.Input.Key.LWin),
				R=Keyboard.IsKeyDown(System.Windows.Input.Key.RWin),
			},
		};
	}

	public Send Mark(Keys key,bool down=true){
		EnsureModsAreLoaded();
		switch(key){
			case LWin:
				_startingMods.Win.L=_currentMods.Win.L=down;
				break;
			case RWin:
				_startingMods.Win.R=_currentMods.Win.R=down;
				break;
			case LControlKey:
				_startingMods.Ctrl.L=_currentMods.Ctrl.L=down;
				break;
			case RControlKey:
				_startingMods.Ctrl.R=_currentMods.Ctrl.R=down;
				break;
			case LMenu:
				_startingMods.Alt.L=_currentMods.Alt.L=down;
				break;
			case RMenu:
				_startingMods.Alt.R=_currentMods.Alt.R=down;
				break;
			case LShiftKey:
				_startingMods.Shift.L=_currentMods.Shift.L=down;
				break;
			case RShiftKey:
				_startingMods.Shift.R=_currentMods.Shift.R=down;
				break;
		}
		return this;
	}

	public Mods Modifiers{
		get{
			EnsureModsAreLoaded();
			return _currentMods;
		}
		set{
			EnsureModsAreLoaded();
			if(value.Win.L!=_currentMods.Win.L) Key(LWin,value.Win.L);
			if(value.Win.R!=_currentMods.Win.R) Key(RWin,value.Win.R);

			if(value.Ctrl.L!=_currentMods.Ctrl.L) Key(LControlKey,value.Ctrl.L);
			if(value.Ctrl.R!=_currentMods.Ctrl.R) Key(RControlKey,value.Ctrl.R);

			if(value.Alt.L!=_currentMods.Alt.L) Key(LMenu,value.Alt.L);
			if(value.Alt.R!=_currentMods.Alt.R) Key(RMenu,value.Alt.R);

			if(value.Shift.L!=_currentMods.Shift.L) Key(LShiftKey,value.Shift.L);
			if(value.Shift.R!=_currentMods.Shift.R) Key(RShiftKey,value.Shift.R);
		}
	}

	public struct Lr{
		public bool L,R;
	}

	public struct Mods{
		public Lr Shift,Ctrl,Alt,Win;
	}
	#endregion


	#region Send
	private Input[] _arr=new Input[1];
	private int _length;

	private Send Add(Input input){
		if(_length==_arr.Length) Array.Resize(ref _arr,_length+1);
		_arr[_length++]=input;
		return this;
	}

	private void Remove(int cnt)=>Array.Copy(_arr,cnt,_arr,0,_length-=cnt);


	public void SendNow(){
		if(_hasMods) Modifiers=_startingMods;
		if(_length==0) return;
		var i=0;
		while(true){
			if(i==_length||_arr[i].Type==-1){
				SendInput(i,_arr,Marshal.SizeOf(typeof(Input)));
				if(i==_length){
					Remove(i);
					return;
				}
				var time=_arr[i].InputUnion.ki.Time;
				Remove(i+1);
				i=0;
				if(time!=0) Thread.Sleep(time);
				Application.DoEvents();
				continue;
			}
			i++;
		}
	}
	#endregion

	/*
	#region Send
	private readonly List<Input> _list=new List<Input>();

	private Send Add(Input input){
		_list.Add(input);
		return this;
	}

	public void SendNow(){
		Restore();
		while(_list.Count!=0){
			var i=_list.FindIndex(input=>input.Type==-1);
			if(i==0){
				var time=_list[0].InputUnion.ki.Time;
				if(time!=0) Thread.Sleep(time);
				Application.DoEvents();
				_list.RemoveAt(0);
			} else if(i==-1){
				var array=_list.ToArray();
				SendInput(array.Length,array,Marshal.SizeOf(typeof(Input)));
				_list.Clear();
				return;
			} else{
				var array=_list.ToArray();
				SendInput(i,array,Marshal.SizeOf(typeof(Input)));
				var time=_list[i].InputUnion.ki.Time;
				if(time!=0) Thread.Sleep(time);
				Application.DoEvents();
				_list.RemoveRange(0,i+1);
			}
		}
	}
	#endregion*/

	#region Primitives
	public Send Unicode(char c,SendFlags flags)
		=>(flags&KeyPress)!=KeyPress
		  ?Add(new Input{
			  Type=1,
			  InputUnion=new Inputunion{
				  ki=new KeyBdInput{
					  WVk=0,
					  WScan=(short) c,
					  Time=0,
					  DwFlags=flags.HasFlag(KeyDown)?4:6,
					  DwExtraInfo=flags.HasFlag(Hidden)||_hidden?ProcessHandle:IntPtr.Zero,
				  },
			  },
		  })
		  :Unicode(c,flags&~KeyUp).Unicode(c,flags&~KeyDown);

	public Send Key(Keys key,SendFlags flags)
		=>(flags&KeyPress)!=KeyPress
		  ?MouseButtons.Contains(key)
		   ?Mouse(key,flags)
		   :Add(new Input{
			   Type=1,
			   InputUnion=new Inputunion{
				   ki=new KeyBdInput{
					   WVk=(short) key,
					   WScan=MapVirtualKey((short) key,0),
					   Time=0,
					   DwFlags=(flags.HasFlag(KeyDown)?0:2)|(Extended.Contains(key)?1:0),
					   DwExtraInfo=flags.HasFlag(Hidden)||_hidden?ProcessHandle:IntPtr.Zero,
				   },
			   },
		   })
		  :Key(key,flags&~KeyUp).Key(key,flags&~KeyDown);

	private Send Mouse(Keys key,SendFlags flags)
		=>(flags&KeyPress)!=KeyPress
		  ?Add(new Input{
			  Type=0,
			  InputUnion=new Inputunion{
				  mi=new MouseInput{
					  Dx=0,
					  Dy=0,
					  MouseData=key switch{
						  XButton1=>1,
						  XButton2=>2,
						  _=>0,
					  },
					  Time=0,
					  DwFlags=key switch{
						  LButton=>flags.HasFlag(KeyDown)?0x2:0x4,
						  RButton=>flags.HasFlag(KeyDown)?0x8:0x10,
						  MButton=>flags.HasFlag(KeyDown)?0x20:0x40,
						  XButton1=>flags.HasFlag(KeyDown)?0x80:0x100,
						  XButton2=>flags.HasFlag(KeyDown)?0x80:0x100,
						  var _=>throw new ArgumentOutOfRangeException(nameof(key),key,null),
					  },
					  DwExtraInfo=flags.HasFlag(Hidden)||_hidden?ProcessHandle:IntPtr.Zero,
				  },
			  },
		  })
		  :Mouse(key,flags&~KeyUp).Mouse(key,flags&~KeyDown);

	public Send Wait(int delay=0)=>delay<0?this:Add(new Input{Type=-1,InputUnion={ki=new KeyBdInput{Time=delay}}});
	#endregion

	#region Mouse
	public Send MouseMove(int x,int y,bool relative=false,bool hidden=false){
		if(!relative){
			var size=Screen.PrimaryScreen.Bounds.Size;
			x=x*65536/size.Width;//GetSystemMetrics(78);//SM_CXVIRTUALSCREEN
			y=y*65536/size.Height;//GetSystemMetrics(79);//SM_CYVIRTUALSCREEN
		}

		return Add(new Input{
			Type=0,
			InputUnion=new Inputunion{
				mi=new MouseInput{
					Dx=x,
					Dy=y,
					MouseData=0,
					Time=0,
					DwFlags=relative?1:0x8001,//relative ? Move : Move|Absolute
					DwExtraInfo=hidden||_hidden?ProcessHandle:IntPtr.Zero,
				},
			},
		});
	}

	public Send Click(Keys mouseButton,int x,int y,bool relative=false,bool hidden=false)
		=>MouseMove(x,y,relative,hidden)
		.Key(mouseButton,null,hidden);

	public Send Click(int x,int y,bool relative=false,bool hidden=false)
		=>MouseMove(x,y,relative,hidden)
		.Key(LButton,null,hidden);

	public Send ClickRight(Keys mouseButton,int x,int y,bool relative=false,bool hidden=false)
		=>MouseMove(x,y,relative,hidden)
		.Key(RButton,null,hidden);
	#endregion

	#region Advanced
	public Send Text(string s,bool hidden=false){
		var flags=SetHidden(KeyPress,hidden);
		foreach(var c in s.Replace("\r\n","\n")) Char(c,flags);
		return this;
	}

	public Send Char(char c,SendFlags flags){
		if(c=='\n') c='\r';

		int num1=VkKeyScan(c);
		if(num1==-1) return Unicode(c,flags);
		Mod(ModifierKeys.Shift,(num1&256)!=0);
		Mod(ModifierKeys.Control,(num1&512)!=0);
		Mod(ModifierKeys.Alt,(num1&1024)!=0);
		Mod(ModifierKeys.Windows,false);
		Key((Keys) (num1&255),flags);
		return this;
	}

	public Send Mod(ModifierKeys keys,SendFlags flags){
		if((flags&KeyPress)==KeyPress){
			Mod(ModifierKeys.Shift,keys.HasFlag(ModifierKeys.Shift));
			Mod(ModifierKeys.Control,keys.HasFlag(ModifierKeys.Control));
			Mod(ModifierKeys.Alt,keys.HasFlag(ModifierKeys.Alt));
			Mod(ModifierKeys.Windows,keys.HasFlag(ModifierKeys.Windows));
			return this;
		}
		EnsureModsAreLoaded();
		var b=flags.HasFlag(KeyDown);

		if((keys&ModifierKeys.AltGr)==ModifierKeys.AltGr){
			Console.WriteLine("Send.Mod(ModifierKeys.AltGr,...) is not Supported");//TODO why??
			return this;
		}
		if(keys.HasFlag(ModifierKeys.Shift))
			if(b!=(_currentMods.Shift.L||_currentMods.Shift.R))
				if(b)
					if(_startingMods.Shift.R) Key(RShiftKey,SetDown(flags,_currentMods.Shift.R=true));
					else Key(LShiftKey,SetDown(flags,_currentMods.Shift.L=true));
				else{
					if(_currentMods.Shift.L) Key(LShiftKey,SetDown(flags,_currentMods.Shift.L=false));
					if(_currentMods.Shift.R) Key(RShiftKey,SetDown(flags,_currentMods.Shift.R=false));
				}
		if(keys.HasFlag(ModifierKeys.Control))
			if(b!=(_currentMods.Ctrl.L||_currentMods.Ctrl.R))
				if(b)
					if(_startingMods.Ctrl.R) Key(RControlKey,SetDown(flags,_currentMods.Ctrl.R=true));
					else Key(LControlKey,SetDown(flags,_currentMods.Ctrl.L=true));
				else{
					if(_currentMods.Ctrl.L) Key(LControlKey,SetDown(flags,_currentMods.Ctrl.L=false));
					if(_currentMods.Ctrl.R) Key(RControlKey,SetDown(flags,_currentMods.Ctrl.R=false));
				}
		if(keys.HasFlag(ModifierKeys.Alt))
			if(b!=(_currentMods.Alt.L||_currentMods.Alt.R))
				if(b)
					if(_startingMods.Alt.L&&!keys.HasFlag(ModifierKeys.AltGr)) Key(LMenu,SetDown(flags,_currentMods.Alt.L=true));
					else Key(RMenu,SetDown(flags,_currentMods.Alt.R=true));
				else{
					if(_currentMods.Alt.L) Key(LMenu,SetDown(flags,_currentMods.Alt.L=false));
					if(_currentMods.Alt.R) Key(RMenu,SetDown(flags,_currentMods.Alt.R=false));
				}
		if(keys.HasFlag(ModifierKeys.Windows))
			if(b!=(_currentMods.Win.L||_currentMods.Win.R))
				if(b)
					if(_startingMods.Win.R) Key(RWin,SetDown(flags,_currentMods.Win.R=true));
					else Key(LWin,SetDown(flags,_currentMods.Win.L=true));
				else{
					if(_currentMods.Win.L) Key(LWin,SetDown(flags,_currentMods.Win.L=false));
					if(_currentMods.Win.R) Key(RWin,SetDown(flags,_currentMods.Win.R=false));
				}


		return this;
	}
	#endregion

	#region Shortcuts
	public Send Char(char c,bool? down=null,bool hidden=false)=>Char(c,SetHidden(down.HasValue?down.Value?KeyDown:KeyUp:KeyPress,hidden));

	public Send Key(Keys c,bool? down=null,bool hidden=false)=>Key(c,SetHidden(down.HasValue?down.Value?KeyDown:KeyUp:KeyPress,hidden));

	public Send Mod(ModifierKeys c,bool? down=null,bool hidden=false)=>Mod(c,SetHidden(down.HasValue?down.Value?KeyDown:KeyUp:KeyPress,hidden));

	public Send Key(Keys key,int cnt,int delay=0,bool hidden=false){
		while(cnt-->0) Key(key,SetHidden(KeyPress,hidden)).Wait(delay);
		return this;
	}
	#endregion

	#region DLL Imports
	[DllImport("user32.dll")]
	private static extern uint SendInput(int cInputs,Input[] pInputs,int cbSize);


	[DllImport("user32.dll",CharSet=CharSet.Auto)]
	private static extern short VkKeyScan(char key);

	[DllImport("user32.dll",CharSet=CharSet.Auto)]
	private static extern short MapVirtualKey(short uCode,short uMapType);


	public struct MouseInput{
		public int Dx;
		public int Dy;
		public int MouseData;
		public int DwFlags;
		public int Time;
		public IntPtr DwExtraInfo;
	}

	public struct KeyBdInput{
		public short WVk;
		public short WScan;
		public int DwFlags;
		public int Time;
		public IntPtr DwExtraInfo;
	}

	public struct HardwareInput{
		public int UMsg;
		public short WParamL;
		public short WParamH;
	}

	public struct Input{
		public int Type;
		public Inputunion InputUnion;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct Inputunion{
		[FieldOffset(0)]
		public MouseInput mi;
		[FieldOffset(0)]
		public KeyBdInput ki;
		[FieldOffset(0)]
		public HardwareInput hi;
	}

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int smIndex);
	#endregion
}