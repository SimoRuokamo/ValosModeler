using Enterprixe.ValosUITools.Elements3D;
using Enterprixe.ValosUITools.Features;
using Enterprixe.WPF.Tools.Localization;
using Epx.BIM;
using Epx.BIM.GeometryTools;
using Epx.BIM.GridMesh;
using Epx.BIM.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ValosModeler.Infrastructure;
using Enterprixe.WPF.Tools.Elements3D;
using ValosModeler;

namespace ValosModeler.Views.Model3DView
{
    public class FeatureEngineViewModel : ViewModelBase, IFeatureEngine
	{
		public FeatureEngineViewModel(IPointInputToolController viewport, ModelViewViewModel windowViewModel)
		{
			Viewport = viewport;
			ModelViewModel = windowViewModel;
			//OriginalReferencePlanesSelectionEnabled = ModelViewModel.IsReferencePlanesSelectionEnabled;
		}


		public IPointInputToolController Viewport { get; set; }
		public ModelViewViewModel ModelViewModel { get; set; }

		#region RunPlugin (viewport part)

		/// <summary>
		/// 0 = none, 1 = pre inputs, 2 = show dialog, 3 = post inputs, 4 = end
		/// </summary>
		private int _pluginState = 0;
		private PluginInput _pluginPreInput;
		private PluginInput _pluginPostInput;
		private PluginInfo _activeCommandPlugin;

		public void BeginPlugin(PluginInfo activeCommandPlugin)
		{
			// plugin localization is handled inside plugin
			_activeCommandPlugin = activeCommandPlugin;
			BeginRunPlugin();
			_pluginState = 0;
			_pluginPreInput = ActivePlugin.GetNextPreDialogInput(null);
			//_pluginPostInputs = WindowViewModel.ActivePlugin.GetPostDialogInputs();
			//if (_pluginPreInput == null) _pluginPreInput = new List<PluginInput>();
			//if (_pluginPostInput == null) _pluginPostInput = new List<PluginInput>();

			if (_pluginPreInput != null)
			{
				_pluginState = 1;
				ModelViewModel.SetCommandStatus(_pluginState, CultureManager.GetLocalizedString(_pluginPreInput.Prompt));
				Viewport.IsPickPointOperation = _pluginPreInput is PluginPointInput;
				if (!(ActivePlugin is IModelViewFeature)) IsModelViewPluginRunning = true;

			}
			else
			{
				_pluginState = 2;
				Viewport.IsPickPointOperation = false;
				if (!(ActivePlugin is IModelViewFeature)) IsModelViewPluginRunning = false;

				var activePlugin = ActivePlugin;//PluginDialogClosed call inside plugin set ActivePlugin to null. PluginDialogClosed call inside plugin is needed in case where plugin uses modal dialog.
				bool dialogResult = ShowPluginDialog();
				if (!dialogResult)
				{
					if (activePlugin is IModelViewFeature)
					{
					}
					else
					{
						PluginDialogClosed(dialogResult);
					}
				}
				else
				{
					PluginDialogClosed(dialogResult);
				}
			}
		}

