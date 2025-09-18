namespace KeyControl.Configuration;

public sealed partial class ConfigWindow{
	private readonly bool _darkMode;

	private void BrowserPreviewKeyDown(object? _,PreviewKeyDownEventArgs e){
		switch(e.KeyCode){
			case Keys.F5:
				e.IsInputKey=true;
				_browser.Refresh();
				return;
			case Keys.Delete:
			case Keys.Tab:


			case Keys.Escape:
			case Keys.Up:
			case Keys.Down:
			case Keys.Left:
			case Keys.Right:
				e.IsInputKey=false;
				return;
		}
		if(e.Control)
			switch(e.KeyCode){
				case Keys.A:
				case Keys.C:
				case Keys.V:
				case Keys.X:
				case Keys.Z:
				case Keys.Y:
				case Keys.Add:
				case Keys.Subtract:
				case Keys.Oemplus:
				case Keys.OemMinus:
					e.IsInputKey=false;
					return;
				default:{
					e.IsInputKey=true;
					return;
				}
			}
		e.IsInputKey=false;
	}

	private void BrowserDocumentCompleted(object? _,WebBrowserDocumentCompletedEventArgs e){
		_browser.Document?.InvokeScript("setDarkMode",new object[]{_darkMode});
	}
}