using System;
using System.IO;
using System.Windows.Forms;
using KeyControl.Hooks;
using KeyControl.HotKeyHandler;
using KeyControl.Utilities;

namespace KeyControl.Interfaces{
	public static class Logger{
		public enum Action{
			None,
			Packet,
			Ctrl,
			Char,
			Special,
			Internal,
			Mouse
		}

		private static readonly string _file="log.html";
		private static readonly StreamWriter LogStream;
		private static Action _last=Action.None;

		static Logger(){
			if(!Config.Constant.EnableLogging) return;
			if(File.Exists(_file)) LogStream=File.AppendText(_file);
			else{
				LogStream=File.CreateText(_file);
				Log(Action.None,"<style>"+
				                "body{word-break:break-all;white-space:pre-wrap;background:#323232;font-family: \"Helvetica\", \"Arial\", sans-serif}"+
				                "p{display:inline;margin:0}"+
				                ".special{color:#ff8d1c}"+
				                ".ctrl{color:red;margin:0 2px;font-weight:bold}"+
				                ".packet{color:purple}"+
				                ".char{color:#6a8759}"+
				                ".mouse{color:yellow;margin:0 2px;font-weight:bold}"+
				                "</style>");
			}
			LogStream.AutoFlush=true;
			AppDomain.CurrentDomain.ProcessExit+=(sender,args)=>Log(Action.Internal,"EXIT");
		}

		private static void Log(Action action,string s){
			if(!Config.Constant.EnableLogging) return;
			Scheduler.RunLater(0,()=>{
				if(action!=_last){
					if(_last!=Action.None) LogStream.Write("</p>");
					if(action!=Action.None){
						LogStream.Write("<p class=\"");
						LogStream.Write(action.ToString().ToLowerInvariant());
						LogStream.Write("\">");
					}
					_last=action;
				}
				LogStream.Write(s);
			});
		}

		public static void LogPacket(KeyEvent e){
			//Log(Action.Packet,"█");
			LogChars(char.ToString((char)e.ScanCode),e,1);
		}

		public static void LogControl(Keys key){
			HotStringHandler.Reset();

			Log(Action.Ctrl,key.ToString());
			Log(Action.None,"");
		}

		public static void LogMouse(MouseButtons button){
			Log(Action.Mouse,button.ToString());
			Log(Action.None,"");
		}

		public static bool LogChars(string s,KeyEvent e,int deleteCount=1){
			var s2=s.Replace("&","&amp;");
			s2=s2.Replace("<","&lt;");
			s2=s2.Replace(">","&gt;");
			Log(Action.Char,s2);
			return HotStringHandler.Write(s,e,deleteCount);
		}

		public static void Init()=>Log(Action.Internal,"START");
	}
}