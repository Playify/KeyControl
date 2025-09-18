using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Games;

public static partial class CrossHair{
	private sealed class CrossHairWindow:Form{
		protected override bool ShowWithoutActivation=>true;

		internal CrossHairWindow(){
			MinimumSize=Size.Empty;
			FormBorderStyle=FormBorderStyle.None;
			StartPosition=FormStartPosition.Manual;

			ShowInTaskbar=false;
			TopMost=true;

			BackColor=Color.Red;
		}

		protected override CreateParams CreateParams{
			get{
				var createParams=base.CreateParams;
				createParams.ExStyle|=0x08000000;//NoActivate
				return createParams;
			}
		}

		internal void Apply(int x,int y,int size,Region reg,Color color,byte alpha){
			Region=reg;
			Bounds=new Rectangle(x,y,size,size);
			BackColor=color;
			var window=new WinWindow(Handle);
			if(!Visible){
				Visible=true;
				window.ClickThrough=true;
			}
			window.Alpha=alpha;
			window.AlwaysOnTop=true;
		}
	}
}