using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.PlugIns;

namespace JewellersHands
{
    public class C_MirrorTrim : Rhino.Commands.Command
    {
        public C_MirrorTrim()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_MirrorTrim Instance { get; private set; }

        public override string EnglishName => "JH_MirrorTrim";

        private List<Brep> MirrorNewBrep(List<Brep> newBreps)
        {
            List<Brep> finalBreps = new List<Brep>();

            if (JHandsPlugin.Instance.Xmirror != 0)
            {
                RhinoApp.WriteLine("Mirror in X");
                Plane plane = Plane.WorldZX;
                if (JHandsPlugin.Instance.Xmirror == 2)
                    plane.Flip();
                Transform mirror = Transform.Mirror(plane);
                foreach (Brep newBrep in newBreps)
                {
                    Brep[] trimmedBrep = newBrep.Trim(plane, 0.0001);
                    if (trimmedBrep != null)
                    {
                        foreach (Brep singlePiece in trimmedBrep)
                        {
                            Brep originalPiece = singlePiece.DuplicateBrep();
                            singlePiece.Transform(mirror);
                            bool flag = singlePiece.Join(originalPiece, 0.1, true);
                            if (flag)
                            {
                                finalBreps.Add(singlePiece);
                            }
                            else
                            {
                                finalBreps.Add(originalPiece);
                                originalPiece.Transform(mirror);
                                finalBreps.Add(originalPiece);
                            }
                        }
                    }
                }
                newBreps.Clear();
                foreach (Brep brep in finalBreps)
                {
                    newBreps.Add(brep);
                }
                finalBreps.Clear();
            }

            if (JHandsPlugin.Instance.Ymirror != 0)
            {
                RhinoApp.WriteLine("Mirror in X");
                Plane plane = Plane.WorldYZ;
                if (JHandsPlugin.Instance.Ymirror == 2)
                    plane.Flip();
                Transform mirror = Transform.Mirror(plane);
                foreach (Brep newBrep in newBreps)
                {
                    Brep[] trimmedBrep = newBrep.Trim(plane, 0.0001);
                    if (trimmedBrep != null)
                    {
                        foreach (Brep singlePiece in trimmedBrep)
                        {
                            Brep originalPiece = singlePiece.DuplicateBrep();
                            singlePiece.Transform(mirror);
                            bool flag = singlePiece.Join(originalPiece, 0.1, true);
                            if (flag)
                            {
                                finalBreps.Add(singlePiece);
                            }
                            else
                            {
                                finalBreps.Add(originalPiece);
                                originalPiece.Transform(mirror);
                                finalBreps.Add(originalPiece);
                            }
                        }
                    }
                }
                newBreps.Clear();
                foreach (Brep brep in finalBreps)
                {
                    newBreps.Add(brep);
                }
                finalBreps.Clear();
            }
            return newBreps;
        }

        private List<Curve> MirrorNewCurve(List<Curve> newCurves)
        {
            BoundingBox boundingBox = new BoundingBox();
            Transform mirrorX = new Transform(0);
            Transform mirrorY = new Transform(0);
            List<Curve> pieces = new List<Curve>();

            foreach (Curve curve in newCurves)
            {
                boundingBox = curve.GetBoundingBox(true);
            }
            boundingBox.Max = new Point3d(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z + 1);
            boundingBox.Min = new Point3d(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z - 1);
            boundingBox.Inflate(1);

            Brep bbBrep = boundingBox.ToBrep();

            if (JHandsPlugin.Instance.Xmirror != 0)
            {
                RhinoApp.WriteLine("Mirror in X");
                Plane plane = Plane.WorldZX;
                if (JHandsPlugin.Instance.Xmirror == 2)
                    plane.Flip();
                mirrorX = Transform.Mirror(plane);
                Brep[] trimmedbb = bbBrep.Trim(plane, 0.001);
                Brep singlebb = trimmedbb[0];
                bbBrep = singlebb.CapPlanarHoles(0.1);
            }

            if (JHandsPlugin.Instance.Ymirror != 0)
            {
                RhinoApp.WriteLine("Mirror in X");
                Plane plane = Plane.WorldYZ;
                if (JHandsPlugin.Instance.Ymirror == 2)
                    plane.Flip();
                mirrorY = Transform.Mirror(plane);
                Brep[] trimmedbb = bbBrep.Trim(plane, 0.001);
                Brep singlebb = trimmedbb[0];
                bbBrep = singlebb.CapPlanarHoles(0.1);
            }

            JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] {bbBrep});
            //RhinoDoc.ActiveDoc.Objects.AddBrep(bbBrep);

