using System.Windows.Forms;

namespace KeyControl.Hooks;

public delegate void MouseEventHandler(MouseEvent mouseEvent);

public class MouseEvent{
	public MouseEvent(int x,int y,MouseButtons button,int delta=0){
		X=x;
		Y=y;
		Button=button;
		Delta=delta;
	}

	public bool Handled{get;set;}
	public int X{get;}
	public int Y{get;}

	public int Delta{get;}
	public MouseButtons Button{get;}

	public Keys Key
		=>Button switch{
			MouseButtons.Left=>Keys.LButton,
			MouseButtons.Middle=>Keys.MButton,
			MouseButtons.Right=>Keys.RButton,
			MouseButtons.XButton1=>Keys.XButton1,
			MouseButtons.XButton2=>Keys.XButton2,
			var _=>Keys.None,
		};
}