using System.Diagnostics;
using System.Net;
using System.Reflection;
using KeyControl.Features.Strings.HotStrings;
using PlayifyUtility.Jsons;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Web;

namespace KeyControl.Configuration;

public class ConfigServer:WebBase{
	private static readonly HashSet<WebSocket> Connected=new();
	public static event Action<WebSocket>? OnConnect;
	public static event Action<WebSocket>? OnDisconnect;
	private static readonly Dictionary<string[],Action<WebSocket,Json>> Listeners=new();

	public static void Broadcast(string s){
		foreach(var ws in Connected) ws.Send(s);
	}


	private static readonly ConfigServer Server=new();
	public override bool CacheByDefault=>false;

	public static void Run()=>RunWithRetry().Background();

	private static async Task RunWithRetry(){
		while(true)
			try{
				await Server.RunHttp(new IPEndPoint(new IPAddress(new byte[]{127,2,4,8}),5001));
			} catch(Exception e){
				Console.WriteLine(e);
				await Task.Delay(10);
			}
		// ReSharper disable once FunctionNeverReturns
	}

	public static Stream? OpenFile(string path){
		//*
#if DEBUG
		if(Debugger.IsAttached){
			var filePath=Path.Combine("../../../Resources",path.Trim('/'));
			if(File.Exists(filePath)) return File.OpenRead(filePath);
			Console.WriteLine("WebPath doesn't exist!");
		}
#endif//*/
		return Assembly.GetExecutingAssembly()
		               .GetManifestResourceStream($"{nameof(KeyControl)}.Resources{path.Replace('/','.')}");
	}

	protected override async Task HandleRequest(WebSession session){
		if(session.WantsWebSocket(out var create)){
			await HandleWebSocket(await create());
			return;
		}

		if(session.Path=="/version"){
			await session.Send
			             .Header("Access-Control-Allow-Origin","*")
			             .Text(Config.VersionString);
			return;
		}

		var path=session.Path switch{
			"/"=>"/index.html",
			"/combined"=>"/combined.html",
			_=>session.Path,
		};

		if(OpenFile(path) is{} stream) await session.Send.Stream(stream,path);
		else await session.Send.Error(404);
	}

	public static async Task HandleWebSocket(WebSocket ws){
		try{
			Connected.Add(ws);
			OnConnect?.Invoke(ws);


			await foreach(var (s,_) in ws)
				if(s!=null)
					ReceiveWebSocket(ws,s);

		} finally{
			Connected.Remove(ws);
			OnDisconnect?.Invoke(ws);
		}
	}

	private static void ReceiveWebSocket(WebSocket ws,string s){
		if(!Json.TryParse(s,out var json)){
			Console.WriteLine("Invalid received: "+s);
			return;
		}

		if(json is JsonArray arr){
			var value=arr.Last();
			var keys=arr.SkipLast(1).Select(j=>j.AsString()).ToArray();

			foreach(var (key,action) in Listeners)
				if(keys.SequenceEqual(key)){
					action(ws,value);
					return;
				}


			Config.Update(keys,value);
		} else HotStringSaveAble.Update(json);
	}

	public static void Register(Action<WebSocket,Json> action,params string[] keys)=>Listeners.Add(keys,action);

	public static void Log(string s)=>Broadcast(new JsonArray("Log",s).ToString());
}