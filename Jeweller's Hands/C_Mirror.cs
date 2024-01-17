using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.UI;

namespace JewellersHands
{
    public class C_Mirror : Command
    {
        public C_Mirror()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_Mirror Instance { get; private set; }

        public override string EnglishName => "JH_Mirror";

        bool isBrep = false;
        bool isCurve = false;
        bool deleteSelected = JHandsPlugin.Instance.deleteSelected;
        bool joinSelected = JHandsPlugin.Instance.joinSelected;
        List<Brep> newBrep = new List<Brep>();
        List<Brep> originalBrep = new List<Brep>();
        List<ObjectAttributes> originalBrepAtt = new List<ObjectAttributes>();
        List<Curve> newCurve = new List<Curve>();
        List<Curve> originalCurve = new List<Curve>();
        List<ObjectAttributes> originalCurveAtt = new List<ObjectAttributes>();
        List<Guid> IDs = new List<Guid>();
        Plane CPlane;
        int Quadrant = 1;
        bool firstPass = true;

        private List<Brep> MirrorNewBrep(List<Brep> newBreps, Plane CPlane)
        {
            List<Brep> finalBreps = new List<Brep>();
            List<Brep> YtrimBreps = new List<Brep>();
            List<Brep> XtrimBreps = new List<Brep>();

            //Y MIRROR TRANSFORM
            Vector3d XAxis = CPlane.XAxis;
            Transform rotateX = Transform.Rotation(Math.PI / 2, XAxis, CPlane.Origin);

            Plane YPlane = CPlane.Clone();
            YPlane.Transform(rotateX);
            if (Quadrant == 3 || Quadrant == 4)
            {
                YPlane.Flip();
            }
            Transform Ymirror = Transform.Mirror(YPlane);

            //X MIRROR TRANSFORM
            Vector3d YAxis = CPlane.YAxis;
            Transform rotateY = Transform.Rotation(Math.PI / 2, YAxis, CPlane.Origin);
            Plane XPlane = CPlane.Clone();
            XPlane.Transform(rotateY);
            if (Quadrant == 1 || Quadrant == 4)
            {
                XPlane.Flip();
            }
            Transform Xmirror = Transform.Mirror(XPlane);

            //COMPOUND MIRROR TRANSFORM
            Transform Cmirror = Ymirror * Xmirror;

            foreach (Brep aBrep in newBreps)
            {
                /*BoundingBox aBB = aBrep.GetBoundingBox(false);
                int brepQuadrant = GetQuadrant(aBB.Center);*/

                Brep[] trimmedBrep = aBrep.Trim(YPlane, 0.0001);
                if (trimmedBrep.Length != 0)
                {
                    foreach (Brep singlePiece in trimmedBrep)
                    {
                        {
                            YtrimBreps.Add(singlePiece);
                        }
                    }
                }
                else
                {
                    YtrimBreps.Add(aBrep);
                }
            }

            foreach (Brep aBrep in YtrimBreps)
            {
                BoundingBox aBB = aBrep.GetBoundingBox(true);
                int brepQuadrant = GetQuadrant(aBB.Center);

                Brep[] trimmedBrep = aBrep.Trim(XPlane, 0.0001);
                if (trimmedBrep.Length != 0)
                {
                    foreach (Brep singlePiece in trimmedBrep)
                    {
                        {
                            BoundingBox pBB = singlePiece.GetBoundingBox(true);
                            int pieceQuadrant = GetQuadrant(pBB.Center);

                            if (pieceQuadrant == Quadrant)
                            {
                                XtrimBreps.Add(singlePiece);
                            }
                        }
                    }
                }
                else if (brepQuadrant == Quadrant)
                {
                    XtrimBreps.Add(aBrep);
                }
            }

            foreach(Brep aBrep in XtrimBreps)
            {
                finalBreps.Add(aBrep);

                Brep XCopy = aBrep.DuplicateBrep();
                XCopy.Transform(Xmirror);
                finalBreps.Add(XCopy);

                Brep YCopy = aBrep.DuplicateBrep();
                YCopy.Transform(Ymirror);
                finalBreps.Add(YCopy);

                Brep CCopy = aBrep.DuplicateBrep();
                CCopy.Transform(Cmirror);
                finalBreps.Add(CCopy);

                if (joinSelected)
                {
                    finalBreps = Brep.JoinBreps(finalBreps, 0.001).ToList<Brep>();
                }
            }

            return finalBreps;
        }

