using AvalonDock.Layout;
using Epx.BIM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using ValosModeler.Infrastructure.Events;

namespace ValosModeler.Infrastructure
{
	/// <summary>
	/// Base class for all ViewModel classes in the application.
	/// It provides support for property change notifications
	/// and has a DisplayName property.  This class is abstract.
	/// </summary>
	/// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
	/// <seealso cref="System.IDisposable" />
	public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ViewModelBase"/> class.
		/// </summary>
		protected ViewModelBase()
		{
			ThrowOnInvalidPropertyName = false;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ViewModelBase"/> class.
		/// </summary>
		/// <param name="dataNodes">The data nodes.</param>
		protected ViewModelBase(List<BaseDataNode> dataNodes)
		{
			_dataNodes = dataNodes;
			ThrowOnInvalidPropertyName = false;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ViewModelBase"/> class.
		/// </summary>
		/// <param name="dataNode">The data node.</param>
		protected ViewModelBase(BaseDataNode dataNode)
			: this(new List<BaseDataNode>() { dataNode })
		{
		}

		/// <summary>
		/// The _data nodes
		/// </summary>
		protected List<BaseDataNode> _dataNodes = null;
		/// <summary>
		/// The first or only data node of the viewmodel.
		/// </summary>
		/// <value>The data nodes.</value>
		public List<BaseDataNode> DataNodes
		{
			get { return _dataNodes; }
		}
		/// <summary>
		/// The first or only data node of the viewmodel.
		/// </summary>
		/// <value>The data node.</value>
		public BaseDataNode DataNode
		{
			get { return _dataNodes != null ? _dataNodes.FirstOrDefault() : null; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is data node editable.
		/// </summary>
		/// <value><c>true</c> if this instance is data node editable; otherwise, <c>false</c>.</value>
		public virtual bool IsDataNodeEditable
		{
			get
			{
				if (_dataNodes != null)
				{
					return _dataNodes.All(n => n.IsEditable);
				}
				return true;
			}
		}

		/// <summary>
		/// The _ is enabled
		/// </summary>
		private bool _IsEnabled = true;
		/// <summary>
		/// Gets or sets a value indicating whether this instance is enabled.
		/// </summary>
		/// <value><c>true</c> if this instance is enabled; otherwise, <c>false</c>.</value>
		public virtual bool IsEnabled
		{
			get { return _IsEnabled; }
			set
			{
				if (_IsEnabled != value)
				{
					_IsEnabled = value;
					OnPropertyChanged("IsEnabled");
				}
			}
		}
		/// <summary>
		/// Is data of nodes loaded.
		/// </summary>
		/// <value><c>true</c> if this instance is loaded; otherwise, <c>false</c>.</value>
		public virtual bool IsDataLoaded
		{
			get { return _dataNodes != null ? _dataNodes.All(n => n.IsDataLoaded) : false; }
			set { }
		}
		/// <summary>
		/// The _ is closeable
		/// </summary>
		private bool _IsCloseable = true;
		/// <summary>
		/// Gets/sets whether this viewmodel can be closed.
		/// </summary>
		/// <value><c>true</c> if this instance is closeable; otherwise, <c>false</c>.</value>
		public bool IsCloseable
		{
			get { return _IsCloseable; }
			set
			{
				if (_IsCloseable != value)
				{
					_IsCloseable = value;
					OnPropertyChanged("IsCloseable");
				}
			}
		}

		private string _title = "Title";
		public string WindowTitle
		{
			get { return _title; }
			set
			{
				if (_title != value)
				{
					_title = value;
					OnPropertyChanged();
				}
			}
		}

		#region Mediator

		/// <summary>
		/// The _mediator
		/// </summary>
		static readonly Mediator _mediator = Infrastructure.Mediator.Instance;
		/// <summary>
		/// The _is registered to mediator
		/// </summary>
		private bool _isRegisteredToMediator = false;

		/// <summary>
		/// Gets the mediator.
		/// </summary>
		/// <value>The mediator.</value>
		public Mediator Mediator
		{
			get { return _mediator; }
		}

		/// <summary>
		/// Gets the mediator static.
		/// </summary>
		/// <value>The mediator static.</value>
		public static Mediator MediatorStatic
		{
			get { return _mediator; }
		}
		/// <summary>
		/// Register this viewmodel with the Mediator. Unregistered in Dispose().
		/// Sets flag to avoid unnecessary Unregister calls.
		/// </summary>
		protected void RegisterToMediator()
		{
			Mediator.Register(this);
			_isRegisteredToMediator = true;
		}

		/// <summary>
		/// Called when [view model node property changed].
		/// </summary>
		/// <param name="payload">The payload.</param>
		[MediatorMessageSink(Events.NodePropertyChanged.MessageID)]
		public virtual void OnViewModelNodePropertyChanged(NodePropertyChangedPayload payload)
		{
			if ((_dataNodes != null) && _dataNodes.Intersect(payload.DataNodes).Count() > 0)
			{
				foreach (string str in payload.ChangedPropertyNames)
				{
					if (str == "IsEditable")
					{
						OnPropertyChanged("IsDataNodeEditable");
					}
					else if (str == "DataLoaded" || str == "IsDataLoaded")
					{
						OnPropertyChanged("IsDataLoaded");
					}
					else
					{
						OnPropertyChanged(str);
					}
				}
			}
		}

		#endregion

		#region Debugging Aides

		/// <summary>
		/// Warns the developer if this object does not have
		/// a public property with the specified name. This
		/// method does not exist in a Release build.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <exception cref="System.Exception"></exception>
		[Conditional("DEBUG")]
		//[DebuggerStepThrough]
		public void VerifyPropertyName(string propertyName)
		{
			// Verify that the property name matches a real,  
			// public, instance property on this object.
			if (TypeDescriptor.GetProperties(this)[propertyName] == null)
			{
				string msg = "Invalid property name: " + propertyName;

				if (this.ThrowOnInvalidPropertyName)
					throw new Exception(msg);
				else
					Debug.WriteLine(msg); // Returns from function.
			}
		}

		/// <summary>
		/// Verifies the property.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
		/// <exception cref="System.Exception"></exception>
		public bool VerifyProperty(string propertyName)
		{
			bool res = true;
			// Verify that the property name matches a real,  
			// public, instance property on this object.
			if (TypeDescriptor.GetProperties(this)[propertyName] == null)
			{
				string msg = "Invalid property name: " + propertyName;

				if (this.ThrowOnInvalidPropertyName)
					throw new Exception(msg);
				else
					Debug.WriteLine(msg);

				res = false;
			}
			return res;
		}

		/// <summary>
		/// Returns whether an exception is thrown, or if a Debug.Fail() is used
		/// when an invalid property name is passed to the VerifyPropertyName method.
		/// The default value is false, but subclasses used by unit tests might
		/// override this property's getter to return true.
		/// </summary>
		/// <value><c>true</c> if [throw on invalid property name]; otherwise, <c>false</c>.</value>
		protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

		#endregion // Debugging Aides

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Raised when a property on this object has a new value.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises this object's PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">The property that has a new value.</param>
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			this.VerifyPropertyName(propertyName);

			PropertyChangedEventHandler handler = this.PropertyChanged;
			if (handler != null)
			{
				var e = new PropertyChangedEventArgs(propertyName);
				handler(this, e);
			}
		}

		#endregion // INotifyPropertyChanged Members

		#region IDisposable Members

		/// <summary>
		/// The _disposed
		/// </summary>
		private bool _disposed = false;
		/// <summary>
		/// Invoked when this object is being removed from the application
		/// and will be subject to garbage collection.
		/// NOTE!! Has to be invoked manually if used.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Child classes can override this method to perform
		/// clean-up logic, such as removing event handlers.
		/// Always call base.OnDispose() from overriding handlers.
		/// </summary>
		/// <param name="disposing">True if user called (dipose managed + unmanaged), false if GC finalizer called (unmanaged only).</param>
		protected virtual void Dispose(bool disposing)
		{
			// Dispose(bool disposing) executes in two distinct scenarios.
			// If disposing equals true, the method has been called directly
			// or indirectly by a user's code. Managed and unmanaged resources
			// can be disposed.
			// If disposing equals false, the method has been called by the
			// runtime from inside the finalizer and you should not reference
			// other objects. Only unmanaged resources can be disposed.

			if (!_disposed)
			{
				if (_isRegisteredToMediator) Mediator.Unregister(this);
				_disposed = true;
			}
		}
		/// <summary>
		/// Dispose in another thread. Be careful with UI thread objects.
		/// </summary>
		public void DisposeAsync()
		{
			System.Threading.Tasks.Task.Run(() => Dispose());
		}
		/// <summary>
		/// Dispose in another thread. Be careful with UI thread objects.
		/// </summary>
		/// <param name="viewmodels"></param>
		internal static void DisposeAsync(IEnumerable<ViewModelBase> viewmodels)
		{
			System.Threading.Tasks.Task.Run(() =>
			{
				foreach (var vm in viewmodels) vm.Dispose();
			});
		}

		/// <summary>
		/// Useful for ensuring that ViewModel objects are properly garbage collected.
		/// This destructor will run only if the Dispose method
		/// does not get called.
		/// Do not provide destructors in types derived from this class.
		/// </summary>
		~ViewModelBase()
		{
			Dispose(false);
		}

		#endregion // IDisposable Members

		#region Undo

		//protected static ActionManager actionManager = new ActionManager();

		///// <summary>
		///// Undo/Redo manager.
		///// </summary>
		//public ActionManager ActionManager
		//{
		//	get { return actionManager; }
		//}

		///// <summary>
		///// Undo/Redo manager.
		///// </summary>
		//public static ActionManager ActionManagerStatic
		//{
		//	get { return actionManager; }
		//}

		//public static void ResetActionManager()
		//{
		//	//actionManager.Clear();
		//}

		//public void RecordAction(AbstractAction action)
		//{
		//	action.Execute();
		//}

		/// <summary>
		/// Calls the method action.
		/// </summary>
		/// <param name="execute">The execute.</param>
		/// <param name="unexecute">The unexecute.</param>
		/// <param name="targetObject">The target object.</param>
		/// <param name="title">The title.</param>
		/// <param name="description">The description.</param>
		/// <param name="consequenceType">Type of the consequence.</param>
		public virtual void CallMethodAction(Action execute, Action unexecute, object targetObject, string title, string description, int consequenceType = 0)
		{
			// can implement undo/redo here
			execute.Invoke();
		}

		/// <summary>
		/// Record action with the given do/undo delegate.
		/// Checks difference between new and old value before recording action for double,int,float,decimal types.
		/// If node count == 1 does not record action if difference is less than 0.0001.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value.</param>
		/// <param name="doUndoAction">The do undo action.</param>
		/// <param name="allowToMergeWithPrevious">specify true only when ie. dragging something with mouse</param>
		/// <param name="consequenceType">Type of the consequence.</param>
		public virtual void SetNodePropertyAction(List<BaseDataNode> nodes, string propertyName, object value, Action<BaseDataNode, PropertyInfo> doUndoAction, bool allowToMergeWithPrevious, int consequenceType)
		{
			var property = nodes.First().GetType().GetProperty(propertyName);
			//object oldvalue = property.GetValue(nodes.First(), null);

			if (nodes.Any(n => AllowRecord(property.GetValue(n, null), value)))
			{
				foreach (BaseDataNode node in nodes)
				{
					property.SetValue(node, value, null);
					doUndoAction.Invoke(node, property);
				}
			}
		}
		/// <summary>
		/// Record action with the given do/undo delegate.
		/// Checks difference between new and old value before recording action for double,int,float,decimal types.
		/// If node count == 1 does not record action if difference is less than 0.0001.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value.</param>
		/// <param name="doAction">The do action.</param>
		/// <param name="undoAction">The undo action.</param>
		/// <param name="allowToMergeWithPrevious">specify true only when ie. dragging something with mouse</param>
		/// <param name="consequenceType">Type of the consequence.</param>
		public virtual void SetNodePropertyAction(List<BaseDataNode> nodes, string propertyName, object value, Action<BaseDataNode, PropertyInfo> doAction, Action<BaseDataNode, PropertyInfo> undoAction, bool allowToMergeWithPrevious, int consequenceType)
		{
			var property = nodes.First().GetType().GetProperty(propertyName);
			//object oldvalue = property.GetValue(nodes.First(), null);

			if (nodes.Any(n => AllowRecord(property.GetValue(n, null), value)))
			{
				foreach (BaseDataNode node in nodes)
				{
					property.SetValue(node, value, null);
					doAction.Invoke(node, property);
				}
			}
		}
		/// <summary>
		/// Record action with the given do/undo delegate. Does not merge actions with previous ones.
		/// Checks difference between new and old value before recording action for double,int,float,decimal types.
		/// If node count == 1 does not record action if difference is less than 0.0001.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value.</param>
		/// <param name="doAction">The do action.</param>
		/// <param name="undoAction">The undo action.</param>
		/// <param name="consequenseType">Type of the consequense.</param>
		public void SetNodePropertyAction(List<BaseDataNode> nodes, string propertyName, object value, Action<BaseDataNode, PropertyInfo> doAction, Action<BaseDataNode, PropertyInfo> undoAction, int consequenseType = 0)
		{
			SetNodePropertyAction(nodes, propertyName, value, doAction, undoAction, false, consequenseType);
		}
		/// <summary>
		/// Record action with the given do/undo delegate. Does not merge actions with previous ones.
		/// Checks difference between new and old value before recording action for double,int,float,decimal types.
		/// If node count == 1 does not record action if difference is less than 0.0001.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value.</param>
		/// <param name="doUndoAction">The do undo action.</param>
		/// <param name="consequenseType">Type of the consequense.</param>
		public void SetNodePropertyAction(List<BaseDataNode> nodes, string propertyName, object value, Action<BaseDataNode, PropertyInfo> doUndoAction, int consequenseType = 0)
		{
			SetNodePropertyAction(nodes, propertyName, value, doUndoAction, false, consequenseType);
		}

		/// <summary>
		/// Record action with the given do/undo delegate. Does not merge actions with previous ones.
		/// Checks difference between new and old value before recording action for double,int,float,decimal types.
		/// Does not record action if difference is less than 0.0001.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value.</param>
		/// <param name="doAction">The do action.</param>
		/// <param name="undoAction">The undo action.</param>
		/// <param name="consequenseType">Type of the consequense.</param>
		public void SetNodePropertyAction(BaseDataNode node, string propertyName, object value, Action<BaseDataNode, PropertyInfo> doAction, Action<BaseDataNode, PropertyInfo> undoAction, int consequenseType = 0)
		{
			SetNodePropertyAction(new List<BaseDataNode>() { node }, propertyName, value, doAction, undoAction, false, consequenseType);
		}
		/// <summary>
		/// Record action with the given do/undo delegate. Does not merge actions with previous ones.
		/// Checks difference between new and old value before recording action for double,int,float,decimal types.
		/// Does not record action if difference is less than 0.0001.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value.</param>
		/// <param name="doUndoAction">The do undo action.</param>
		/// <param name="consequenseType">Type of the consequense.</param>
		public void SetNodePropertyAction(BaseDataNode node, string propertyName, object value, Action<BaseDataNode, PropertyInfo> doUndoAction, int consequenseType = 0)
		{
			SetNodePropertyAction(new List<BaseDataNode>() { node }, propertyName, value, doUndoAction, false, consequenseType);
		}

		/// <summary>
		/// Allows the record.
		/// </summary>
		/// <param name="oldvalue">The oldvalue.</param>
		/// <param name="value">The value.</param>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
		protected bool AllowRecord(object oldvalue, object value)
		{
			bool allowRecord = true;

			if (oldvalue == value)
			{
				allowRecord = false;
			}
			else if (oldvalue.Equals(value))
			{
				allowRecord = false;
			}
			else if (oldvalue is double && value is double)
			{
				double diff = Math.Abs((double)oldvalue - (double)value);
				if (diff < 0.0001) allowRecord = false;
			}
			else if (oldvalue is int && value is int)
			{
				int diff = Math.Abs((int)oldvalue - (int)value);
				if (diff < 0.0001) allowRecord = false;
			}
			else if (oldvalue is float && value is float)
			{
				float diff = Math.Abs((float)oldvalue - (float)value);
				if (diff < 0.0001) allowRecord = false;
			}
			else if (oldvalue is decimal && value is decimal)
			{
				decimal diff = Math.Abs((decimal)oldvalue - (decimal)value);
				if (diff < (decimal)0.0001) allowRecord = false;
			}
			else if (oldvalue is Point && value is Point)
			{
				double diffx = Math.Abs(((Point)oldvalue).X - ((Point)value).X);
				double diffy = Math.Abs(((Point)oldvalue).Y - ((Point)value).Y);
				if (diffx < 0.0001 && diffy < 0.0001) allowRecord = false;
			}
			else if (oldvalue is Point3D && value is Point3D)
			{
				double diffx = Math.Abs(((Point3D)oldvalue).X - ((Point3D)value).X);
				double diffy = Math.Abs(((Point3D)oldvalue).Y - ((Point3D)value).Y);
				double diffz = Math.Abs(((Point3D)oldvalue).Z - ((Point3D)value).Z);
				if (diffx < 0.0001 && diffy < 0.0001 && diffz < 0.0001) allowRecord = false;
			}

			return allowRecord;
		}

		#endregion

		//#region MessageBox

		///// <summary>
		///// Handle return value in resultCallBack function.
		///// </summary>
		///// <param name="identifier">The id of the behavior to call. (== where to show dialog)</param>
		///// <param name="text"></param>
		///// <param name="caption"></param>
		///// <param name="buttons"></param>
		///// <param name="resultCallBack">MessageBoxResult</param>
		//protected void ShowMessageBox(string identifier, string text, string caption, MessageBoxButton buttons, Action<object> resultCallBack)
		//{
		//	Mediator.NotifyColleagues<DialogCommand>(MediatorMessages.ShowDialog, new DialogCommand(identifier, result => resultCallBack(result), text, caption, buttons));
		//}

		//#endregion

		#region AvalonDock

		protected static AvalonDock.DockingManager _dockingManager;
		/// <summary>
		/// The docking manager. Can only be set once.
		/// </summary>
		public static AvalonDock.DockingManager DockingManager
		{
			get { return _dockingManager; }
			set
			{
				if (_dockingManager == null)
				{
					_dockingManager = value;
				}
			}
		}

		/// <summary>
		/// Content id for LayoutDocuments which should not be saved.
		/// </summary>
		/// <remarks>For use with AvalonDock.</remarks>
		public const string NoSaveContentId = "DoNotSave";

		/// <summary>
		/// Specify true if this viewmodel is a Ribbon Tab viewmodel and should be disposed when Ribbon Tabs are regenerated.
		/// Specify false for viewmodels which are loaded into Ribbon Tabs from ie. LayoutDocuments.
		/// </summary>
		/// <remarks>For use with AvalonDock.</remarks>
		public bool DisposeOnRibbonTabGeneration
		{
			get; protected set;
		}

		/// <summary>
		/// Called when a LayoutDocument or LayoutAnchorable is closed. Use also when default unsubscription from events (in Dispose())
		/// is not possible during normal runtime. (ie. because of ribbontabs using same viewmodel instance)
		/// </summary>
		/// <remarks>For use with AvalonDock.</remarks>
		//public virtual void DisposeFinally()
		//{
		//	Dispose();
		//}
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>For use with AvalonDock.</remarks>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void OnLayoutDocumentClosed(object sender, System.EventArgs args)
		{
			//DisposeFinally();
			LayoutContent doc = sender as LayoutContent;
			if (doc != null)
			{
				doc.Closed -= this.OnLayoutDocumentClosed;
				//if (doc.Content is IFloatingWindow && doc.IsFloating)
				//	(doc.Content as IFloatingWindow).SaveFloatingData(doc);
			}
		}
		/// <summary>
		/// Disposes the view modle while still attached to the visual tree.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void OnLayoutDocumentClosing(object sender, System.EventArgs args)
		{
			Dispose(); // Dispose while still attached to visual tree so that bindings still work
			LayoutContent doc = sender as LayoutContent;
			if (doc != null)
			{
				doc.Closing -= this.OnLayoutDocumentClosing;
			}
		}

		private string _contentId = string.Empty;
		/// <summary>
		/// ContentId for content identification when (de)serializing docking layout.
		/// </summary>
		/// <remarks>For use with AvalonDock.</remarks>
		public string ContentId
		{
			get { return _contentId; }
			set
			{
				if (_contentId != value)
				{
					_contentId = value;
					OnPropertyChanged();
				}
			}
		}

		//private Uri _iconSource = null;
		//public Uri IconSource
		//{
		//	get { return _iconSource; }
		//	set
		//	{
		//		if (_iconSource != value)
		//		{
		//			_iconSource = value;
		//			RaisePropertyChanged(() => this.IconSource);
		//		}
		//	}
		//}

		private bool _isActiveContent = false;
		/// <summary>
		/// Gets/sets the view containing this viewmodel as the active content in the docking system.
		/// </summary>
		/// <remarks>For use with AvalonDock.</remarks>
		public bool IsActiveContent
		{
			get { return _isActiveContent; }
			set
			{
				if (_isActiveContent != value)
				{
					_isActiveContent = value;
					if (_isActiveContent) Mediator.NotifyColleagues<ViewModelBase>("SetActiveContent", this);
					OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// Called when the view containing this viewmodel was or is set as the ActiveContent. Base must be called.
		/// </summary>
		/// <remarks>For use with AvalonDock.</remarks>
		/// <param name="isActiveContent"></param>
		public virtual void IsActiveContentChanged(bool isActiveContent)
		{
			if (_isActiveContent != isActiveContent)
			{
				_isActiveContent = isActiveContent;
				if (_isActiveContent) UpdateLastActiveDockableContent();
				OnPropertyChanged(nameof(this.IsActiveContent));
			}
		}

		/// <summary>
		/// Keep track of the dockable content window which was last active.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UpdateLastActiveDockableContent()
		{
			if (DockingManager.ActiveContent is FrameworkElement activeContent)
			{
				if (!_lastActiveDockableContent.ContainsKey(activeContent.GetType()))
				{
					_lastActiveDockableContent.Add(activeContent.GetType(), activeContent.DataContext as ViewModelBase);
				}
				else
				{
					_lastActiveDockableContent[activeContent.GetType()] = activeContent.DataContext as ViewModelBase;
				}
			}
		}

		static Dictionary<Type, ViewModelBase> _lastActiveDockableContent = new Dictionary<Type, ViewModelBase>();
		/// <summary>
		/// Contains the last active content window for each type of content.
		/// </summary>
		public static Dictionary<Type, ViewModelBase> LastActiveDockableContent
		{
			get { return _lastActiveDockableContent; }
		}
		/// <summary>
		/// Clear the dictionary when a layout or project is loaded.
		/// </summary>
		public static void ClearLastActiveDockableContent()
		{
			_lastActiveDockableContent = new Dictionary<Type, ViewModelBase>();
		}
		public static ViewModelBase LastActiveModelView { get; set; }

		#endregion // AvalonDock

		///// <summary>
		///// Application UnitScaler retrieved from MainWindowViewModel.
		///// </summary>
		//public Enterprixe.WPF.Tools.UnitsAndResultScale UnitScaler
		//{
		//	get { return App.Current.MainWindow.DataContext is MainWindowViewModel ? (App.Current.MainWindow.DataContext as MainWindowViewModel).ApplicationUnitScaler : null; }
		//}

		///// <summary>
		///// The selected nodes in the Project Explorer view.
		///// </summary>
		//public List<ProjectTreeNode> SelectedTreeNodes
		//{
		//	get
		//	{
		//		List<ProjectTreeNode> retval = new List<ProjectTreeNode>();
		//		ProjectExplorerViewModel treeViewVM = MainViewModel.ViewModelProjectExplorer;

		//		if (treeViewVM != null)
		//		{
		//			treeViewVM.SelectedItems.ForEach(si => retval.Add(si.DataNode));
		//		}

		//		return retval;
		//	}
		//}

		public static MainWindowViewModel MainViewModel
		{
			get
			{
				return App.Current.MainWindow == null ? null :
				  App.Current.MainWindow.DataContext as MainWindowViewModel;
			}
		}
	}
}
