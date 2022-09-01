using System;
using System.Collections.Generic;
using System.Text;
using Wm = System.Windows.Media.Media3D;
using Win = System.Windows;
using Bim = Epx.BIM.BaseTools;

namespace ValosModeler
{
    public static class PointsConverters
    {
#if NETCoreOnly
		//3D
		public static Wm.Point3D WinPoint(this Bim.Point3D p)=> new Wm.Point3D(p.X, p.Y, p.Z);
		public static Bim.Point3D BimPoint(this Wm.Point3D p)=> new Bim.Point3D(p.X, p.Y, p.Z);
		public static Wm.Vector3D WinPoint(this Bim.Vector3D p)=> new Wm.Vector3D(p.X, p.Y, p.Z);
		public static Bim.Vector3D BimPoint(this Wm.Vector3D p)=> new Bim.Vector3D(p.X, p.Y, p.Z);
		//2D
		public static Win.Point WinPoint(this Bim.Point p)=> new Win.Point(p.X, p.Y);
		public static Bim.Point BimPoint(this Win.Point p)=> new Bim.Point(p.X, p.Y);
		public static Win.Vector WinPoint(this Bim.Vector p)=> new Win.Vector(p.X, p.Y);
		public static Bim.Vector BimPoint(this Win.Vector p)=> new Bim.Vector(p.X, p.Y);
#else
        // placeholders
        public static Wm.Point3D WinPoint(this Wm.Point3D p) => p;
        public static Wm.Vector3D WinPoint(this Wm.Vector3D p) => p;
        public static Win.Point WinPoint(this Win.Point p) => p;
        public static Win.Vector WinPoint(this Win.Vector p) => p;
        public static Wm.Point3D BimPoint(this Wm.Point3D p) => p;
        public static Wm.Vector3D BimPoint(this Wm.Vector3D p) => p;
        public static Win.Point BimPoint(this Win.Point p) => p;
        public static Win.Vector BimPoint(this Win.Vector p) => p;
#endif

        public static void PointsTest()
        {
#if NETCoreOnly
			Bim.Point3D bp = new Bim.Point3D(1000,1000,1000);
			Bim.Vector3D bv = new Bim.Vector3D(1000,1000,1000);

			Wm.RotateTransform3D rot =  new Wm.RotateTransform3D( new Wm.AxisAngleRotation3D(new Wm.Vector3D(1,0,1), 40));
			Wm.TranslateTransform3D trans= new Wm.TranslateTransform3D(new Wm.Vector3D(300,300,300));
			Wm.Transform3DGroup trf= new Wm.Transform3DGroup{Children = { trans, rot, trans } };

			var wm = trf.Value;
			Bim.Matrix3D bm= new Bim.Matrix3D(
				   wm.M11, wm.M12, wm.M13, wm.M14,
				   wm.M21, wm.M22, wm.M23, wm.M24,
				   wm.M31, wm.M32, wm.M33, wm.M34,
				   wm.OffsetX, wm.OffsetY, wm.OffsetZ, wm.M44 );

			wm.Invert();
			bm.Invert();
			wm.Invert();
			bm.Invert();

			var trfp = wm.Transform(bp.WinPoint());
			var trfv = wm.Transform(bv.WinPoint());

			var trfp2= bm.Transform(bp);
			var trfv2= bm.Transform(bv);

			if(trfp != trfp2.WinPoint())
			{
				//System.Diagnostics.Debugger.Break();
			}

			if(trfv != trfv2.WinPoint())
			{
				//System.Diagnostics.Debugger.Break();
			}

#endif
        }

    }


}
