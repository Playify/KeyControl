using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsyncFriendlyStackTrace;
using PlayifyUtils.Web;

namespace KeyControl.Interfaces;

[ComVisible(true)]
public class External{
	private readonly WebBrowser _browser;
	public readonly bool Available=true;
	public string Hash="#hotkeys";

	public External(WebBrowser browser)=>_browser=browser;

	/*
	[ComVisible(true)]
	public delegate void Sender(object[] s);*/
	[ComVisible(true)]
	public class Sender{
		private readonly WebSocket _client;

		public Sender(WebSocket client)=>_client=client;

		public void Send(string s)=>_client.Send(s);
	}

	public Sender Init(object recv){
		var (server,client)=WebSocket.CreateLinked();
		_=((Func<Task>) (async ()=>{
				                try{
					                await Task.WhenAll(ConfigProvider.HandleWebSocket(server),RunClient(client,recv));
				                } catch(Exception e){
					                Console.WriteLine(e.ToAsyncString());
				                }
			                }))();
		return new Sender(client);
	}

	private async Task RunClient(WebSocket client,dynamic recv){
		await foreach(var (s,_) in client){
			if(s==null) continue;
			if(_browser.Document==null) throw new Exception("Document is null");
			//_browser.Document?.InvokeScript("receive",new object[]{s});
			recv(s);
		}
	}

	public void Log(string s)=>Console.WriteLine(s);

	public void Close()=>ConfigWindow.CloseToTray();
}