		public void PluginMouseUp(Point3D helperPoint, BaseDataNode commandNode, PluginKeyInput keyInput = null)
		{
			Viewport.PointInputTool.EndInput();
			string newPrompt = string.Empty;
			switch (_pluginState)
			{
				case 1:
					newPrompt = string.Empty;
					var preInput = keyInput ?? _pluginPreInput;

					if (SetPluginInput(preInput, helperPoint, commandNode))
					{						
						_pluginPreInput = ActivePlugin.GetNextPreDialogInput(preInput); //TODO preInput or _pluginPreInput?
						if ((preInput is PluginPointInput))
						{
							if (!(_pluginPreInput is PluginPointInput) || (preInput as PluginPointInput).EndContinuousInput == true)
								Viewport.PointInputTool.EndInput();
						}
						if (_pluginPreInput != null)
						{
							newPrompt = _pluginPreInput.Prompt;
						}
					}
					else if (preInput != null && !string.IsNullOrEmpty(preInput.ErrorMessage))
					{
						newPrompt = preInput.ErrorMessage;
					}
					else if (_pluginPreInput != null && !string.IsNullOrEmpty(_pluginPreInput.Prompt))
					{
						newPrompt = _pluginPreInput.Prompt;
					}

					if (_pluginPreInput == null)
					{
						Viewport.PointInputTool.EndInput();
						_pluginState = 2;
						// plugin localization is handled inside plugin
						ModelViewModel.SetCommandStatus(_pluginState, string.Empty);
						Viewport.IsPickPointOperation = false;
						if (!(ActivePlugin is IModelViewFeature)) IsModelViewPluginRunning = false;
						var activePlugin = ActivePlugin;//PluginDialogClosed call inside plugin set ActivePlugin to null. PluginDialogClosed call inside plugin is needed in case where plugin uses modal dialog.
						bool dialogResult = ShowPluginDialog();
						if (!dialogResult)
						{
							if (activePlugin is IModelViewFeature)
							{
							}
							else
							{
								PluginDialogClosed(dialogResult);
							}
						}
						else
						{
							PluginDialogClosed(dialogResult);
						}
					}
					else
					{
						ModelViewModel.SetCommandStatus(_pluginState, CultureManager.GetLocalizedString(newPrompt));
						Viewport.IsPickPointOperation = _pluginPreInput is PluginPointInput;
					}
					break;

				case 3:
					if (_pluginPostInput != null)
					{
						newPrompt = string.Empty;
						var postInput = keyInput ?? _pluginPostInput;
						if (SetPluginInput(postInput, helperPoint, commandNode))
						{
							_pluginPostInput = ActivePlugin.GetNextPostDialogInput(postInput);//TODO preInput or _pluginPreInput?
							if ((postInput is PluginPointInput))
							{
								if (!(_pluginPostInput is PluginPointInput) || (postInput as PluginPointInput).EndContinuousInput == true)
									Viewport.PointInputTool.EndInput();
							}
							if (_pluginPostInput != null)
							{
								newPrompt = _pluginPostInput.Prompt;
							}
						}
						else if (postInput != null && !string.IsNullOrEmpty(postInput.ErrorMessage))
						{
							newPrompt = postInput.ErrorMessage;
						}
						else if (!string.IsNullOrEmpty(_pluginPostInput.Prompt))
						{
							newPrompt = _pluginPostInput.Prompt;
						}
					}

					if (_pluginPostInput == null)
					{
						_pluginState = 4;
						ModelViewModel.SetCommandStatus(_pluginState, string.Empty);
						Viewport.PointInputTool.EndInput();
						Viewport.IsPickPointOperation = false;
						EndRunPlugin();
						ModelViewModel.ActiveCommand = ModelViewViewModel.DesignCommands.None;
					}
					else
					{
						ModelViewModel.SetCommandStatus(_pluginState, CultureManager.GetLocalizedString(newPrompt));
						Viewport.IsPickPointOperation = _pluginPostInput is PluginPointInput;
					}
					break;

				default:
					break;
			}
		}

		public void PluginMouseMove(Point viewportPosition, SnapPoint3D snapPoint, SnapPoint3D mouseDownGlobalPoint, Visuals.IModelPartVisual designCommandVisual)
		{
			// plugin localization is handled inside plugin

			switch (_pluginState)
			{
				case 1:
					if (_pluginPreInput is PluginPointInput)
					{
					}
					else if (_pluginPreInput is PluginObjectInput)
					{
						if (designCommandVisual is HighlightableVisual3D)
						{
							(designCommandVisual as HighlightableVisual3D).Highlight(new DiffuseMaterial(Brushes.Lime));
						}
					}
					break;

				case 3:
					if (_pluginPostInput is PluginPointInput)
					{
					}
					else if (_pluginPostInput is PluginObjectInput)
					{
						if (designCommandVisual is HighlightableVisual3D)
						{
							(designCommandVisual as HighlightableVisual3D).Highlight(new DiffuseMaterial(Brushes.Lime));
						}
					}
					break;

				default:
					break;
			}

			if (mouseDownGlobalPoint != null)
			{
				var mpoint=(GeometryMath.RoundPoint(snapPoint.Point) - mouseDownGlobalPoint.Point).WinPoint();
				Viewport.PointInputTool.MoveInput(mpoint);
			}
			if (ActivePlugin is IModelViewFeature && _pluginState > 0 && _pluginState < 4)
				(ActivePlugin as IModelViewFeature).MouseMoved(snapPoint.Point.WinPoint());

			ModelViewModel.OnUpdate3DHelpers(string.Empty);
		}

