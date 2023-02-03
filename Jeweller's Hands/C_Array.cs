using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using Eto.Drawing;
using JewellersHands;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.UI;

namespace JewellersHands
{
    /// <summary>
    /// Utility class to handle array arguments and calculate offsets.
    /// </summary>
    class SampleCsArrayArgs
    {
        // Curve reference
        public Curve Curve { get; set; }

        // Gem reference
        public Brep Gem { get; set; }
        // Gem base point
        public Point3d basePoint { get; set; }


        // Object counts
        public int CountX { get; set; }

        // Distance between objects
        public double DistanceX { get; set; }

        public double ScaleX { get; set; }

        // The transformation plane
        public Rhino.Geometry.Plane Plane { get; set; }

        // The offsets to be used during transformation
        public List<Rhino.Geometry.Vector3d> Offsets;

        /// <summary>
        /// Constructor
        /// </summary>
        public SampleCsArrayArgs()
        {
            CountX = 1;
            ScaleX = 1;
            DistanceX = 0.0;
            Plane = Rhino.Geometry.Plane.WorldXY;
            Offsets = new List<Rhino.Geometry.Vector3d>();
        }

        /// <summary>
        /// Ensures that the capacity of the offset list is valid
        /// </summary>
        public void ValidateOffsets()
        {
            if (CountX < 1) CountX = 1;
            int offsetCount = CountX;
            if (Offsets.Count != offsetCount)
            {
                Offsets.Clear();
                Offsets.Capacity = offsetCount;
                Offsets.AddRange(Enumerable.Repeat(Rhino.Geometry.Vector3d.Zero, offsetCount));
            }
        }

        /// <summary>
        /// Resets the offset list
        /// </summary>
        public void ResetOffsets()
        {
            ValidateOffsets();
            for (int i = 0; i < Offsets.Count; i++)
                Offsets[i] = Rhino.Geometry.Vector3d.Zero;
        }

        /// <summary>
        /// Calculates the offsets (to be used later during transformation)
        /// </summary>
        public void CalculateOffsets()
        {
            /*
            Point3d[] gemBBCorners = Gem.GetBoundingBox(true).GetCorners();
            double gemRadius = gemBBCorners[0].DistanceTo(gemBBCorners[1]);
            Point3d[] dividePoints = Curve.DivideEquidistant(DistanceX + gemRadius);

            CountX = dividePoints.Length;

            ValidateOffsets();

            int index = 0;

            for (int x = 0; x < CountX; x++)
            {
                Rhino.Geometry.Vector3d offset = new Vector3d(dividePoints[x]);

                Offsets[index++] = (offset.IsValid) ? offset : Rhino.Geometry.Vector3d.Zero;
            }
            */ //Non-scalable version

            Point3d[] gemBBCorners = Gem.GetBoundingBox(true).GetCorners();
            double gemRadius = gemBBCorners[0].DistanceTo(gemBBCorners[1]) / 2; //Gemstones radius

            Point3d startPoint = Curve.PointAtStart;
            Point3d endPoint = Curve.PointAtEnd;

            List<Point3d> arrayIntersections = new List<Point3d>();
            arrayIntersections.Add(startPoint);
            int iteration = 0;
            double currentRadius = gemRadius;
            double nextRadius = gemRadius * ScaleX;
            Curve newCurve = Curve;
            Curve[] curveIntersection = null;
            Point3d[] pointIntersection = null;

            bool availableSpace = startPoint.DistanceTo(endPoint) > (gemRadius + DistanceX + nextRadius);

            while (availableSpace)
            {
                startPoint = newCurve.PointAtStart;
                Sphere sphere = new Sphere(startPoint, currentRadius + DistanceX + nextRadius);
                bool gotIntersection = Intersection.CurveBrep(newCurve, sphere.ToBrep(), 0.1, out curveIntersection, out pointIntersection);

                if (pointIntersection != null & gotIntersection)
                {
                    Point3d newPoint = pointIntersection[0];
                    arrayIntersections.Add(newPoint);
                    double intersectionParameter;
                    newCurve.ClosestPoint(newPoint, out intersectionParameter);
                    newCurve = newCurve.Trim(intersectionParameter, 1);
                    newCurve.Domain = new Interval(0, 1);
                    iteration++;
                }
                else
                {
                    break;
                }

                currentRadius = nextRadius;
                nextRadius *= ScaleX;

                startPoint = newCurve.PointAtStart;
                endPoint = newCurve.PointAtEnd;
                availableSpace = startPoint.DistanceTo(endPoint) > (currentRadius + DistanceX + nextRadius);
            }

            CountX = arrayIntersections.Count;

            ValidateOffsets();

            int index = 0;

            for (int x = 0; x < CountX; x++)
            {
                Rhino.Geometry.Vector3d offset = new Vector3d(arrayIntersections[x]);

                Offsets[index++] = (offset.IsValid) ? offset : Rhino.Geometry.Vector3d.Zero;
            }
        }

