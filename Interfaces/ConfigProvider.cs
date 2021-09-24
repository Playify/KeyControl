using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using AsyncFriendlyStackTrace;
using KeyControl.Hotstring;
using KeyControl.Hotstring.Complex;
using KeyControl.Hotstring.Saveable;
using PlayifyUtils.Jsons;
using PlayifyUtils.Utils;
using PlayifyUtils.Web;

namespace KeyControl.Interfaces{
	public class ConfigProvider:WebBase{
		private static readonly HashSet<WebSocket> Connected=new();

		private ConfigProvider(){
			//RunHttp(new IPEndPoint(IPAddress.Parse("127.2.4.8"),5000));
			_=RunHttpWithRetry();
		}

		private async Task RunHttpWithRetry(){
			while(true){
				try{
					await RunHttp(new IPEndPoint(IPAddress.Parse("127.2.4.8"),5000));
					return;
				} catch(Exception e){
					Console.WriteLine(e.ToAsyncString());
					await Task.Delay(10);
				}
			}
		}

		public static ConfigProvider Instance{get;}=new();

		public override bool CacheByDefault=>false;

		protected override async Task HandleRequest(WebSession session){
			if(session.Path=="/"){
				var webSocket=await session.CreateWebSocket();
				if(webSocket==null){
					var webFile=await GetWebFile("/index.html");
					if(webFile==null) await session.Send.Error(404);
					else await session.Send.Document().Set(webFile).Send();
					return;
				}
				await HandleWebSocket(webSocket);
			} else if(session.Path=="/combined"){
				var webSocket=await session.CreateWebSocket();
				if(webSocket==null){
					var webFile=await GetWebFile("/combined.html");
					if(webFile==null) await session.Send.Error(404);
					else await session.Send.Document().Set(webFile).Send();
					return;
				}
				await HandleWebSocket(webSocket);
			} else if(session.Path=="/version"){
				var version=ConfigWindow._instance.Text;

				await session.Send
				       .Header("Access-Control-Allow-Origin","*")
				       .Document()
				       .MimeType("text/plain")
				       .Set(version)
				       .Send();
			} else {
				var webFile=await GetWebFile(session.Path);
				if(webFile==null) await session.Send.Error(404);
				else await session.Send.Document().Set(webFile).MimeType(Path.GetExtension(session.Path)).Send();
			}
		}

		public static async Task HandleWebSocket(WebSocket webSocket){
			Connected.Add(webSocket);
			//var block=new uint[]{0};
			var block=new ReferenceTo<uint>();
			try{
				var list=new List<Task>();
					
				//maybe init Random, but since only one client is supposed to be connected, a randomly generated id will suffice

				foreach(var (key,tuple) in Config.ValuesWebsite)
					list.Add(webSocket.Send(key+"="+tuple.Item1()));
				foreach(var (_,json) in HotStringCategory.Master.GetRecursive()) list.Add(webSocket.Send($"hotString={json}"));

				await Task.WhenAll(list);
				await foreach(var (s,_) in webSocket)
					if(s!=null)
						_=Receive(webSocket,s,block);
			} finally{
				lock(HotStringCategory.Blocked){
					uint id=block;
					if(id!=0){
						var i=(HotStringCategory.Blocked.TryGetValue(id,out var tmp)?tmp:0)-1;
						if(i==0) HotStringCategory.Blocked.Remove(id);
						else HotStringCategory.Blocked[id]=i;
					}
				}
				Connected.Remove(webSocket);
			}
			webSocket.Close();
		}

		public static Task SendUpdate(string key){
			var tuple=Config.ValuesWebsite[key];
			var answer=key+"="+(tuple.Item1()??JsonNull.Null);
			return Task.WhenAll(Connected.Select(w=>w.Send(answer)));
		}