		public void PluginKeyDown(Key key, Point3D helperPoint, BaseDataNode commandNode)
		{
			PluginInput currentInput = null;
			switch (_pluginState)
			{
				case 1:
					if (_pluginPreInput != null)
					{
						currentInput = _pluginPreInput;
					}
					break;

				case 3:
					if (_pluginPostInput != null)
					{
						currentInput = _pluginPostInput;
					}
					break;

				default:
					break;
			}

			if (currentInput != null && !(currentInput is PluginKeyInput)) // current input is not requesting a key but key can be a modifier
			{
				PluginMouseUp(helperPoint, commandNode, new PluginKeyInput(key) { Index = currentInput.Index });
			}
			else if (currentInput is PluginKeyInput) // current input is requesting a key
			{
				(currentInput as PluginKeyInput).InputKey = key;
				PluginMouseUp(helperPoint, commandNode, null);
			}
			//else
			//	PluginMouseUp(new PluginKeyInput(key));
		}

		private bool SetPluginInput(PluginInput input, Point3D point, BaseDataNode node)
		{
			// plugin localization is handled inside plugin
			bool retval = false;

			if (input is PluginPointInput)
			{
				(input as PluginPointInput).Point = point.BimPoint();
			}
			else if (input is PluginObjectInput)
			{
				(input as PluginObjectInput).Object = node;
				(input as PluginObjectInput).Point = point.BimPoint();
			}

			if (_pluginState == 1 && ActivePlugin.IsPreDialogInputValid(input))
			{
				retval = true;
			}
			else if (_pluginState == 3 && ActivePlugin.IsPostDialogInputValid(input))
			{
				retval = true;
			}
			else
			{
				retval = false;
				if (!string.IsNullOrEmpty(input.ErrorMessage))
					ModelViewModel.SetCommandStatus(_pluginState, input.ErrorMessage);
			}

			if (input is PluginPointInput)
			{
				if (retval)
				{
					Viewport.PointInputTool.StartInput(point);
				}
				else
				{
					// input was not valid -> start from previous point
					var prevPointInput = _pluginPreInput as Epx.BIM.Plugins.PluginPointInput;
					if (prevPointInput != null)
					{
						Viewport.PointInputTool.StartInput(prevPointInput.Point.WinPoint());
					}
				}
			}

			if (retval)
			{
				if (input is PluginObjectInput)
				{
					Viewport.AddSelectedVisual();
				}
			}

			return retval;
		}

		private void EndPlugin()
		{
			_pluginState = 0;
			_pluginPreInput = null;
			_pluginPostInput = null;
			Viewport.IsPickPointOperation = false;
			Viewport.PointInputTool.EndInput();
		}

		#region IFeatureEngine
		public void PluginDialogClosed(bool dialogResult)
		{
			if (!dialogResult)
			{
				ModelViewModel.SetCommandStatus(_pluginState, string.Empty);
				EndRunPlugin(true);
			}
			else
			{
				_pluginPostInput = ActivePlugin.GetNextPostDialogInput(null);
				//if (_pluginPostInput == null) _pluginPostInput = new List<PluginInput>();

				if (_pluginPostInput != null)
				{
					_pluginState = 3;
					ModelViewModel.SetCommandStatus(_pluginState, CultureManager.GetLocalizedString(_pluginPostInput.Prompt));
					Viewport.IsPickPointOperation = _pluginPostInput is PluginPointInput;
					if (!(ActivePlugin is IModelViewFeature)) IsModelViewPluginRunning = true;
				}
				else
				{
					_pluginState = 4;
					ModelViewModel.SetCommandStatus(_pluginState, string.Empty);
					Viewport.PointInputTool.EndInput();
					Viewport.IsPickPointOperation = false;
					EndRunPlugin();
				}
			}
		}

