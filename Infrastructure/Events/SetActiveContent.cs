using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
	/// <summary>
	/// Set the active docking window document.
	/// </summary>
	public class SetActiveContent
	{
		public const string MessageID = "SetActiveContent";

		public static void Publish(ViewModelBase viewModel)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<ViewModelBase>(typeof(SetActiveContent).Name, viewModel);
		}
	}
}
