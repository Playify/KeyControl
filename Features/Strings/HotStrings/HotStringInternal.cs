namespace KeyControl.Features.Strings.HotStrings;

public abstract class HotStringInternal{
	public abstract (int bs,string s)? Replace(string s);

	public virtual (int bs,string s)? Replace(string s,out HotStringInternal curr){
		curr=this;
		return Replace(s);
	}
}