		public void PluginUpdate3D(bool redrawOnly)
		{
			if (ActivePlugin is IModelViewFeature)
				DoPluginUpdate3D(redrawOnly);
		}
		#endregion

		#endregion

		#region RunPluginAction (viewmodel part)

		private bool _IsModelViewPluginRunning = false;
		public bool IsModelViewPluginRunning
		{
			get { return _IsModelViewPluginRunning; }
			set
			{
				if (_IsModelViewPluginRunning != value)
				{
					_IsModelViewPluginRunning = value;
					//ModelViewModel.ModelDesignCommandInProgress = value;
					OnPropertyChanged("IsModelViewPluginRunning");
				}
			}
		}
		
		/// <summary>
		/// Dimension lines created by the feature.
		/// </summary>
		public List<ModelDimensionBase> DimensionLines { get; set; }
		/// <summary>
		/// UIContent created by the feature.
		/// </summary>
		public List<ModelUIContentElement> UIContentElements { get; set; }

		public string ActivePluginRibbonHeaderText
		{
			get
			{
				return CultureManager.GetLocalizedString("Plugin Tool");
			}
		}

		private Dictionary<string, PluginTool> _pluginLastExecuted = new Dictionary<string, PluginTool>();
		public PluginTool ActivePlugin { get; set; }
		public bool IsPluginRunning
		{
			get { return ActivePlugin != null; }
		}

		/// <summary>
		/// Creates the ActivePlugin.
		/// </summary>
		private void BeginRunPlugin()
		{
			if (_activeCommandPlugin == null)
			{
				ActivePlugin = null;
				return;
			}

			if (ModelViewModel.ActiveCommand == ModelViewViewModel.DesignCommands.EditPlugin)
			{
				BeginEditPlugin();
				return;
			}

			ActivePlugin = FeatureManager.GetPluginTool(_activeCommandPlugin.PluginType, _activeCommandPlugin.StartAttributes);
			if (ActivePlugin is IModelViewFeature) IsModelViewPluginRunning = true;

			BaseDataNode target = _activeCommandPlugin.MasterNode.GetParent<DataModel>().GetTargetFolder();

			ActivePlugin.OnSetCreateMode(_activeCommandPlugin.MasterNode, target);
			string ss = _activeCommandPlugin.PluginType.FullName.ToString();
			if (_pluginLastExecuted.ContainsKey(ss))
			{
				ActivePlugin.InitializePluginParameters(_pluginLastExecuted[ss]);
			}
			else
			{
				ActivePlugin.InitializePluginParameters(null);
			}
			if (ActivePlugin is IModelViewFeature)
			{
				(ActivePlugin as IModelViewFeature).FeatureEngine = this;
			}
		}

		private void BeginEditPlugin()
		{
			if (_activeCommandPlugin == null)
			{
				ActivePlugin = null;
				return;
			}

			IPluginNode editedNode = _activeCommandPlugin.MasterNode as IPluginNode;
			if (editedNode == null) return;
			(editedNode as BaseDataNode).Parent.RemoveChild(editedNode as BaseDataNode);
			ActivePlugin = FeatureManager.GetPluginTool(editedNode.AssemblyName, editedNode.FullClassName,
				editedNode is IPluginNodeWithStartAttributes ? (editedNode as IPluginNodeWithStartAttributes).StartAttributes :null);
			if (ActivePlugin is IModelViewFeature)
			{
				IsModelViewPluginRunning = true;
				(ActivePlugin as IModelViewFeature).FeatureEngine = this;
			}
			ActivePlugin.OnSetEditMode(editedNode);
			string ss = ActivePlugin.GetType().FullName.ToString();
			if (_pluginLastExecuted.ContainsKey(ss))
			{
				ActivePlugin.InitializePluginParameters(_pluginLastExecuted[ss]);
			}
			else
			{
				ActivePlugin.InitializePluginParameters(null);
			}
		}

		private bool ShowPluginDialog()
		{
			if (!ActivePlugin.IsInEditMode && ActivePlugin is IRibbonFeature)//clear predialog ribbontab content
			{
				Infrastructure.Events.ShowRibbonContextualTabs.Publish(Tuple.Create(ActivePlugin, this));
			}
			var oldCursor = Mouse.OverrideCursor;
			Mouse.OverrideCursor = null;
			bool applyEnabled = false;
			bool res = ActivePlugin.ShowDialog(applyEnabled);
			Mouse.OverrideCursor = oldCursor;
			return res;
		}

