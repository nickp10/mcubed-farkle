using System.Windows;

namespace mCubed.Farkle
{
	public partial class HighScoresWindow : Window
	{
		public HighScoresWindow()
		{
			InitializeComponent();
		}

		private void CloseButtonClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}