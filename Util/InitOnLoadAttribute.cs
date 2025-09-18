using System.Reflection;
using JetBrains.Annotations;
using PlayifyUtility.Utils.Extensions;

namespace KeyControl.Util;

[MeansImplicitUse(ImplicitUseTargetFlags.Itself)]
[AttributeUsage(AttributeTargets.Class)]
public class InitOnLoadAttribute:Attribute{
	private readonly int _priority;
	private static readonly List<(int i,Action a)> List=new();

	//Higher priority loads first
	public InitOnLoadAttribute(int priority=0)=>_priority=priority;

	public static void LoadAssembly(Assembly? assembly=null){
		assembly??=Assembly.GetCallingAssembly();

		foreach(var type in assembly
		                    .GetTypes()
		                    .Select(type=>type.GetCustomAttribute<InitOnLoadAttribute>().NotNull(out var attr)
			                                  ?(type,attr._priority)
			                                  :((Type type,int prio)?)null)
		                    .NonNull()
		                    .OrderBy(tup=>tup.prio)
		                    .Select(tup=>tup.type))
			type.RunClassConstructor();

		var tuples=List.OrderByDescending(t=>t.i).Select(t=>t.a).ToArray();
		List.Clear();
		foreach(var action in tuples) action();
	}

	public static void OnAfter(int i,Action action)=>List.Add((i,action));
}