		public void EndRunPlugin(bool isCancel = false)
		{
			EndPlugin();

			if (ModelViewModel.ActiveCommand == ModelViewViewModel.DesignCommands.EditPlugin)
			{
				EndEditPlugin(isCancel);
				ModelViewModel.ActiveCommand = ModelViewViewModel.DesignCommands.None;
				return;
			}

			PluginTool plugin = ActivePlugin;
			BaseDataNode pluginMaster = plugin.Master;
			BaseDataNode pluginTarget = plugin.Target;
			if (pluginTarget == null) pluginTarget = pluginMaster; // assume Master is the target if Target was validated by the plugin but it is null.
			_pluginTempLineDimensions.Clear();
			DimensionLines = null;
			UIContentElements = null;
			if (pluginTarget != null) _pluginTempNodes.ForEach(n => pluginTarget.RemoveChild(n));
			_pluginTempNodes.Clear();
			_tempHiddenNodes.ForEach(n => n.IsShownIn3D = true);
			Infrastructure.Events.Update3D.Publish(_tempHiddenNodes);
			_tempHiddenNodes.Clear();
			
			if (!isCancel)
			{
				Infrastructure.Events.DesignCommand.CommitDragObject(null);
				Action doAction, undoAction;
				List<BaseDataNode> update3dnodes;
				List<BaseDataNode> returnedNodes = plugin.Excecute(out doAction, out undoAction, out update3dnodes);
				IPluginNode pluginNode = plugin.PluginDataNode;
				PluginTool.CustomAttributes pluginAttributes = plugin.PluginAttributes;

				if (doAction != null && undoAction != null)
				{
					if (pluginNode is BaseDataNode)
					{
						pluginNode.AssemblyName = plugin.AssemblyName;
						pluginNode.FullClassName = plugin.FullClassName;

						returnedNodes.Add(pluginNode as BaseDataNode);
					}

					//ActionManager.RecordAction(new .AdvancedCallMethodAction(
					//	delegate // do
						{
							doAction.Invoke();
							EndPluginActionPostProcess(pluginMaster, update3dnodes, pluginAttributes);
						}
						//,
						//delegate // undo
						//{
						//	undoAction.Invoke();
						//	EndPluginActionPostProcess(pluginMaster, update3dnodes, pluginAttributes);
						//	Infrastructure.Events.ShowRibbonContextualTabs.Publish(null);
						//}, pluginMaster, "RunPlugin", "Run Plugin", (int)plugin.ConsequenceType, pluginNode));
				}
				else if ((returnedNodes.Count == 0 && pluginNode != null) ||
					(returnedNodes.Count > 0 && !returnedNodes.Any(n => n is IPluginNode) && pluginNode == null))
				{
					if (pluginNode is BaseDataNode)
					{
						pluginNode.AssemblyName = plugin.AssemblyName;
						pluginNode.FullClassName = plugin.FullClassName;

						returnedNodes.Add(pluginNode as BaseDataNode);
					}

					//ActionManager.RecordAction(new AdvancedCallMethodAction(
					//	delegate // do
						{
							foreach (var node in returnedNodes)
							{
								pluginTarget.AddChild(node);
							}
							EndPluginActionPostProcess(pluginMaster, update3dnodes, pluginAttributes);
						}
						//,
						//delegate // undo
						//{
						//	foreach (var node in returnedNodes)
						//	{
						//		pluginTarget.RemoveChild(node);
						//	}
						//	EndPluginActionPostProcess(pluginMaster, update3dnodes, pluginAttributes);
						//	Infrastructure.Events.ShowRibbonContextualTabs.Publish(null);
						//}, pluginMaster, "RunPlugin", "Run Plugin", (int)plugin.ConsequenceType, pluginNode));
				}
				else
				{
					// plugin did not follow Execute instructions					
					EndPluginActionPostProcess(pluginMaster, update3dnodes, PluginTool.CustomAttributes.Attribute1 | PluginTool.CustomAttributes.Attribute2);
				}

				Infrastructure.Events.ShowRibbonContextualTabs.Publish(pluginMaster);
				ModelViewModel.UpdateDimensionsOverlay();

				if (!_pluginLastExecuted.ContainsKey(ActivePlugin.GetType().FullName))
				{
					_pluginLastExecuted.Add(ActivePlugin.GetType().FullName, ActivePlugin);
				}
				else
				{
					_pluginLastExecuted[ActivePlugin.GetType().FullName] = ActivePlugin;
				}

				//is continuous
				if (_activeCommandPlugin.IsContinuous)
				{
					BeginPlugin(_activeCommandPlugin);
					//Mediator.NotifyColleagues<ModelViewViewModel.DesignCommands>(MediatorMessages.DesignCommandInProgressChanged, ModelViewViewModel.DesignCommands.RunPlugin);
					return;
				}
			}
			else // is cancel
			{
				Infrastructure.Events.DesignCommand.CancelDragObject(null);
				if (plugin is IModelViewFeature)
				{
					(plugin as IModelViewFeature).CancelFeature();
				}
				ModelViewModel.OnUpdate3D(null);
				ModelViewModel.UpdateDimensionsOverlay();
			}
			IsModelViewPluginRunning = false;
			ActivePlugin = null;
			Mouse.OverrideCursor = null;
			ModelViewModel.ActiveCommand = ModelViewViewModel.DesignCommands.None;
		}

