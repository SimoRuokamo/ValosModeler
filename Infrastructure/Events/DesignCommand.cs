using Epx.BIM;
using Epx.BIM.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
	public class DesignCommandPayload
	{
		public DesignCommandPayload(bool isInProgress, object sender)
		{
			Sender = sender;
		}
		public DesignCommandPayload(PluginInfo plugin, object sender)
		{
			Plugin = plugin;
			Sender = sender;
		}

		public PluginInfo Plugin { get; set; }
		public object Sender { get; set; }
	}

	public class DesignCommand
	{
		/// <summary>
		/// 
		/// </summary>
		public const string ChangedID = "DesignCommandChanged";
		public const string RunID = "RunDesignCommand";
		public const string EditID = "EditDesignCommand";
		public const string EndID = "EndDesignCommand";
		public const string CancelID = "CancelDesignCommand";
		//dragging objects
		public const string StartDragging = "StartDragging";
		public const string CommitDragging = "CommitDragging";
		public const string CancelDragging = "CancelDragging";
		//
		public static void Changed(bool isInProgress, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<DesignCommandPayload>(ChangedID, new DesignCommandPayload(isInProgress, sender));
		}
		public static void Run(PluginInfo plugin, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<DesignCommandPayload>(RunID, new DesignCommandPayload(plugin, sender));
		}
		public static void Edit(PluginInfo plugin, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<DesignCommandPayload>(EditID, new DesignCommandPayload(plugin, sender));
		}
		public static void End(object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<DesignCommandPayload>(EndID, new DesignCommandPayload(null, sender));
		}
		//dragging
		public static void StartDragObject(object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<DesignCommandPayload>(StartDragging, new DesignCommandPayload(null, sender));
		}
		public static void CommitDragObject(object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<DesignCommandPayload>(CommitDragging, new DesignCommandPayload(null, sender));
		}
		public static void CancelDragObject(object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<DesignCommandPayload>(CancelDragging, new DesignCommandPayload(null, sender));
		}


	}
}
