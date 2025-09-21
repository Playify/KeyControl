using PlayifyUtility.Windows.Win;

namespace KeyControl.Features.Hotkeys.Advanced;

public partial class Spammer{
	private sealed class SpammerIndicatorWindow:Form{
		protected override bool ShowWithoutActivation=>true;

		internal SpammerIndicatorWindow(){
			FormBorderStyle=FormBorderStyle.None;
			StartPosition=FormStartPosition.Manual;
			Size=new Size(58,13);
			MinimumSize=Size;
			MaximumSize=Size;

			ShowInTaskbar=false;
			TopMost=true;
		}

		protected override CreateParams CreateParams{
			get{
				var createParams=base.CreateParams;
				createParams.ExStyle|=0x08000000;//NoActivate
				return createParams;
			}
		}

		internal void Apply(bool running){
			if(Screen.PrimaryScreen?.WorkingArea is not{} primaryScreen) return;
			var corner=primaryScreen.Location+primaryScreen.Size;
			Location=corner-Size;

			BackColor=running?Color.ForestGreen:Color.Red;
			Opacity=running?1:.75;


			var window=new WinWindow(Handle);
			if(!Visible){
				Visible=true;
				window.ClickThrough=true;
			} else Invalidate();
			window.AlwaysOnTop=true;
		}

		protected override void OnPaint(PaintEventArgs e){
			var g=e.Graphics;
			var font=new Font(FontFamily.GenericSansSerif,8);
			var brush=Brushes.White;
			g.DrawString("SPAMMER",font,brush,0,0);
		}
	}
}