		public void EndEditPlugin(bool isCancel = false)
		{
			PluginTool plugin = ActivePlugin;
			BaseDataNode pluginTarget = plugin.Target;
			BaseDataNode pluginMaster = plugin.Master;
			if (pluginTarget == null) pluginTarget = pluginMaster; // assume Master is the target if Target was validated by the plugin but it is null.
			_pluginTempLineDimensions.Clear();
			DimensionLines = null;
			UIContentElements = null;
			if (pluginTarget != null) _pluginTempNodes.ForEach(n => pluginTarget.RemoveChild(n));
			_pluginTempNodes.Clear();

			_tempHiddenNodes.ForEach(n => n.IsShownIn3D = true);
			Infrastructure.Events.Update3D.Publish(_tempHiddenNodes);
			_tempHiddenNodes.Clear();
			
			if (!isCancel)
			{
				Action doAction, undoAction;
				List<BaseDataNode> update3dnodes;
				List<BaseDataNode> returnedNodes = plugin.Excecute(out doAction, out undoAction, out update3dnodes);
				IPluginNode pluginNode = plugin.PluginDataNode;
				PluginTool.CustomAttributes pluginAttributes = plugin.PluginAttributes;

				if (doAction != null && undoAction != null)
				{
					if (pluginNode is BaseDataNode)
					{
						pluginNode.AssemblyName = plugin.AssemblyName;
						pluginNode.FullClassName = plugin.FullClassName;
					}

					//ActionManager.RecordAction(new AdvancedCallMethodAction(
					//	delegate // do
						{
							doAction.Invoke();
							EndPluginActionPostProcess(pluginMaster, update3dnodes, pluginAttributes);
						}
						//,
						//delegate // undo
						//{
						//	undoAction.Invoke();
						//	EndPluginActionPostProcess(pluginMaster, update3dnodes, pluginAttributes);
						//	Infrastructure.Events.ShowRibbonContextualTabs.Publish(null);
						//}, pluginMaster, "EditPlugin", "Edit Plugin", (int)plugin.ConsequenceType, pluginNode));
				}
				else if ((returnedNodes.Count == 0 && pluginNode != null) ||
					(returnedNodes.Count > 0 && !returnedNodes.Any(n => n is IPluginNode) && pluginNode == null))
				{
					if (pluginNode is BaseDataNode)
					{
						pluginNode.AssemblyName = plugin.AssemblyName;
						pluginNode.FullClassName = plugin.FullClassName;

						returnedNodes.Add(pluginNode as BaseDataNode);
					}

					//ActionManager.RecordAction(new AdvancedCallMethodAction(
					//	delegate // do
						{
							pluginTarget.RemoveChild(pluginMaster as BaseDataNode);
							pluginTarget.AddChild(pluginNode as BaseDataNode);
							EndPluginActionPostProcess(pluginMaster, update3dnodes, pluginAttributes);
						}
						//,
						//delegate // undo
						//{
						//	pluginTarget.RemoveChild(pluginNode as BaseDataNode);
						//	pluginTarget.AddChild(pluginMaster as BaseDataNode);
						//	EndPluginActionPostProcess(pluginMaster, update3dnodes, pluginAttributes);
						//}, pluginMaster, "EditPlugin", "Edit Plugin", (int)plugin.ConsequenceType, pluginNode));
				}
				Infrastructure.Events.ShowRibbonContextualTabs.Publish(null);
				ModelViewModel.UpdateDimensionsOverlay();

				if (!_pluginLastExecuted.ContainsKey(ActivePlugin.GetType().FullName))
				{
					_pluginLastExecuted.Add(ActivePlugin.GetType().FullName, ActivePlugin);
				}
				else
				{
					_pluginLastExecuted[ActivePlugin.GetType().FullName] = ActivePlugin;
				}
			}
			else // is cancel
			{
				if (plugin is IModelViewFeature)
				{
					(plugin as IModelViewFeature).CancelFeature();
				}
				pluginMaster.Parent.AddChild(pluginMaster);
				ModelViewModel.OnUpdate3D(null);
				ModelViewModel.UpdateDimensionsOverlay();
			}
			IsModelViewPluginRunning = false;
			ActivePlugin = null;
			Mouse.OverrideCursor = null;
		}

