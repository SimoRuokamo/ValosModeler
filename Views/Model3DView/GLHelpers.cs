using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NETCoreOnly
	using Epx.BIM.BaseTools;
#else
	using System.Windows.Media.Media3D;
#endif

namespace ValosModeler.Views.Model3DView
{

	public static class GLHelpers
	{
		public static Vector3 ToGLPoint(this Point3D point)
		{
			double dist = (float)point.X - point.X;
			if(dist > 10)
			{
				//System.Diagnostics.Debugger.Break();
			}
			return new Vector3((float)point.X, (float)point.Y, (float)point.Z);
		}
		public static Point3D ToEPXPoint(this Vector3 point)
		{
			return new Point3D(point.X, point.Y, point.Z);
		}
		public static Vector3[] Points3DToGLPointArray(IEnumerable<Point3D> points)
		{
			var glpoints = new Vector3[points.Count()];
			for (int ii = 0; ii < points.Count(); ii++)
				glpoints[ii] = points.ElementAt(ii).ToGLPoint();
			return glpoints;
		}

		public static uint[] Ints32ToUintArray(IEnumerable<int> indices)
		{
			var uints = new uint[indices.Count()];
			for (int ii = 0; ii < indices.Count(); ii++)
				uints[ii] = (uint)indices.ElementAt(ii);
			return uints;
		}

		public static System.Drawing.Color ColorFromMediaColor(System.Windows.Media.Color mc)
		{
			return System.Drawing.Color.FromArgb(mc.A, mc.R, mc.G, mc.B);
		}

		public static System.Drawing.Color ColorFromSDKColor(int mc)
		{
			return System.Drawing.Color.FromArgb(mc);
		}

		public static Matrix4 MediaMatrixToGLMatrix(Matrix3D m)
		{
			return new Matrix4(
				   (float)m.M11, (float)m.M12, (float)m.M13, (float)m.M14,
				   (float)m.M21, (float)m.M22, (float)m.M23, (float)m.M24,
				   (float)m.M31, (float)m.M32, (float)m.M33, (float)m.M34,
				   (float)m.OffsetX, (float)m.OffsetY, (float)m.OffsetZ, (float)m.M44
				);

		}
	}
}
