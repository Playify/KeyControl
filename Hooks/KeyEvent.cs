using System.Windows.Forms;

namespace KeyControl.Hooks{
	public delegate void KeyEventHandler(object sender,KeyEvent e);

	public class KeyEvent{
		public readonly int ScanCode;

		public readonly int VkCode;

		public KeyEvent(Keys key,int vkCode,int scanCode){
			Key=key;
			VkCode=vkCode;
			ScanCode=scanCode;
		}

		public bool Handled{get;set;}
		public Keys Key{get;}
	}
}