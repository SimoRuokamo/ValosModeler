using Enterprixe.WPF.Tools.Localization;
using Epx.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ValosModeler.Infrastructure;
using ValosModeler.Properties;
using ValosService;

namespace ValosModeler
{
	/// <summary>
	/// Class StartupWindowViewModel.
	/// </summary>
	/// <seealso cref="ViewModelBase" />
	public class StartupWindowViewModel : ViewModelBase
	{
		/// <summary>
		/// The session information.
		/// </summary>
		ValosSessionStartup _sessionInformation;
		/// <summary>
		/// The _open file name.
		/// </summary>
		private string _openFileName;
		/// <summary>
		/// The _save queried.
		/// </summary>
		//private bool _saveQueried = false;
		/// <summary>
		/// The _startup win.
		/// </summary>
		private StartupWindow _startupWin;

		/// <summary>
		/// Initializes a new instance of the <see cref="StartupWindowViewModel" /> class.
		/// </summary>
		/// <param name="startupWin">The startup win.</param>
		/// <param name="sessionInformation">Contains important information necessary for establishing a session with the server.</param>
		/// <exception cref="System.ArgumentNullException">sessionInformation</exception>
		public StartupWindowViewModel(StartupWindow startupWin, ValosSessionStartup sessionInformation)
		{
			WindowTitle = App.AppName;
			_startupWin = startupWin;
			if (sessionInformation != null)
			{
				_openFileName = sessionInformation.ModelName;
				_sessionInformation = sessionInformation;
			}
			//else
			//	throw new ArgumentNullException("sessionInformation");
		}

		/// <summary>
		/// Windows the loaded.
		/// </summary>
		public void WindowLoaded()
		{
			//if (_sessionInformation != null)
			//{
			//	OpenMainWindow();
			//	_startupWin.Close();
			//}
			if (CheckLicense())
			{
				PreCheckUpdate(true);
			}
			// on to window to get input
		}

		public bool CheckLicense()
		{

#if !DisableValos
			if (_sessionInformation != null) // session was retrieved from the file which was downloaded from the portal
			{
				EnterValosLicense = true;
				return true; // continue regular startup
			}
			else
			{
				EnterValosLicense = true; // query the user account etc info
				return false;
			}
#else
			bool bypassLogin = false; // toggle this when debuggin or developing to bypass Valos login
			if (bypassLogin)
			{
				return true;
			}
			else
			{
				EnterValosLicense = true;
				return false;
			}
#endif
		}


		#region Update
		private void PreCheckUpdate(bool showSplash)
		{
			bool checkUpdateRes = false;
			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += delegate
			{
#if NOUPDATE
				checkUpdateRes = false;
#else
				checkUpdateRes = CheckUpdate();
#endif
			};
			worker.RunWorkerCompleted += delegate
			{
				if (!checkUpdateRes)
				{
					if (showSplash)
					{
						BackgroundWorker worker2 = new BackgroundWorker();
						worker2.DoWork += delegate
						{
#if DEBUG
							//System.Threading.Thread.Sleep(100000);
#else
							System.Threading.Thread.Sleep(500);
#endif
						};
						worker2.RunWorkerCompleted += delegate
						{
							Mouse.OverrideCursor = Cursors.Wait;
							OpenMainWindow();
							_startupWin.Close();
						};
						worker2.RunWorkerAsync();
					}
					else
					{
						Mouse.OverrideCursor = Cursors.Wait;
						OpenMainWindow();
						_startupWin.Close();
					}
				}
			};
			worker.RunWorkerAsync();
		}

		/// <summary>
		/// Checks if an update is available update.
		/// </summary>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
		private bool CheckUpdate()
		{
#if DEBUG
			// testing
			bool val = false;
			IsUpdateAvailable = val;
			//IsUpdateForced = false;
			//IsUpdateUninstall = false;
			return val;
#else
			return false;
#endif
		}

		private bool _IsUpdateAvailable = false;
		public bool IsUpdateAvailable
		{
			get { return _IsUpdateAvailable; }
			set
			{
				if (_IsUpdateAvailable != value)
				{
					_IsUpdateAvailable = value;
					OnPropertyChanged("IsUpdateAvailable");
				}
			}
		}
		#endregion

		/// <summary>
		/// Checks the license entered from the UI.
		/// </summary>
		public void CheckEnteredLicense()
		{
#if !DisableValos
			if (CheckValosLicense())
			{
				Settings.Default.ServiceUsername = ValosUsername;
				Settings.Default.ServicePassword = EncryptTool.Encrypt(ValosPassword);
				Settings.Default.ServiceAddress = ValosAddress;
				Settings.Default.ApplicationID = ValosAppID;
				Settings.Default.Save();
				EnterValosLicense = false;
				PreCheckUpdate(false);
			}
#else
			// for testing
			if (true)
			{
				EnterValosLicense = false;
				PreCheckUpdate(false);
			}
#endif
			else
			{
				// handled in CheckValosLicense()
			}

		}

		#region MainWindow

		/// <summary>
		/// Opens the main window.
		/// </summary>
		public void OpenMainWindow()
		{
			MainWindow mainWindow = new MainWindow();
			// Create the ViewModel to which the main window binds.
			var viewModel = new MainWindowViewModel(_sessionInformation);

			// When the ViewModel asks to be closed, 
			// close the window.
			EventHandler handler = null;
			handler = delegate
			{
				viewModel.RequestClose -= handler;
				mainWindow.Close();
			};
			viewModel.RequestClose += handler;


			mainWindow.Loaded += delegate (object sender, RoutedEventArgs e)
			{
				if (!viewModel.InitialLoadCompleted)
				{
					mainWindow.DataContext = viewModel;
					ViewModelBase.DockingManager = mainWindow.dockingManager;
					viewModel.MainWindowLoaded();
				}
			};

			Application.Current.MainWindow = mainWindow;
			// dont' set datacontext before window is shown, bindings might not get updated
			mainWindow.Show();
			Mouse.OverrideCursor = null;
		}
		#endregion