        public Brep[] RunArray()
        {
            Brep[] GemBreps = new Brep[CountX];

            for (int i = 0; i < Offsets.Count; i++)
            {
                // Skip the first one...
                if (!Offsets[i].IsZero)
                {
                    Rhino.Geometry.Transform xform = Rhino.Geometry.Transform.Translation(Offsets[i]);
                    Rhino.Geometry.Transform sform = Rhino.Geometry.Transform.Scale(basePoint, 1);
                    if (i > 0)
                    {
                        sform = Rhino.Geometry.Transform.Scale(basePoint, Math.Pow(ScaleX, i));
                    }
                    
                    if (xform.IsValid && xform != Rhino.Geometry.Transform.Identity)
                    {
                        Rhino.Geometry.Brep dupBrep = Gem.DuplicateBrep();
                        dupBrep.Transform(sform);
                        dupBrep.Transform(xform);
                        if (dupBrep.IsValid)
                            GemBreps[i] = dupBrep;
                    }
                }
            }
            return GemBreps;
        }
    }

    public class C_Array : Rhino.Commands.Command
    {
        public C_Array()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        public static C_Array Instance { get; private set; }

        public override string EnglishName => "JH_Array";

        public static Rhino.Commands.Result GetGem(Rhino.RhinoDoc doc, out Brep brep)
        {
            Rhino.Input.Custom.GetObject gemp = new Rhino.Input.Custom.GetObject();
            gemp.SetCommandPrompt("Get Gem");
            gemp.GeometryFilter = Rhino.DocObjects.ObjectType.Brep;
            gemp.GetMultiple(1, 1);
            gemp.EnablePreSelect(true, true);

            brep = null;

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above

                Rhino.Input.GetResult get_rc = gemp.Get();

                if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                else if (gemp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    foreach (var objref in gemp.Objects())
                    {
                        brep = objref.Brep();
                    }
                    if (brep.IsValid)
                    {
                        return Rhino.Commands.Result.Success;
                    }
                }
            }
            return Rhino.Commands.Result.Failure;
        }