        private List<Curve> MirrorNewCurve(List<Curve> newCurves, Plane CPlane)
        {
            List<Curve> finalCurves = new List<Curve>();

            //Y MIRROR TRANSFORM
            Vector3d XAxis = CPlane.XAxis;
            Transform rotateX = Transform.Rotation(Math.PI / 2, XAxis, CPlane.Origin);

            Plane YPlane = CPlane.Clone();
            YPlane.Transform(rotateX);
            if (Quadrant == 3 || Quadrant == 4)
            {
                YPlane.Flip();
            }
            Transform Ymirror = Transform.Mirror(YPlane);

            //X MIRROR TRANSFORM
            Vector3d YAxis = CPlane.YAxis;
            Transform rotateY = Transform.Rotation(Math.PI / 2, YAxis, CPlane.Origin);
            Plane XPlane = CPlane.Clone();
            XPlane.Transform(rotateY);
            if (Quadrant == 1 || Quadrant == 4)
            {
                XPlane.Flip();
            }
            Transform Xmirror = Transform.Mirror(XPlane);

            //COMPOUND MIRROR TRANSFORM
            Transform Cmirror = Ymirror * Xmirror;

            foreach (Curve curve in newCurves)
            {
                List<Curve> pieces = new List<Curve>();

                BoundingBox firstBB = curve.GetBoundingBox(true);
                firstBB.Inflate(1);
                Brep firstBrep = firstBB.ToBrep();

                int firstQuadrant = GetQuadrant(firstBB.Center);

                Brep[] firsttrimmedBrep = firstBrep.Trim(YPlane, 0.0001);
                Brep secondBrep = null;
                if (firsttrimmedBrep.Length != 0)
                {
                    Brep closedBrep = firsttrimmedBrep[0].CapPlanarHoles(0.001);
                    secondBrep = closedBrep;
                }
                else
                {
                    secondBrep = firstBrep;
                }

                if(secondBrep == null )
                {
                    continue;
                }

                int secondQuadrant = GetQuadrant(secondBrep.GetBoundingBox(true).Center);

                Brep[] secondtrimmedBrep = secondBrep.Trim(XPlane, 0.0001);
                Brep thirdBrep = null;
                if (secondtrimmedBrep.Length != 0)
                {
                    Brep closedBrep = secondtrimmedBrep[0].CapPlanarHoles(0.001);
                    secondQuadrant = GetQuadrant(closedBrep.GetBoundingBox(true).Center);

                    if(secondQuadrant == Quadrant)
                    {
                        thirdBrep = closedBrep;
                    }
                }
                else if (secondQuadrant == Quadrant)
                {
                    thirdBrep = secondBrep;
                }

                if (thirdBrep == null)
                {
                    continue;
                }
                
                //JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { thirdBrep});

                Curve[] splitCurve = curve.Split(thirdBrep, 0.001, 0.001);
                if(splitCurve.Length == 0)
                {
                    pieces.Add(curve);

                    Curve XCopy = curve.DuplicateCurve();
                    XCopy.Transform(Xmirror);
                    pieces.Add(XCopy);

                    Curve YCopy = curve.DuplicateCurve();
                    YCopy.Transform(Ymirror);
                    pieces.Add(YCopy);

                    Curve CCopy = curve.DuplicateCurve();
                    CCopy.Transform(Cmirror);
                    pieces.Add(CCopy);

                    if (joinSelected)
                    {
                        pieces = Curve.JoinCurves(pieces.ToArray()).ToList();
                    }

                    foreach (Curve piece in pieces)
                    {
                        finalCurves.Add(piece);
                    }
                }

                foreach(Curve aCurve in splitCurve)
                {
                    double t;
                    aCurve.LengthParameter(0.5, out t);
                    Point3d midPoint = aCurve.PointAt(t);
                    bool flag = thirdBrep.IsPointInside(midPoint, 0.001, true);
                    if (flag)
                    {
                        pieces.Add(aCurve);

                        Curve XCopy = aCurve.DuplicateCurve();
                        XCopy.Transform(Xmirror);
                        pieces.Add(XCopy);

                        Curve YCopy = aCurve.DuplicateCurve();
                        YCopy.Transform(Ymirror);
                        pieces.Add(YCopy);

                        Curve CCopy = aCurve.DuplicateCurve();
                        CCopy.Transform(Cmirror);
                        pieces.Add(CCopy);

                        if (joinSelected)
                        {
                            pieces = Curve.JoinCurves(pieces.ToArray()).ToList();
                        }

                        foreach(Curve piece in pieces)
                        {
                            finalCurves.Add(piece);
                        }
                    }
                }
            }

            return finalCurves;
        }

