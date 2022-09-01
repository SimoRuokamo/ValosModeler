using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
	class Update3DMappings
	{
		/// <summary>
		/// If part has truss as parent redraw truss, otherwise if it has building as parent, redraw building.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns>BaseDataNode.</returns>
		public static BaseDataNode GetUpdate3DNode(BaseDataNode node)
		{
			return node;
		}

	}
}
