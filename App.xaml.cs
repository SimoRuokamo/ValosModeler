using Epx.SDK;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using ValosModeler.Properties;

namespace ValosModeler
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{/// <summary>
	 /// Initializes static members of the <see cref="App"/> class.
	 /// </summary>
		static App()
		{
			// Ensure the current culture passed into bindings is the OS culture.
			// By default, WPF uses en-US as the culture, regardless of the system settings.
			//
			FrameworkElement.LanguageProperty.OverrideMetadata(
			  typeof(FrameworkElement),
			  new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
#if !DEBUG
			// enter your local info here
			if (string.IsNullOrEmpty(Settings.Default.ServiceAddress))
			{
				//Settings.Default.ServerAddress = "http://localhost:50634/ValosModelServices.svc/basic"; // local, basic address only valid on local machine
				//Settings.Default.ServerAddress = "https://localhost:44340/ValosModelServices.svc"; // local https
				Settings.Default.ServiceAddress = "https://valos.enterprixe.com/ValosModelServices/ValosModelServices.svc"; // basic endpoint not exposed on server, use https
			}
			if (string.IsNullOrEmpty(Settings.Default.ServiceUsername))
			{
				Settings.Default.ServiceUsername = "";
			}
			if (string.IsNullOrEmpty(Settings.Default.ServicePassword))
			{
				Settings.Default.ServicePassword = EncryptTool.Encrypt("");
			}
			if (string.IsNullOrEmpty(Settings.Default.ApplicationID))
			{
				Settings.Default.ApplicationID = ""; // server
				//Settings.Default.ApplicationID = ""; // local, enter your local app id here
			}
#elif DEBUG
			// toggleswitches for selecting addressess and IDs
			int selectServer = 1; // 0 = Debug (local), 1 = valos1 (DEV), 2 = valos (production)

			// enter your local info here
			if (string.IsNullOrEmpty(Settings.Default.ServiceUsername))
			{
				Settings.Default.ServiceUsername = "";
			}
			if (string.IsNullOrEmpty(Settings.Default.ServicePassword))
			{
				Settings.Default.ServicePassword = EncryptTool.Encrypt("");
			}
			if (string.IsNullOrEmpty(Settings.Default.ServiceAddress))
			{
				if (selectServer == 0) Settings.Default.ServiceAddress = "https://localhost:44340/ValosModelServices.svc"; // local https
				else if (selectServer == 1) Settings.Default.ServiceAddress = "https://valos1.enterprixe.com/ValosModelServices/ValosModelServices.svc"; // basic endpoint not exposed on server, use https
				else if (selectServer == 2) Settings.Default.ServiceAddress = "https://valos.enterprixe.com/ValosModelServices/ValosModelServices.svc";
				//Settings.Default.ServiceAddress = "http://localhost:50634/ValosModelServices.svc/basic"; // local, basic address only valid on local machine
			}
			if (string.IsNullOrEmpty(Settings.Default.ApplicationID))
			{
				if (selectServer == 0) Settings.Default.ApplicationID = ""; // local, enter your local app id here
				else if (selectServer == 1) Settings.Default.ApplicationID = "13869736-1566-46fd-bba1-94bdb0944a3f"; // server valos1.enterprixe.com	
				else if (selectServer == 2) Settings.Default.ApplicationID = ""; // server valos.enterprixe.com
			}
#endif
		}

		/// <summary>
		/// The _startup arguments first argument
		/// </summary>
		private string _startupArgsFirstArg;

		/// <summary>
		/// Initializes a new instance of the <see cref="App"/> class.
		/// </summary>
		public App()
		{
			HasErrorHappened = false;
#if !DEBUG
			this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#endif
		}

		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// <value>The name of the application.</value>
		public static string AppName
		{
			get { return "Valos Modeler"; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance has error happened.
		/// </summary>
		/// <value><c>true</c> if this instance has error happened; otherwise, <c>false</c>.</value>
		public bool HasErrorHappened { get; set; }

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Application.Startup" /> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.StartupEventArgs" /> that contains the event data.</param>
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			Infrastructure.AppModelInstanceManager.Initialize();

			// command line arguments
			if (e.Args != null && e.Args.Length > 0)
			{
				_startupArgsFirstArg = e.Args[0];
			}

			// file extension arguments
			//var args = AppDomain.CurrentDomain.SetupInformation.ActivationArguments;
			//if (args != null && args.ActivationData != null && args.ActivationData.Length > 0)
			//{
			//	string fname = null;
			//	try
			//	{
			//		fname = args.ActivationData[0];
			//		Uri uri = new Uri(fname);
			//		fname = uri.LocalPath;
			//		_startupArgsFirstArg = fname;
			//	}
			//	catch
			//	{
			//		fname = null;
			//	}
			//}
//#if LOCAL_ONLY
			var viewModel = new MainWindowViewModel();
			MainWindow mainWindow = new MainWindow { Left = 300, Top = 200, Width = 1500, Height = 1000 };
			mainWindow.Loaded +=(ss,ee)=>
			{
				mainWindow.DataContext = viewModel;
				ValosModeler.Infrastructure.ViewModelBase.DockingManager = mainWindow.dockingManager;
				viewModel.MainWindowLoaded();
			};
			Application.Current.MainWindow = mainWindow;
			mainWindow.Show();
//#else
//			OpenStartupWindow();
//#endif
		}

		/// <summary>
		/// Opens the startup window.
		/// </summary>
		public void OpenStartupWindow()
		{
			StartupWindow startupWin = new StartupWindow();
			ValosSessionStartup sessionInformation = null;
			string openFileName = string.Empty;
			if (!string.IsNullOrEmpty(_startupArgsFirstArg))
			{
				try
				{
					sessionInformation = new ValosSessionStartup(_startupArgsFirstArg);
					startupWin.Title = sessionInformation.ModelName;
					openFileName = sessionInformation.ModelName;
				}
				catch (Exception exception)
				{
					if (exception is ArgumentException)
					{ }//have  pop window here
					throw;
				}
			}

			StartupWindowViewModel startupViewModel = new StartupWindowViewModel(startupWin, sessionInformation);
			startupWin.DataContext = startupViewModel;
			this.MainWindow = startupWin;
			startupWin.Show();
		}

		/// <summary>
		/// Handles the DispatcherUnhandledException event of the App control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Threading.DispatcherUnhandledExceptionEventArgs"/> instance containing the event data.</param>
		private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			HasErrorHappened = true;
			e.Handled = true;
			Application.Current.Shutdown();
		}  

		/// <summary>
		/// Handles the UnhandledException event of the CurrentDomain control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="args">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			HasErrorHappened = true;
			Application.Current.Shutdown();
		}
	}
}
