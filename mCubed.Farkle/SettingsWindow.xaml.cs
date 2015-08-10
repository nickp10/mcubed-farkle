using System.Windows;

namespace mCubed.Farkle
{
	public partial class SettingsWindow : Window
	{
		public SettingsWindow()
		{
			InitializeComponent();
			Closing += delegate { Serialization.Save(); };
		}

		private void SaveSettingsClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}