        public Rhino.Commands.Result MirrorConfiguration(RhinoDoc doc)
        {
            var gp = new Rhino.Input.Custom.GetPoint();
            OptionToggle joinToggle = new OptionToggle(JHandsPlugin.Instance.joinSelected, "Off", "On");
            OptionToggle deleteToggle = new OptionToggle(JHandsPlugin.Instance.deleteSelected, "Off", "On");

            while (true)
            {
                gp.ClearCommandOptions();
                var JToggle = gp.AddOptionToggle("JoinSelected", ref joinToggle);
                var DToggle = gp.AddOptionToggle("DeleteSelected", ref deleteToggle);

                gp.DynamicDraw += Gp_DynamicDraw;
                
                var gp_result = gp.Get();

                if (gp_result == Rhino.Input.GetResult.Point)
                {
                    Quadrant = GetQuadrant(gp.Point());

                    RhinoApp.WriteLine($"The picked point falls on the {Quadrant.ToString()} Quadrant");
                    /*
                    if (isBrep)
                    {
                        newBrep.Clear();
                        foreach (Brep aBrep in originalBrep)
                        {
                            newBrep.Add(aBrep);
                        }
                        newBrep = MirrorNewBrep(newBrep, CPlane);
                    }

                    if (isCurve)
                    {
                        newCurve.Clear();
                        foreach (Curve aCurve in originalCurve)
                        {
                            newCurve.Add(aCurve);
                        }
                        newCurve = MirrorNewCurve(newCurve, CPlane);
                    }
                    */
                    return Result.Success;
                }
                else if (gp_result == Rhino.Input.GetResult.Option) //State logic
                {
                    var option = gp.Option();
                    if (null != option)
                    {
                        if (option.Index == JToggle)
                        {
                            JHandsPlugin.Instance.joinSelected = !JHandsPlugin.Instance.joinSelected;
                            deleteToggle.CurrentValue = JHandsPlugin.Instance.joinSelected;
                            joinSelected = JHandsPlugin.Instance.joinSelected;
                        }
                        else if (option.Index == DToggle)
                        {
                            JHandsPlugin.Instance.deleteSelected = !JHandsPlugin.Instance.deleteSelected;
                            deleteToggle.CurrentValue = JHandsPlugin.Instance.deleteSelected;
                            deleteSelected = JHandsPlugin.Instance.deleteSelected;
                        }
                    }
                    continue;
                }

                if (gp_result == Rhino.Input.GetResult.Cancel)
                {
                    break;
                }
            }

            return Result.Cancel;
        }

        private void Gp_DynamicDraw(object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            int presentQuadrant = Quadrant;
            Quadrant = GetQuadrant(e.CurrentPoint);
            //RhinoApp.WriteLine($"Point fall on Quadrant {Quadrant}, CPlane is {CPlane}");

            if (Quadrant != presentQuadrant || firstPass)
            {
                RunMirror();
            }

            firstPass = false;
        }

        private void RunMirror()
        {
            if (isBrep)
            {
                newBrep.Clear();
                foreach (Brep aBrep in originalBrep)
                {
                    newBrep.Add(aBrep);
                }
                newBrep = MirrorNewBrep(newBrep, CPlane);
                JHandsPlugin.Instance.BrepDisplay.SetObjects(newBrep.ToArray());
            }

            if (isCurve)
            {
                newCurve.Clear();
                foreach (Curve aCurve in originalCurve)
                {
                    newCurve.Add(aCurve);
                }
                newCurve = MirrorNewCurve(newCurve, CPlane);
                JHandsPlugin.Instance.BrepDisplay.SetCurves(newCurve.ToArray());
            }
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
        }

