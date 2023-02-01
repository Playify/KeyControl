using System;
using System.Collections.Generic;
using System.Windows.Forms;
using KeyControl.Interfaces;
using PlayifyUtils.Utils;

namespace KeyControl.Utilities;

public partial class SendBuilder{
	private readonly List<ProtoBase> _list=new();

	public SendBuilder(){
	}

	public SendBuilder(string s)=>Parse(s);

	public override string ToString()=>_list.Join("");

	public SendBuilder Parse(string s){
		/*if(_list.Count==0&&s.StartsWith("{raw}",StringComparison.OrdinalIgnoreCase)){
			return Proto(new ProtoComment(s.Substring(0,5))).Text(s.Substring(5));
		}*///would not work, because {} would get escaped
		var escape=false;
		var curr="";
		var special=false;
		foreach(var c in s){
			if(escape){
				escape=false;
				switch(c){
					case 't':
						curr+='\t';
						break;
					case 'n':
						curr+='\n';
						break;
					case 'b':
						curr+='\b';
						break;
					case '\n':
						if(curr!=""){
							if(special) throw new ArgumentException("Can't use \\⏎ in Special Block");
							Text(curr);
							curr="";
						}
						Proto(new ProtoComment("\\\n"));
						break;
					default:throw new ArgumentException("Illegal Escape Sequence \\"+c);
				}
				continue;
			}
			if(c=='\\'){
				escape=true;
				continue;
			}
			if(special){
				if(c=='{') throw new ArgumentException("Illegal {");
				if(c=='}'){
					special=false;
					Special(curr);
					curr="";
					continue;
				}
			} else{
				if(c=='}') throw new ArgumentException("Illegal }");
				if(c=='{'){
					special=true;
					if(curr!=""){
						Text(curr);
						curr="";
					}
					continue;
				}
			}
			curr+=c;
		}
		if(special) throw new ArgumentException("Unclosed { on end of string");
		if(escape) throw new ArgumentException("Escaped on end of string");
		if(curr!="") Text(curr);
		return this;
	}

	private SendBuilder Special(string s){
		s=s.Trim();
		if(s.StartsWith("#")) return Proto(new ProtoComment("{"+s+"}"));
		var args=s.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
		switch(args.Length){
			case 1:{
				var mods=ModsOf(ref args[0]);
				return KeyCombo(mods,StringToKey(args[0]));
			}
			case 2:{
				//TODO variables

				var mods=ModsOf(ref args[0]);
				var key=StringToKey(args[0]);

				var arg1=args[1];

				if(int.TryParse(arg1,out var times)&&times>=0)
					if(mods!=0) return KeyCombo(mods,key,times);
					else return Key(key,times);
				if(mods!=0) throw new ArgumentException("KeyCombo can only be used with {<combo> [times] [wait]}");
				if(bool.TryParse(arg1,out var down)) return Key(key,down);
				if(arg1.Equals("press",StringComparison.OrdinalIgnoreCase)) return Key(key);
				if(arg1.Equals("down",StringComparison.OrdinalIgnoreCase)) return Key(key,true);
				if(arg1.Equals("up",StringComparison.OrdinalIgnoreCase)) return Key(key,false);
				throw new ArgumentException($"Unknown Special Block: {{{s}}}");
			}
			case 3:{
				//TODO variables

				var mods=ModsOf(ref args[0]);
				var key=StringToKey(args[0]);
				if(int.TryParse(args[1],out var times)&&times>=0&&int.TryParse(args[2],out var wait)&&wait>=0)
					if(mods!=0) return KeyCombo(mods,key,times,wait);
					else return Key(key,times,wait);
				throw new ArgumentException($"Unknown Special Block: {{{s}}}");
			}
			default:{
				throw new ArgumentException($"Unknown Special Block: {{{s}}}");
			}
		}
	}