		private static async Task Receive(WebSocket webSocket,string s,ReferenceTo<uint> block){
			if(s.Length==0) return;
			Console.WriteLine("RECV:"+s);
			try{
				var indexOf=s.IndexOf('=');
				if(indexOf==-1) throw new ArgumentException("Illegal WebSocket Command: "+s);
				var key=s.Substring(0,indexOf);
				var valueStr=s.Substring(indexOf+1);
				var value=valueStr.Length==0?JsonNull.Null:Json.Parse(valueStr);

				if(value==null) return;

				switch(key){
					case "calc":{
						var expression=value.AsString();
						if(expression?.StartsWith("@=")??false) expression=expression.Substring(2);
						var result=expression?.Replace("\r","").Split('\n').Select(HotStringComplexCalc.Calculate).Join("\n");
						await webSocket.Send("calc="+JsonString.Escape(result));
						return;
					}
					case "flip1":{
						await webSocket.Send("flip2="+JsonString.Escape(HotStringComplexFlip.Flip(value.AsString())));
						return;
					}
					case "flip2":{
						await webSocket.Send("flip1="+JsonString.Escape(HotStringComplexFlip.Flip(value.AsString())));
						return;
					}
					case "convertBin":{
						string[] strings;
						try{
							var v=value.AsString();
							strings=new[]{("Dec",10),("Hex",16)}
							        .Select(t=>(t.Item1,HotStringComplexNumberConverter.ConvertNumber(v,2,t.Item2)))
							        .Select(t=>"convert"+t.Item1+"="+JsonString.Escape(t.Item2)).ToArray();
						} catch(Exception e){
							await webSocket.Send("err:convertBin="+JsonString.Escape(e.ToString()));
							return;
						}
						await Task.WhenAll(strings.Select(webSocket.Send));
						return;
					}
					case "convertDec":{
						string[] strings;
						try{
							var v=value.AsString();
							strings=new[]{("Bin",2),("Hex",16)}
							        .Select(t=>(t.Item1,HotStringComplexNumberConverter.ConvertNumber(v,10,t.Item2)))
							        .Select(t=>"convert"+t.Item1+"="+JsonString.Escape(t.Item2)).ToArray();
						} catch(Exception e){
							await webSocket.Send("err:convertDec="+JsonString.Escape(e.ToString()));
							return;
						}
						await Task.WhenAll(strings.Select(webSocket.Send));
						return;
					}
					case "convertHex":{
						string[] strings;
						try{
							var v=value.AsString();
							strings=new[]{("Bin",2),("Dec",10)}
							        .Select(t=>(t.Item1,HotStringComplexNumberConverter.ConvertNumber(v,16,t.Item2)))
							        .Select(t=>"convert"+t.Item1+"="+JsonString.Escape(t.Item2)).ToArray();
						} catch(Exception e){
							await webSocket.Send("err:convertHex="+JsonString.Escape(e.ToString()));
							return;
						}
						await Task.WhenAll(strings.Select(webSocket.Send));
						return;
					}
					case "unicode1":{
						string text;
						try{
							text=HotStringComplexUnicode.GetText(value.AsString());
						} catch(Exception e){
							await webSocket.Send("err:unicode1="+JsonString.Escape(e.ToString()));
							return;
						}
						await webSocket.Send("unicode2="+JsonString.Escape(text));
						return;
					}
					case "unicode2":{
						string text;
						try{
							text=HotStringComplexUnicode.GetText(value.AsString());
						} catch(Exception e){
							await webSocket.Send("err:unicode2="+JsonString.Escape(e.ToString()));
							return;
						}
						await webSocket.Send("unicode1="+JsonString.Escape(text));
						return;
					}
					case "hotString":{
						var result=HotStringBase.Update(value);

						var answer="hotString="+result;
						var includeSelf=true;
						if(value.Equals(result)) includeSelf=false;
						else if(value is JsonObject a&&result is JsonObject b&&a.EqualsIgnoreOrder(b)) includeSelf=false;
						if(includeSelf) await Task.WhenAll(Connected.Select(w=>w.Send(answer)));
						else await Task.WhenAll(Connected.Where(w=>w!=webSocket).Select(w=>w.Send(answer)));

						if(value is JsonNumber) return;//don't save file on memory clear
						break;
					}
					case "hotStringBlock":{
						JsonArray array;
						lock(HotStringCategory.Blocked){
							uint id=block;
							if(id!=0){
								var i=(HotStringCategory.Blocked.TryGetValue(id,out var tmp)?tmp:0)-1;
								if(i==0) HotStringCategory.Blocked.Remove(id);
								else HotStringCategory.Blocked[id]=i;
							}
							id=block.Value=(uint)value.AsNumber();
							if(id!=0){
								var i=(HotStringCategory.Blocked.TryGetValue(id,out var tmp)?tmp:0)+1;
								HotStringCategory.Blocked[id]=i;
							}

							array=new JsonArray(HotStringCategory.Blocked.Keys.Select(u=>(long)u));
						}
						var answer="hotStringBlock="+array;
						await Task.WhenAll(Connected.Select(w=>w.Send(answer)));
						return;
					}
					default:
						if(!Config.ValuesWebsite.TryGetValue(key,out var tuple)) throw new ArgumentException("Illegal Config Key in Command: "+s);
						if(valueStr.Length==0) await webSocket.Send(key+"="+(tuple.Item1()??JsonNull.Null));//send specific
						else{//set
							try{
								tuple.Item2(value);
							} catch(Exception e){
								Console.WriteLine(e);
								await webSocket.Send("err:"+key+"="+JsonString.Escape(e.ToString()));
								return;
							}
							var answer=key+"="+(tuple.Item1()??JsonNull.Null);
							await Task.WhenAll(Connected.Where(w=>w!=webSocket).Select(w=>w.Send(answer)));
						}
						break;
				}
				Config.Save();
			} catch(Exception e){
				Console.WriteLine(e.ToAsyncString());
			}
		}

		public static async Task<string> GetWebFile(string s){
#if DEBUG
			if(Environment.MachineName.Equals("PLAYIFY",StringComparison.OrdinalIgnoreCase)){
				const string path=@"S:\Code\C#\own\KeyControl\Resources";
				if(Directory.Exists(path)){
					return await WebUtils.Read(Path.Combine(path,s.Trim('/')));
				}
				Console.WriteLine("WebPath doesn't exist!");
			}
#endif
			var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KeyControl)}.Resources{s.Replace('/','.')}");
			if(resource==null) return null;
			using var stream=new StreamReader(resource);
			return await stream.ReadToEndAsync();
		}
	}
}