using System.Windows;

namespace mCubed.Farkle
{
	public partial class PromptNameWindow : Window
	{
		public PromptNameWindow()
		{
			InitializeComponent();
		}

		private void SaveButtonClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}