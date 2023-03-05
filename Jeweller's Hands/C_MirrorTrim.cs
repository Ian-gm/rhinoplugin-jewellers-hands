using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.PlugIns;
using Rhino.UI;

namespace JewellersHands
{
    public class C_MirrorTrim : Rhino.Commands.Command
    {
        public C_MirrorTrim()
        {
            //Instance = this;
        }

        bool isBrep = false;
        bool isCurve = false;
        bool deleteSelected = JHandsPlugin.Instance.deleteSelected;
        List<Brep> newBrep = new List<Brep>();
        List<Brep> originalBrep = new List<Brep>();
        List<Curve> newCurve = new List<Curve>();
        List<Curve> originalCurve = new List<Curve>();
        List<Guid>  IDs = new List<Guid>();
        Plane CPlane;

        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_MirrorTrim Instance { get; private set; }

        public override string EnglishName => "JH_MirrorTrim";

        private List<Brep> MirrorNewBrep(List<Brep> newBreps, Plane CPlane)
        {
            List<Brep> finalBreps = new List<Brep>();

            if (JHandsPlugin.Instance.Xmirror != 0)
            {
                RhinoApp.WriteLine("Mirror in X");
                Vector3d XAxis = CPlane.XAxis;
                Transform rotateX = Transform.Rotation(Math.PI/2, XAxis, CPlane.Origin);
                Plane plane = CPlane.Clone();
                plane.Transform(rotateX);
                if (JHandsPlugin.Instance.Xmirror == 2)
                    plane.Flip();
                Transform mirror = Transform.Mirror(plane);
                foreach (Brep aBrep in newBreps)
                {
                    Brep[] trimmedBrep = aBrep.Trim(plane, 0.0001);
                    if (trimmedBrep.Length != 0)
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
                    else
                    {
                        Brep originalPiece = aBrep.DuplicateBrep();
                        aBrep.Transform(mirror);

                        finalBreps.Add(originalPiece);
                        finalBreps.Add(aBrep);
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
                RhinoApp.WriteLine("Mirror in Y");
                Vector3d YAxis = CPlane.YAxis;
                Transform rotateY = Transform.Rotation(Math.PI/2, YAxis, CPlane.Origin);
                Plane plane = CPlane.Clone();
                plane.Transform(rotateY);
                if (JHandsPlugin.Instance.Ymirror == 2)
                    plane.Flip();
                Transform mirror = Transform.Mirror(plane);
                foreach (Brep aBrep in newBreps)
                {
                    Brep[] trimmedBrep = aBrep.Trim(plane, 0.0001);
                    if (trimmedBrep.Length != 0)
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
                    else
                    {
                        Brep originalPiece = aBrep.DuplicateBrep();
                        aBrep.Transform(mirror);

                        finalBreps.Add(originalPiece);
                        finalBreps.Add(aBrep);
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

        private List<Curve> MirrorNewCurve(List<Curve> newCurves, Plane CPlane)
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
                Vector3d XAxis = CPlane.XAxis;
                Transform rotateX = Transform.Rotation(Math.PI / 2, XAxis, CPlane.Origin);
                Plane plane = CPlane.Clone();
                plane.Transform(rotateX);
                if (JHandsPlugin.Instance.Xmirror == 2)
                    plane.Flip();
                mirrorX = Transform.Mirror(plane);
                Brep[] trimmedbb = bbBrep.Trim(plane, 0.001);
                if(trimmedbb.Length != 0)
                {
                Brep singlebb = trimmedbb[0];
                bbBrep = singlebb.CapPlanarHoles(0.1);
            }
            }

            if (JHandsPlugin.Instance.Ymirror != 0)
            {
                RhinoApp.WriteLine("Mirror in X");
                Vector3d YAxis = CPlane.YAxis;
                Transform rotateY = Transform.Rotation(Math.PI / 2, YAxis, CPlane.Origin);
                Plane plane = CPlane.Clone();
                plane.Transform(rotateY);
                if (JHandsPlugin.Instance.Ymirror == 2)
                    plane.Flip();
                mirrorY = Transform.Mirror(plane);
                Brep[] trimmedbb = bbBrep.Trim(plane, 0.001);
                if (trimmedbb.Length != 0)
                {
                Brep singlebb = trimmedbb[0];
                bbBrep = singlebb.CapPlanarHoles(0.1);
            }
            }

            JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] {bbBrep});
            //RhinoDoc.ActiveDoc.Objects.AddBrep(bbBrep);

            foreach (Curve newCurve in newCurves)
            {
                Curve[] trimmedCurve = newCurve.Split(bbBrep, 0.001, 0.1);
                if (trimmedCurve.Length != 0)
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
                else
                {
                    Curve originalCurve = newCurve.DuplicateCurve();
                    pieces.Add(originalCurve.DuplicateCurve());

                    if (!mirrorX.IsZero)
                    {
                        newCurve.Transform(mirrorX);
                        pieces.Add(newCurve.DuplicateCurve());

                        if (!mirrorY.IsZero)
                        {
                            newCurve.Transform(mirrorY);
                            originalCurve.Transform(mirrorY);
                            pieces.Add(newCurve);
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
                        newCurve.Transform(mirrorY);
                        pieces.Add(newCurve);
                        RhinoApp.Write("Mirror in Y");
                    }
                }
            }

            pieces = Curve.JoinCurves(pieces).ToList();

            return pieces;
        }

        private Plane UpdateCPlane(RhinoDoc doc, int value)
        {
            Plane CPlane = Plane.WorldXY;
            RhinoViewport activeViewport = doc.Views.ActiveView.ActiveViewport;
            string activeName = activeViewport.Name;

            if (value == 0) //Copy active viewports CPlane
            {
                CPlane = activeViewport.ConstructionPlane();
            }
            else if (value == 1) //Top CPlane
            {
                CPlane = Plane.WorldXY;
            }
            else if (value == 2) //Front CPlane
            {
                CPlane = Plane.WorldZX;
                CPlane.Flip();
            }
            else if(value == 3) //Right CPlane
            {
                CPlane = Plane.WorldYZ;
            }

            return CPlane;
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

            Rhino.Input.GetResult go_get = go.GetMultiple(1, 0);
            if (go_get == Rhino.Input.GetResult.Cancel)
            { return Result.Cancel; }
            else if(go.CommandResult() == Result.Success)
            {
                foreach(ObjRef objref in go.Objects())
                {
                    IDs.Add(objref.ObjectId);
                    GeometryBase gb = objref.Geometry();

                    if (gb.HasBrepForm)
                    { 
                    RhinoApp.WriteLine("this is a brep!");
                    isBrep = true;
                        newBrep.Add(objref.Brep());
                        originalBrep.Add(objref.Brep());
                }
                else
                {
                    RhinoApp.WriteLine("this is a curve!");
                    isCurve = true;
                        newCurve.Add(objref.Curve());
                        originalCurve.Add(objref.Curve());
                    }
                }
            }

            foreach(Guid ID in IDs)
            {
            doc.Objects.Hide(ID, true);
            }

            JHandsPlugin.Instance.BrepDisplay.Enabled = true;
            JHandsPlugin.Instance.PreviewMirror = true;

            CPlane = UpdateCPlane(doc, 0);

            if (isBrep)
            {
                newBrep = MirrorNewBrep(newBrep, CPlane);
                JHandsPlugin.Instance.BrepDisplay.SetObjects(newBrep.ToArray());
            }
            
            if (isCurve)
            { 
                newCurve = MirrorNewCurve(newCurve, CPlane);
                JHandsPlugin.Instance.BrepDisplay.SetCurves(newCurve.ToArray());
            }

            Rhino.Input.GetResult configuration = MirrorConfiguration(doc);

            JHandsPlugin.Instance.BrepDisplay.Enabled = false;
            JHandsPlugin.Instance.PreviewMirror = false;

            if (configuration != 0)
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
                foreach (Brep aBrep in newBrep)
                {
                    doc.Objects.AddBrep(aBrep);
                }
            }
            
            if(isCurve) 
            {
                foreach(Curve aCurve in newCurve)
                {
                    doc.Objects.AddCurve(aCurve);
                }
            }
            doc.Views.Redraw();

            CleanVariables();
            return Result.Success;
        }

        public Rhino.Input.GetResult MirrorConfiguration(RhinoDoc doc)
        {
            var gopt = new GetOption();

            var Xoption = new[] { "Off", "Top", "Bottom" };
            string[] Yoption = { "Off", "Left", "Right" };
            var Poption = new[] { "Active", "Top", "Front", "Right" };
            Plane CPlane = UpdateCPlane(doc, 0);

            var X_value = Settings.GetInteger("Xdirection", JHandsPlugin.Instance.Xmirror);
            var Y_value = Settings.GetInteger("Ydirection", JHandsPlugin.Instance.Ymirror);
            var P_value = Settings.GetInteger("MirrorPlane", JHandsPlugin.Instance.MirrorPlane);
            OptionToggle deleteToggle = new OptionToggle(JHandsPlugin.Instance.deleteSelected, "Off", "On");

            gopt.SetCommandPrompt("Choose Mirror options");
            gopt.AcceptNothing(true);

            while (true)
            {
                gopt.ClearCommandOptions();

                var Xdirection = gopt.AddOptionList("XAxis", Xoption, JHandsPlugin.Instance.Xmirror);
                var Ydirection = gopt.AddOptionList("YAxis", Yoption, JHandsPlugin.Instance.Ymirror);
                var MPlane = gopt.AddOptionList("MirrorPlane", Poption, JHandsPlugin.Instance.MirrorPlane);
                var DToggle = gopt.AddOptionToggle("DeleteSelected", ref deleteToggle);

                Rhino.Input.GetResult get_rc = gopt.Get(); 
                
                if (get_rc != Rhino.Input.GetResult.Cancel)
                {
                    RhinoView.SetActive += OnViewSetActive;
                }

                if (get_rc == Rhino.Input.GetResult.Nothing)
                {
                    RhinoApp.WriteLine("Pressed Enter Command");
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
                        else if (option.Index == MPlane)
                        {
                            P_value = option.CurrentListOptionIndex;
                            JHandsPlugin.Instance.MirrorPlane = option.CurrentListOptionIndex;
                            RhinoApp.WriteLine(JHandsPlugin.Instance.MirrorPlane.ToString());
                            CPlane = UpdateCPlane(doc, option.CurrentListOptionIndex);
                        }
                        else if(option.Index == DToggle)
                        {
                            RhinoApp.WriteLine("togle presed");
                            JHandsPlugin.Instance.deleteSelected = !JHandsPlugin.Instance.deleteSelected;
                            deleteToggle.CurrentValue = JHandsPlugin.Instance.deleteSelected;
                            deleteSelected = JHandsPlugin.Instance.deleteSelected;
                        }
                    }

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
                    continue;
                }
                else if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    return Rhino.Input.GetResult.Cancel;
                }
                }

            return Rhino.Input.GetResult.NoResult;
            }

        private void OnViewSetActive(object sender, ViewEventArgs args)
        {
            RhinoApp.WriteLine("Active viewport changed. MirrorPlane variable is: " + JHandsPlugin.Instance.MirrorPlane.ToString());

            CPlane = UpdateCPlane(RhinoDoc.ActiveDoc, JHandsPlugin.Instance.MirrorPlane);
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
            else if (isCurve)
            {
                newCurve.Clear();
                foreach (Curve aCurve in originalCurve)
                {
                    newCurve.Add(aCurve);
                }
                newCurve = MirrorNewCurve(newCurve, CPlane);
                JHandsPlugin.Instance.BrepDisplay.SetCurves(newCurve.ToArray());
                }
            }

        private void CleanVariables()
        {
            deleteSelected = JHandsPlugin.Instance.deleteSelected;
            newBrep = new List<Brep>();
            originalBrep = new List<Brep>();
            newCurve = new List<Curve>();
            originalCurve = new List<Curve>();
            IDs = new List<Guid>();
        }
    }
}