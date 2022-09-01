using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ValosModeler
{
    /// <summary>
    /// Interaction logic for StartupWindow.xaml
    /// </summary>
    public partial class StartupWindow : Window
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StartupWindow"/> class.
		/// </summary>
		public StartupWindow()
		{
			InitializeComponent();
			Loaded += new RoutedEventHandler(StartupWindow_Loaded);
		}

		/// <summary>
		/// Handles the Loaded event of the StartupWindow control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
		void StartupWindow_Loaded(object sender, RoutedEventArgs e)
		{
			pwdBox.Password = ViewModel.ValosPassword;
			ViewModel.WindowLoaded();
		}

		/// <summary>
		/// Gets the view model.
		/// </summary>
		/// <value>The view model.</value>
		public StartupWindowViewModel ViewModel
		{
			get { return DataContext as StartupWindowViewModel; }
		}

		private void Window_MouseLeftDown(object sender, MouseButtonEventArgs e)
		{
			this.DragMove();
		}

		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			//if (ViewModel.IsUpdateUninstall)
			//	ViewModel.UninstallReinstall();
			//else
			//	ViewModel.StartUpdate();
		}

		private void LicenseButton_Click(object sender, RoutedEventArgs e)
		{
			Mouse.OverrideCursor = Cursors.Wait;
			ViewModel.ValosPassword = pwdBox.Password;
			ViewModel.CheckEnteredLicense();
			Mouse.OverrideCursor = null;
		}
		/// <summary>
		/// Handles the Click event of the CheckServerModels control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
		private void CheckServerModels_Click(object sender, RoutedEventArgs e)
		{
			Mouse.OverrideCursor = Cursors.Wait;
			ViewModel.ValosPassword = pwdBox.Password;
			ViewModel.GetValosModels();
			Mouse.OverrideCursor = null;
		}

		/// <summary>
		/// Handles the Click event of the ClearServerModels control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
		private void ClearServerModels_Click(object sender, RoutedEventArgs e)
		{
			Mouse.OverrideCursor = Cursors.Wait;
			ViewModel.SelectedModel = null;
			Mouse.OverrideCursor = null;
		}

		private void RestartButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Restart(true);
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
