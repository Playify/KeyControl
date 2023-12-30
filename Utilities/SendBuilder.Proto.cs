using System.Text;
using System.Windows.Forms;
using PlayifyUtils.Utils;

namespace KeyControl.Utilities;

public partial class SendBuilder{
	private abstract class ProtoBase{
		public abstract override string ToString();
		public abstract void AddToSend(Send send);

		public abstract Keys? AsSingleKey();
	}

	private class ProtoKey:ProtoBase{
		private readonly bool? _down;
		private readonly Keys _keys;

		public ProtoKey(Keys keys,bool? down){
			_keys=keys;
			_down=down;
		}

		public override string ToString(){
			var s=KeyToString(_keys);
			if(_down.TryGet(out var down)) s+=" "+(down?"down":"up");
			return "{"+s+"}";
		}

		public override void AddToSend(Send send)=>send.Key(_keys,_down);

		public override Keys? AsSingleKey()=>_down.HasValue?null:_keys;
	}

	private class ProtoText:ProtoBase{
		private readonly string _text;

		public ProtoText(string s)=>_text=s.Replace("\r","");

		public override string ToString()
			=>_text
			  //.Replace("\r","")
			  //.Replace("\n",@"\n")
			  //.Replace("\t",@"\t")
			  .Replace("{",@"\{")
			  .Replace("}",@"\}")
			  .Replace("\\",@"\\")
			  .Replace("\b",@"\b");

		public override void AddToSend(Send send)=>send.Text(_text);
		public override Keys? AsSingleKey()=>null;
	}

	private class ProtoMultiKey:ProtoBase{
		private readonly Keys _keys;
		private readonly int _times;
		private readonly int? _wait;

		public ProtoMultiKey(Keys keys,int times,int? wait=null){
			_keys=keys;
			_times=times;
			_wait=wait;
		}

		public override string ToString()=>"{"+KeyToString(_keys)+" "+_times+(_wait.TryGet(out var wait)?" "+wait:"")+"}";

		public override void AddToSend(Send send)=>send.Key(_keys,_times,_wait.GetValueOrDefault(0));
		public override Keys? AsSingleKey()=>_times!=1?null:_keys;
	}

	private class ProtoKeyCombo:ProtoBase{
		private readonly ModifierKeys _mods;
		private readonly Keys _keys;
		private readonly int? _times;
		private readonly int? _wait;

		public ProtoKeyCombo(ModifierKeys mods,Keys keys,int? times=null,int? wait=null){
			_mods=mods;
			_keys=keys;
			_times=times;
			_wait=wait;
		}

		public override string ToString(){
			var s=new StringBuilder("{");
			var altGr=(_mods&ModifierKeys.AltGr)==ModifierKeys.AltGr;
			if(!altGr&&(_mods&ModifierKeys.Control)!=0) s.Append("Ctrl+");
			if((_mods&ModifierKeys.Shift)!=0) s.Append("Shift+");
			if(!altGr&&(_mods&ModifierKeys.Alt)!=0) s.Append("Alt+");
			if(altGr) s.Append("AltGr+");
			if((_mods&ModifierKeys.Windows)!=0) s.Append("Win+");
			s.Append(KeyToString(_keys));
			if(_times.TryGet(out var times)){
				s.Append(' ').Append(times);
				if(_wait.TryGet(out var wait)) s.Append(' ').Append(wait);
			} else if(_wait.TryGet(out var wait)) s.Append(" 1 ").Append(wait);
			s.Append("}");
			return s.ToString();
		}

		public override void AddToSend(Send send){
			var mods=send.Modifiers;
			send.Mod(_mods);
			send.Key(_keys,_times.GetValueOrDefault(1),_wait.GetValueOrDefault(0));
			send.Modifiers=mods;
		}

		public override Keys? AsSingleKey()=>_mods!=0||_times!=1?null:_keys;
	}

	private class ProtoComment:ProtoBase{
		private readonly string _s;

		public ProtoComment(string s)=>_s=s;

		public override string ToString()=>_s;

		public override void AddToSend(Send send){
		}

		public override Keys? AsSingleKey()=>null;
	}
}