	public static Keys StringToKey(string s){
		if(Enum.TryParse(s,true,out Keys key)){
			switch(key){
				case Keys.Control:
				case Keys.ControlKey:return Keys.LControlKey;
				case Keys.Shift:
				case Keys.ShiftKey:return Keys.LShiftKey;
				case Keys.KeyCode:
				case Keys.Modifiers:throw new ArgumentException($"Illegal Key: {{{key}}}");
				case Keys.Menu:return Keys.LMenu;
			}
			return key;
		}

		if(s.Equals("ArrowUp",StringComparison.OrdinalIgnoreCase)) return Keys.Up;
		if(s.Equals("ArrowDown",StringComparison.OrdinalIgnoreCase)) return Keys.Down;
		if(s.Equals("ArrowLeft",StringComparison.OrdinalIgnoreCase)) return Keys.Left;
		if(s.Equals("ArrowRight",StringComparison.OrdinalIgnoreCase)) return Keys.Right;
		if(s.Equals("AppsKey",StringComparison.OrdinalIgnoreCase)) return Keys.Apps;
		if(s.Equals("Esc",StringComparison.OrdinalIgnoreCase)) return Keys.Escape;
		if(s.Equals(",",StringComparison.OrdinalIgnoreCase)) return Keys.Oemcomma;
		if(s.Equals(".",StringComparison.OrdinalIgnoreCase)) return Keys.OemPeriod;
		if(s.Equals("-",StringComparison.OrdinalIgnoreCase)) return Keys.OemMinus;

		if(s.Equals("Ctrl",StringComparison.OrdinalIgnoreCase)) return Keys.LControlKey;
		if(s.Equals("LCtrl",StringComparison.OrdinalIgnoreCase)) return Keys.LControlKey;
		if(s.Equals("LControl",StringComparison.OrdinalIgnoreCase)) return Keys.LControlKey;
		if(s.Equals("RCtrl",StringComparison.OrdinalIgnoreCase)) return Keys.RControlKey;
		if(s.Equals("RControl",StringComparison.OrdinalIgnoreCase)) return Keys.RControlKey;

		if(s.Equals("Alt",StringComparison.OrdinalIgnoreCase)) return Keys.LMenu;
		if(s.Equals("LAlt",StringComparison.OrdinalIgnoreCase)) return Keys.LMenu;
		if(s.Equals("RAlt",StringComparison.OrdinalIgnoreCase)) return Keys.RMenu;

		if(s.Equals("Win",StringComparison.OrdinalIgnoreCase)) return Keys.LWin;
		if(s.Equals("Windows",StringComparison.OrdinalIgnoreCase)) return Keys.LWin;
		if(s.Equals("LWindows",StringComparison.OrdinalIgnoreCase)) return Keys.LWin;
		if(s.Equals("RWindows",StringComparison.OrdinalIgnoreCase)) return Keys.RWin;

		throw new ArgumentException("Unknown Key: "+s);
	}

	public static string KeyToString(Keys keys){
		switch(keys){
			case Keys.Apps:return "AppsKey";
			case Keys.LMenu:return "LAlt";
			case Keys.RMenu:return "RAlt";
			case Keys.LControlKey:return "LCtrl";
			case Keys.RControlKey:return "RCtrl";
			case Keys.Escape:return "Esc";
			case Keys.Oemcomma:return ",";
			case Keys.OemPeriod:return ".";
			case Keys.OemMinus:return "-";
		}
		return keys.ToString();
	}

	public static ModifierKeys ModsOf(ref string s){
		var i=s.IndexOf('+');
		if(i==-1) return 0;
		var mod=s.Substring(0,i).ToLowerInvariant() switch{
			"shift"=>ModifierKeys.Shift,
			"ctrl"=>ModifierKeys.Control,
			"control"=>ModifierKeys.Control,
			"win"=>ModifierKeys.Windows,
			"windows"=>ModifierKeys.Windows,
			"alt"=>ModifierKeys.Alt,
			"altgr"=>ModifierKeys.AltGr,
			_=>throw new ArgumentException("Illegal Modifier Key: "+s.Substring(0,i)),
		};
		s=s.Substring(i+1);
		var other=ModsOf(ref s);
		if((other&mod)!=0) throw new ArgumentException("Duplicate Modifier Key: "+mod);
		return mod|other;
	}

	private SendBuilder Proto(ProtoBase proto){
		_list.Add(proto);
		return this;
	}

	public SendBuilder Key(Keys keys,bool? down=null)=>Proto(new ProtoKey(keys,down));

	public SendBuilder Key(Keys keys,int times)=>Proto(new ProtoMultiKey(keys,times));
	public SendBuilder Key(Keys keys,int times,int wait)=>Proto(new ProtoMultiKey(keys,times,wait));

	public SendBuilder KeyCombo(ModifierKeys mods,Keys keys)=>Proto(new ProtoKeyCombo(mods,keys));
	public SendBuilder KeyCombo(ModifierKeys mods,Keys keys,int times)=>Proto(new ProtoKeyCombo(mods,keys,times));
	public SendBuilder KeyCombo(ModifierKeys mods,Keys keys,int times,int wait)=>Proto(new ProtoKeyCombo(mods,keys,times,wait));

	public SendBuilder Text(string s)=>Proto(new ProtoText(s));

	//TODO Mouse: Move, Click, Scroll
	//TODO Sleep

	public Send Build(Send send=null){
		send??=new Send();
		foreach(var proto in _list) proto.AddToSend(send);
		return send;
	}

	public Keys? AsSingleKey(){
		var hasFirst=false;
		ProtoBase firstProto=null;
		foreach(var proto in _list){
			if(proto is ProtoComment) continue;
			if(hasFirst) return null;//more then 2

			firstProto=proto;
			hasFirst=true;
		}

		return firstProto?.AsSingleKey();
	}
}