        public static Rhino.Commands.Result GetBasePoint(Rhino.RhinoDoc doc, out Point3d basePoint)
        {
            Rhino.Input.Custom.GetPoint gemp = new Rhino.Input.Custom.GetPoint();
            gemp.SetCommandPrompt("Get Gem base point");

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above

                Rhino.Input.GetResult get_rc = gemp.Get();

                if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                else if (gemp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    basePoint = gemp.Point();
                    return Rhino.Commands.Result.Success;
                }
            }
            basePoint = new Point3d(0,0,0);
            return Rhino.Commands.Result.Failure;
        }

        public static Rhino.Commands.Result GetCurve(Rhino.RhinoDoc doc, out Curve curve)
        {
            Rhino.Input.Custom.GetObject gemp = new Rhino.Input.Custom.GetObject();
            gemp.SetCommandPrompt("Get Curve");
            gemp.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            gemp.GetMultiple(1, 1);
            gemp.EnablePreSelect(true, true);

            curve = null;

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above

                Rhino.Input.GetResult get_rc = gemp.Get();

                if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                else if (gemp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    foreach (var objref in gemp.Objects())
                    {
                        curve = objref.Curve();
                    }
                    if (curve.IsValid)
                    {
                        return Rhino.Commands.Result.Success;
                    }
                }
            }
            return Rhino.Commands.Result.Failure;
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Define a unit Brep box
            Rhino.Geometry.Point3d min = new Rhino.Geometry.Point3d(0.0, 0.0, 0.0);
            Rhino.Geometry.Point3d max = new Rhino.Geometry.Point3d(1.0, 1.0, 1.0);
            Rhino.Geometry.BoundingBox bbox = new Rhino.Geometry.BoundingBox(min, max);
            Brep boxbrep = Rhino.Geometry.Brep.CreateFromBox(bbox);
            Brep brep = Rhino.Geometry.Brep.CreateFromBox(bbox);
            Point3d basePoint = new Point3d(0,0,0);
            Curve curve = null;

            // Create and define the arguments of the array
            SampleCsArrayArgs args = new SampleCsArrayArgs();

            if (GetGem(doc, out brep) != Result.Success)
            {
                return Result.Failure;
            }
            if (GetBasePoint(doc, out basePoint) != Result.Success)
            {
                return Result.Failure;
            }
            if (GetCurve(doc, out curve) != Result.Success)
            {
                return Result.Failure;
            }


            args.Gem = brep;
            args.basePoint = basePoint;
            curve.Domain = new Interval(0, 1);
            args.Curve = curve;

            Rhino.Input.Custom.GetOption gp = new Rhino.Input.Custom.GetOption();

            Rhino.Input.Custom.OptionToggle previewGem = new Rhino.Input.Custom.OptionToggle(JHandsPlugin.Instance.PreviewArray, "Off", "On");
            Rhino.Input.Custom.OptionDouble distanceOption = new Rhino.Input.Custom.OptionDouble(10);
            Rhino.Input.Custom.OptionDouble scaleOption = new Rhino.Input.Custom.OptionDouble(1);
            gp.SetCommandPrompt("Array Amount");
            gp.AddOptionToggle("Preview", ref previewGem);
            gp.AddOptionDouble("Distance", ref distanceOption);
            gp.AddOptionDouble("Scale", ref scaleOption);

            JHandsPlugin.Instance.BrepDisplay.Enabled = true;

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above

                Rhino.Input.GetResult get_rc = gp.Get();

                if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                else if (get_rc == Rhino.Input.GetResult.Option)
                {
                    if (gp.OptionIndex() == 1) //Toggle
                    {
                        if (JHandsPlugin.Instance.PreviewArray)
                        {
                            JHandsPlugin.Instance.PreviewArray = false;
                            previewGem.CurrentValue = false;
                            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
                        }
                        else
                        {
                            JHandsPlugin.Instance.PreviewArray = true;
                            previewGem.CurrentValue = true;
                            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
                        }
                    }
                    else
                    {
                        args.DistanceX = distanceOption.CurrentValue;
                        args.ScaleX = scaleOption.CurrentValue;
                        args.CalculateOffsets();
                        Brep[] previewBreps = args.RunArray();
                        JHandsPlugin.Instance.BrepDisplay.SetObjects(previewBreps);
                    }

                }
                else if (gp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    args.DistanceX = distanceOption.CurrentValue;
                    args.CalculateOffsets();
                    Brep[] previewBreps = args.RunArray();
                    JHandsPlugin.Instance.BrepDisplay.SetObjects(previewBreps);
                }
            }

            JHandsPlugin.Instance.BrepDisplay.Enabled = false;

            Brep[] arrayBreps = args.RunArray();

            foreach(Brep arrayBrep in arrayBreps)
            {
                doc.Objects.AddBrep(arrayBrep);
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}