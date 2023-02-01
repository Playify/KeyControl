using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace KeyControl.Utilities;

public static class Scheduler{
	private static readonly object MapLock=new();
	private static readonly Dictionary<Action,long> Map=new();
	private static Thread _executor;
	private static readonly HashSet<Action> Set=new();
	private static readonly AutoResetEvent Reset=new(false);

	public static void RunLater(long delta,Action run,bool mainThread=false)=>RunAtTime(delta+DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),run,mainThread);

	public static void RunNever(Action run){
		if(run==null) return;

		lock(run)
		lock(MapLock){
			var (old,oldTime)=GetNextTime();
			if(old==null)//already empty
				return;
			Set.Remove(run);

			if(!Map.Remove(run))//not in map
				return;
			var (action,time)=GetNextTime();
			if(action==null||oldTime!=time)//now is empty or was first entry
				Reset.Set();
		}
	}

	public static void RunAtTime(long time,Action run,bool mainThread=false){
		if(run==null) return;
		if(time<=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()&&mainThread){
			RunOnMainThread(run);
			return;
		}
		lock(run)
		lock(MapLock){
			var (action,l)=GetNextTime();


			Map[run]=time;
			if(mainThread) Set.Add(run);
			else Set.Remove(run);

			if(action!=null&&l>time)//wasn't first entry in map
				Reset.Set();

			if(_executor!=null) return;
			_executor=new Thread(Run);
			_executor.Start();
		}
	}


	private static (Action action,long time) GetNextTime(){
		lock(MapLock) return Map.OrderBy(p=>p.Value).Select(p=>(p.Key,p.Value)).FirstOrDefault();
	}

	public static bool WillRun(Action run){
		lock(run)
		lock(MapLock)
			return Map.ContainsKey(run);
	}

	public static void RunOnMainThread(Action run){
		if(run!=null) Program.BeginInvoke(run);
	}

	private static void Run(){
		while(true){
			Action action;
			var mainThread=false;
			long time;
			lock(MapLock){
				(action,time)=GetNextTime();
				if(action==null){
					_executor=null;
					return;
				}
				time-=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				if(time<=0){
					Map.Remove(action);
					mainThread=Set.Remove(action);
				}
			}
			if(time<=0)
				lock(action)
					try{
						if(mainThread) RunOnMainThread(action);
						else action();
					} catch(Exception e){
						Console.WriteLine("Scheduler catched:"+e);
					}
			else Reset.WaitOne(TimeSpan.FromMilliseconds(time));
		}
	}
}