		#region EpxService

		private bool _EnterValosLicense = false;
		public bool EnterValosLicense
		{
			get { return _EnterValosLicense; }
			set
			{
				if (_EnterValosLicense != value)
				{
					_EnterValosLicense = value;
					OnPropertyChanged("EnterValosLicense");
				}
			}
		}

		private string _ValosUsername = Properties.Settings.Default.ServiceUsername;
		public string ValosUsername
		{
			get { return _ValosUsername; }
			set
			{
				if (_ValosUsername != value)
				{
					_ValosUsername = value;
					OnPropertyChanged("ValosUsername");
				}
			}
		}
		private string _ValosPassword = EncryptTool.Decrypt(Properties.Settings.Default.ServicePassword);
		public string ValosPassword
		{
			get { return _ValosPassword; }
			set
			{
				if (_ValosPassword != value)
				{
					_ValosPassword = value;
					OnPropertyChanged("ValosPassword");
				}
			}
		}
		private string _ValosAddress = Properties.Settings.Default.ServiceAddress;
		public string ValosAddress
		{
			get { return _ValosAddress; }
			set
			{
				if (_ValosAddress != value)
				{
					_ValosAddress = value;
					OnPropertyChanged("ValosAddress");
				}
			}
		}
		private string _ValosAppID = Properties.Settings.Default.ApplicationID;
		public string ValosAppID
		{
			get { return _ValosAppID; }
			set
			{
				if (_ValosAppID != value)
				{
					_ValosAppID = value;
					OnPropertyChanged("ValosAppID");
				}
			}
		}
		private ServerDataModel _selectedModel = null;
		/// <summary>
		/// Gets or sets the selected model.
		/// </summary>
		/// <value>The selected model.</value>
		public ServerDataModel SelectedModel
		{
			get
			{
				return _selectedModel;
			}
			set
			{
				if (value != _selectedModel)
				{
					_selectedModel = value;
					OnPropertyChanged("SelectedModel");
				}
			}
		}

		/// <summary>
		/// Gets or sets all models.
		/// </summary>
		/// <value>All models.</value>
		public List<ServerDataModel> AllModels
		{
			get
			{
				return _allModels;
			}
			set
			{
				_allModels = value;
				OnPropertyChanged("AllModels");

			}
		}

		/// <summary>
		/// Gets the valos models.
		/// </summary>
		public void GetValosModels()
		{
			try
			{
				var dataModels = new List<ServerDataModel>();
				UserLoginProject.GetUserModelsForApplication(ValosAddress, ValosUsername, new Guid(_ValosAppID), "", ValosPassword, out dataModels);
				AllModels = dataModels.OrderByDescending(m => m.LastModifiedByUser).ToList();
			}
			catch (FormatException formatException)
			{
				throw formatException;
			}
			//catch (Exception e)
			//{
			//	throw e;
			//}
		}

		private List<ServerDataModel> _allModels = new List<ServerDataModel>();
		/// <summary>
		/// Checks the valos license. Displays the list of models available for the user with this application and the option to 
		/// start a "local session" if licenses that aren't tied to any model exist.
		/// </summary>
		/// <returns><c>true</c> if the license is valid, <c>false</c> otherwise.</returns>
		private bool CheckValosLicense()
		{
			bool result = false;
			try
			{
				byte[] sessionID;
				Guid userID = Guid.Empty;
				List<UserPublicProfile> userPublicProfiles;
				Guid modelId = SelectedModel == null ? Guid.Empty : SelectedModel.ModelId;

				// allow non-Valos use
				if (string.IsNullOrEmpty(ValosUsername) && string.IsNullOrEmpty(ValosPassword))
				{
					_sessionInformation = null;
					return true;
				}

				result = UserLoginProject.StartSession(ValosAddress, ValosUsername, new Guid(_ValosAppID), ValosPassword, modelId,
					out userID, out sessionID, out userPublicProfiles);

				if (result && SelectedModel != null)
					_sessionInformation = new ValosSessionStartup(ValosAddress, sessionID, SelectedModel.Name, "", userID);
				else if (result)
					_sessionInformation = new ValosSessionStartup(ValosAddress, sessionID, "", "", userID);
				else
				{
					string title = CultureManager.GetLocalizedString("No Valid Licence");
					string warningMessage = CultureManager.GetLocalizedString("A licence does not exsist or there are no more free licences available");
					MessageBox.Show(warningMessage, title, MessageBoxButton.OK);
				}
			}
			catch (FormatException formatException)
			{
				throw formatException;
			}
			//catch (Exception e)
			//{
			//	throw e;
			//}
			return result; // continue with regular startup
		}

#endregion

		public void Restart(bool isAfterUpdate)
		{
			if (isAfterUpdate)
			{
				//_updateLog.SetElementValue("RestartedAfterUpdate", true);
				//UserSession.Log(_updateLog);
			}

			(Application.Current as App).ShutdownMode = ShutdownMode.OnLastWindowClose;
			//System.Windows.Forms.Application.Restart();
			Application.Current.Shutdown();
		}
	}
}