		/// <summary>
		/// Update the model and equal trusses and truss cuts etc.
		/// </summary>
		/// <param name="pluginMaster">The plugin master.</param>
		/// <param name="update3dnodes">The update3dnodes.</param>
		/// <param name="pluginAttributes">The plugin attributes. Attribute1 do not update equal trusses. Attribute2 updates all truss cuts from the building.</param>
		/// <param name="invalidateAllTrusses">if set to <c>true</c> [invalidate all trusses].</param>
		/// <returns>IEnumerable&lt;PlanarStructure&gt;.</returns>
		private IEnumerable<BaseDataNode> EndPluginActionPostProcess(BaseDataNode pluginMaster, IEnumerable<BaseDataNode> update3dnodes, PluginTool.CustomAttributes pluginAttributes)
		{
			List<BaseDataNode> retval = new List<BaseDataNode>();
			ModelViewModel.OnUpdate3D(null);
			Infrastructure.Events.Update3D.Publish(pluginMaster);

			Infrastructure.Events.Update3D.Publish(update3dnodes);

			//if (pluginAttributes.HasFlag(PluginTool.CustomAttributes.Attribute2))
			//{
			//}

			Infrastructure.Events.ModelChanged.Publish(this);

			return retval;
		}
		public bool IsTemporaryNode(BaseDataNode node)
		{
			if (node == null)
				return false;
			var tmpNodes = _pluginTempNodes.ToList();
			_pluginTempNodes.ForEach(t => tmpNodes.AddRange(t.GetDescendantNodes<BaseDataNode>()));
			return tmpNodes.Contains(node);
		}