        private int GetQuadrant(Point3d point)
        {
            CPlane = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ConstructionPlane();
            Point3d projectedPoint = CPlane.ClosestPoint(point);
            int quadrant = 1;

            CPlane.RemapToPlaneSpace(projectedPoint, out projectedPoint);

            if (projectedPoint[0] >= 0)
            {
                if (projectedPoint[1] >= 0)
                {
                    quadrant = 1;
                }
                else
                {
                    quadrant = 4;
                }
            }
            else
            {
                if (projectedPoint[1] >= 0)
                {
                    quadrant = 2;
                }
                else
                {
                    quadrant = 3;
                }
            }

            return quadrant;
        }

        private void CleanVariables()
        {
            deleteSelected = JHandsPlugin.Instance.deleteSelected;
            newBrep.Clear();
            originalBrep.Clear();
            originalBrepAtt.Clear();
            newCurve.Clear();
            originalCurve.Clear();
            originalCurveAtt.Clear();
            IDs.Clear();
            firstPass = true;

            JHandsPlugin.Instance.BrepDisplay.SetObjects(newBrep.ToArray());
            JHandsPlugin.Instance.BrepDisplay.SetCurves(newCurve.ToArray());
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new Rhino.Input.Custom.GetObject();

            go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve | Rhino.DocObjects.ObjectType.Brep;

            Rhino.Input.GetResult go_get = go.GetMultiple(1, 0);
            if (go_get == Rhino.Input.GetResult.Cancel)
            {
                CleanVariables();
                return Result.Cancel; 
            }
            else if (go.CommandResult() == Result.Success)
            {
                foreach (ObjRef objref in go.Objects())
                {
                    IDs.Add(objref.ObjectId);
                    GeometryBase gb = objref.Geometry();

                    if (gb.HasBrepForm)
                    {
                        //RhinoApp.WriteLine("this is a brep!");
                        isBrep = true;
                        newBrep.Add(objref.Brep());
                        originalBrep.Add(objref.Brep());
                        originalBrepAtt.Add(objref.Object().Attributes);
                    }
                    else
                    {
                        //RhinoApp.WriteLine("this is a curve!");
                        isCurve = true;
                        newCurve.Add(objref.Curve());
                        originalCurve.Add(objref.Curve());
                        originalCurveAtt.Add(objref.Object().Attributes);
                    }
                }
            }

            foreach (Guid ID in IDs)
            {
                doc.Objects.Hide(ID, true);
            }

            JHandsPlugin.Instance.BrepDisplay.Enabled = true;
            JHandsPlugin.Instance.PreviewMirror = true;

            var gpr = MirrorConfiguration(doc);

            JHandsPlugin.Instance.BrepDisplay.Enabled = false;
            JHandsPlugin.Instance.PreviewMirror = false;

            if (gpr != Result.Success)
            {
                foreach (Guid ID in IDs)
                {
                    doc.Objects.Show(ID, true);
                }

                CleanVariables();
                return Result.Cancel;
            }

            if (gpr != 0)
            {
                foreach (Guid ID in IDs)
                {
                    doc.Objects.Show(ID, true);
                }
                CleanVariables();
                return Result.Cancel;
            }
            else if (deleteSelected)
            {
                foreach (Guid ID in IDs)
                {
                    doc.Objects.Delete(doc.Objects.FindId(ID), true, true);
                }
            }
            else
            {
                foreach (Guid ID in IDs)
                {
                    doc.Objects.Show(ID, true);
                }
            }

            if (isBrep)
            {
                for (int i = 0; i < originalBrep.Count; i++)
                {
                    Brep oneBrep = originalBrep[i];
                    ObjectAttributes att = originalBrepAtt[i];
                    att.Visible = true;
                    List<Brep> finalBreps = MirrorNewBrep(new List<Brep> { oneBrep }, CPlane);
                    foreach (Brep aBrep in finalBreps)
                    {
                        doc.Objects.AddBrep(aBrep, att);
                    }
                }
            }

            if (isCurve)
            {
                for (int i = 0; i < originalCurve.Count; i++)
                {
                    Curve oneCurve = originalCurve[i];
                    ObjectAttributes att = originalCurveAtt[i];
                    att.Visible = true;
                    List<Curve> finalCurves = MirrorNewCurve(new List<Curve> { oneCurve }, CPlane);
                    foreach(Curve aCurve in finalCurves)
                    {
                        doc.Objects.AddCurve(aCurve, att);
                    }
                }
            }

            doc.Views.Redraw();

            CleanVariables();

            return Result.Success;
        }

    }
}