            foreach (Curve newCurve in newCurves)
            {
                Curve[] trimmedCurve = newCurve.Split(bbBrep, 0.001, 0.1);
                if (trimmedCurve != null)
                {
                    foreach (Curve singlePiece in trimmedCurve)
                    {
                        singlePiece.Domain = new Interval(0, 1);
                        if (bbBrep.IsPointInside(singlePiece.PointAt(0.5),0.001,true))
                        {
                            Curve originalCurve = singlePiece.DuplicateCurve();

                            pieces.Add(originalCurve.DuplicateCurve());

                            if (!mirrorX.IsZero)
                            {
                                singlePiece.Transform(mirrorX);
                                pieces.Add(singlePiece.DuplicateCurve());

                                if (!mirrorY.IsZero)
                                {
                                    singlePiece.Transform(mirrorY);
                                    originalCurve.Transform(mirrorY);
                                    pieces.Add(singlePiece);
                                    pieces.Add(originalCurve);
                                    RhinoApp.Write("Mirror in X and Y");
                                }
                                else
                                {
                                    RhinoApp.Write("Mirror in X");
                                }
                            }
                            else if (!mirrorY.IsZero)
                            {
                                singlePiece.Transform(mirrorY);
                                pieces.Add(singlePiece);
                                RhinoApp.Write("Mirror in Y");
                            }
                        }
                    }
                }
            }
            return pieces;
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new Rhino.Input.Custom.GetObject();
            bool isBrep = false;
            bool isCurve = false;
            List<Brep> newBrep = new List<Brep>();
            List<Brep> originalBrep = new List<Brep>();
            List<Curve> newCurve = new List<Curve>();
            List<Curve> originalCurve = new List<Curve>();
            Guid ID = Guid.Empty;

            go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve | Rhino.DocObjects.ObjectType.Brep;

            Rhino.Input.GetResult go_get = go.Get();
            if(go_get == Rhino.Input.GetResult.Cancel)
            { return Result.Cancel; }
            else if(go.CommandResult() == Result.Success)
            {
                ID = go.Object(0).ObjectId;
                GeometryBase gb = go.Object(0).Geometry();
                if(gb.HasBrepForm)
                {
                    RhinoApp.WriteLine("this is a brep!");
                    isBrep = true;
                    newBrep.Add(go.Object(0).Brep());
                    originalBrep.Add(go.Object(0).Brep());
                }
                else
                {
                    RhinoApp.WriteLine("this is a curve!");
                    isCurve = true;
                    newCurve.Add(go.Object(0).Curve());
                    originalCurve.Add(go.Object(0).Curve());
                }
            }

            doc.Objects.Hide(ID, true);
           
            var gopt = new GetOption();

            var Xoption = new[] { "No", "Bottom", "Top" };
            string[] Yoption = { "No", "Left", "Right" };
            
            var X_value = Settings.GetInteger("Xdirection", JHandsPlugin.Instance.Xmirror);
            var Y_value = Settings.GetInteger("Ydirection", JHandsPlugin.Instance.Ymirror);
            gopt.SetCommandPrompt("Choose Mirror options");

            JHandsPlugin.Instance.BrepDisplay.Enabled = true;
            JHandsPlugin.Instance.PreviewMirror = true;

            if (isBrep)
            {
                newBrep = MirrorNewBrep(newBrep);
                JHandsPlugin.Instance.BrepDisplay.SetObjects(newBrep.ToArray());
            }
            else if(isCurve) 
            { 
                newCurve = MirrorNewCurve(newCurve);
                JHandsPlugin.Instance.BrepDisplay.SetCurves(newCurve.ToArray());
            }

            while (true)
            {
                gopt.ClearCommandOptions();

                var Xdirection = gopt.AddOptionList("MirrorX", Xoption, JHandsPlugin.Instance.Xmirror);
                var Ydirection = gopt.AddOptionList("MirrorY", Yoption, JHandsPlugin.Instance.Ymirror);

                Rhino.Input.GetResult get_rc = gopt.Get(); 
                
                if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                else if (get_rc == Rhino.Input.GetResult.Option) //State logic
                {
                    var option = gopt.Option();
                    if (null != option)
                    {
                        if (option.Index == Xdirection)
                        {
                            X_value = option.CurrentListOptionIndex;
                            JHandsPlugin.Instance.Xmirror = option.CurrentListOptionIndex;
                            RhinoApp.WriteLine(JHandsPlugin.Instance.Xmirror.ToString());
                        }
                        else if (option.Index == Ydirection)
                        {
                            Y_value = option.CurrentListOptionIndex;
                            JHandsPlugin.Instance.Ymirror = option.CurrentListOptionIndex;
                            RhinoApp.WriteLine(JHandsPlugin.Instance.Ymirror.ToString());
                        }
                    }

                    if (isBrep)
                    {
                        newBrep.Clear();
                        foreach (Brep aBrep in originalBrep)
                        {
                            newBrep.Add(aBrep);
                        }
                        newBrep = MirrorNewBrep(newBrep);
                        JHandsPlugin.Instance.BrepDisplay.SetObjects(newBrep.ToArray());
                    }
                    else if (isCurve)
                    {
                        newCurve.Clear();
                        foreach(Curve aCurve in originalCurve)
                        {
                            newCurve.Add(aCurve);
                        }
                        newCurve= MirrorNewCurve(newCurve);
                        JHandsPlugin.Instance.BrepDisplay.SetCurves(newCurve.ToArray());
                    }
                    continue;
                }
                else if(go.CommandResult() == Result.Success)
                {
                    break;
                }
            }
            JHandsPlugin.Instance.BrepDisplay.Enabled = false;
            JHandsPlugin.Instance.PreviewMirror = false;

            if (isBrep)
            {
                foreach (Brep aBrep in newBrep)
                {
                    doc.Objects.AddBrep(aBrep);
                }
            }
            else if(isCurve) 
            {
                foreach(Curve aCurve in newCurve)
                {
                    doc.Objects.AddCurve(aCurve);
                }
            }

            doc.Objects.Show(ID, true);
            doc.Views.Redraw();


            return Result.Success;
        }
    }
}