		private List<BaseDataNode> _tempHiddenNodes = new List<BaseDataNode>();
		private List<BaseDataNode> _pluginTempNodes = new List<BaseDataNode>();
		private List<ModelUIContentElement> _pluginTempContentElements = new List<ModelUIContentElement>();
		/// <summary>
		/// A lsit of temporary line dimensions for the plugin.
		/// </summary>
		private List<ModelDimensionBase> _pluginTempLineDimensions = new List<ModelDimensionBase>();
		private void DoPluginUpdate3D(bool redrawOnly)
		{
			if (!(ActivePlugin is IModelViewFeature)) return;
			IModelViewFeature modelViewPlugin = ActivePlugin as IModelViewFeature;

			BaseDataNode pluginTarget = ActivePlugin.Target;
			BaseDataNode pluginMaster = ActivePlugin.Master;
			IPluginNode pluginNode = ActivePlugin.PluginDataNode;
			if (pluginTarget == null) pluginTarget = pluginMaster; // assume Master is the target if Target was validated by the plugin but it is null.


			var hiddenNodes = modelViewPlugin.HiddenNodes;
			if (hiddenNodes != null)
			{
				foreach (BaseDataNode bdn in hiddenNodes.ToList())
				{
					var nodes = bdn.GetDescendantNodes<BaseDataNode>().Where(n => n.IsShownIn3D);
					_tempHiddenNodes.AddRange(nodes);
				}
				_tempHiddenNodes.AddRange(hiddenNodes);
				_tempHiddenNodes.ForEach(n => n.IsShownIn3D = false);
				Infrastructure.Events.Update3D.PublishAsync(_tempHiddenNodes);
			}

			// add model view nodes to model
			if (!redrawOnly)
			{
				var newNodes = modelViewPlugin.ModelViewNodes.ToList();
				if(_pluginTempNodes.Count == 0 && newNodes.Count != 0)
					Infrastructure.Events.DesignCommand.StartDragObject(_pluginTempNodes);
				_pluginTempNodes.ForEach(n => pluginTarget.RemoveChild(n));
				_pluginTempNodes.Clear();
				newNodes.ForEach(n => pluginTarget.AddChild(n));
				_pluginTempNodes.AddRange(newNodes);
				//if (ActivePlugin.PluginAttributes.HasFlag(PluginTool.CustomAttributes.Attribute2))
				//{
				//}

				Infrastructure.Events.Update3D.Publish(modelViewPlugin.ModelViewNodes);
				ModelViewModel.OnUpdate3D(null);
				// dimension lines
				_pluginTempLineDimensions.Clear();
				var modelDimensions = modelViewPlugin.ModelViewDimensions;
				if (modelDimensions != null) _pluginTempLineDimensions.AddRange(modelDimensions);
				DimensionLines = new List<ModelDimensionBase>(_pluginTempLineDimensions);
				//overlay content elements
				_pluginTempContentElements.Clear();
				var modelOverlayContents = modelViewPlugin.ModelViewOverlayContents;
				if (modelOverlayContents != null) _pluginTempContentElements.AddRange(modelOverlayContents);
				UIContentElements = new List<ModelUIContentElement>(_pluginTempContentElements);
			}
			else
			{
				Infrastructure.Events.Update3D.Publish(modelViewPlugin.ModelViewNodes);
			}

			ModelViewModel.UpdateDimensionsOverlay();
		}

		#endregion

		public System.Windows.Controls.Ribbon.RibbonGroup ActivePluginRibbonGroup
		{
			get
			{
				if (ActivePlugin is IRibbonFeature && _pluginState == 1)
				{
					var content = (ActivePlugin as IRibbonFeature).PreDialogRibbonTabGroup;
					content.DataContext = (ActivePlugin as IRibbonFeature).PreDialogRibbonViewModel;
					return content;
				}
				return null;
			}
		}

		#region EndCommandCommand
		RelayCommand _endCommandCommand;
		public ICommand EndCommandCommand
		{
			get
			{
				if (_endCommandCommand == null)
					_endCommandCommand = new RelayCommand(execute => this.ExecuteEndCommand(execute), canexecute => this.CanExecuteEndCommand(canexecute));

				return _endCommandCommand;
			}
		}

		protected virtual bool CanExecuteEndCommand(object param)
		{
			return true;
		}

		protected virtual void ExecuteEndCommand(object param)
		{
			Infrastructure.Events.DesignCommand.End(this);
		}
		#endregion

		#region EnterCommand
		RelayCommand _enterCommand;
		/// <summary>
		/// Sends and Enter keypress to the plugin.
		/// </summary>
		public ICommand EnterCommand
		{
			get
			{
				if (_enterCommand == null)
					_enterCommand = new RelayCommand(execute => this.ExecuteEnter(execute), canexecute => this.CanExecuteEnter(canexecute));
				return _enterCommand;
			}
		}

		private bool CanExecuteEnter(object parameter)
		{
			return true;
		}

		private void ExecuteEnter(object parameter)
		{
			PluginKeyDown(Key.Enter, new Point3D(), null);
		}
		#endregion //EnterCommand
	}
}
