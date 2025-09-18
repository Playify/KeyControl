using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Web;

namespace KeyControl.Configuration;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[SuppressMessage("ReSharper","InconsistentNaming")]
public class ConfigScriptingObject{
	public bool Available=true;
	public string? Hash;
	private static WebSocket? _send;

	public void send(string s)=>_send?.Send(s).Background();

	public async void init(dynamic receiveFunc){
		_send?.Close();
		var (server,client)=WebSocket.CreateLinked();
		ConfigServer.HandleWebSocket(server).Background();
		_send=client;
		try{
			await foreach(var (s,b) in client){
				if(s!=null) receiveFunc(s);
				else receiveFunc(b);
			}
		} catch(Exception e){
			Console.WriteLine(e);
		}
	}

	public void Log(dynamic? d){
		Console.WriteLine(d?.ToString()??"null");
	}
}