using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Render;
using Rhino.Render.ChangeQueue;
using Rhino.UI;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Windows.Markup;

namespace JewellersHands
{
    public class C_Weight : Rhino.Commands.Command
    {
        public C_Weight()
        {
            Instance = this;
        }
        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_Weight Instance { get; private set; }

        public override string EnglishName => "JH_Weight";

        private double GemstonesExit(string prompt)
        {
            //Choose a value
            Rhino.Input.Custom.GetOption gp = new Rhino.Input.Custom.GetOption();
            Rhino.Input.Custom.OptionToggle RevertChanges = new Rhino.Input.Custom.OptionToggle(JHandsPlugin.Instance.RevertChanges, "Off", "On");
            Rhino.Input.Custom.OptionInteger TextSize = new Rhino.Input.Custom.OptionInteger(JHandsPlugin.Instance.BrepDisplay.caseTextSize, true, 1);
            Rhino.Input.Custom.OptionToggle TakeCapture = new Rhino.Input.Custom.OptionToggle(JHandsPlugin.Instance.TakeViewCapture, "Off", "On");
            gp.SetCommandPrompt(prompt);
            gp.AddOptionToggle("RevertChanges", ref RevertChanges);
            gp.AddOption("Capture");
            gp.AddOptionInteger("TextSize", ref TextSize);
            //gp.AddOptionToggle("ViewCapture", ref TakeCapture);
            gp.AcceptNothing(true);

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
                    if (gp.OptionIndex() == 1) //REVERT CHANGES
                    {
                        if (JHandsPlugin.Instance.RevertChanges)
                        {
                            JHandsPlugin.Instance.RevertChanges = false;
                            RevertChanges.CurrentValue = false;
                        }
                        else
                        {
                            JHandsPlugin.Instance.RevertChanges = true;
                            RevertChanges.CurrentValue = true;
                        }
                    }
                    else if (gp.OptionIndex() == 2) //CAPTURE
                    {
                        RhinoView view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;
                        string path = Rhino.RhinoDoc.ActiveDoc.Path;
                        newViewCapture(view, path);
                    }
                    else if (gp.OptionIndex() == 3)//CHANGE TEXT SIZE
                    {
                        JHandsPlugin.Instance.BrepDisplay.SetTextSize(TextSize.CurrentValue);
                    }
                    else if (gp.OptionIndex() == 4)
                    {
                        if (JHandsPlugin.Instance.TakeViewCapture)
                        {
                            JHandsPlugin.Instance.TakeViewCapture = false;
                            TakeCapture.CurrentValue = false;
                        }
                        else
                        {
                            JHandsPlugin.Instance.TakeViewCapture = true;
                            TakeCapture.CurrentValue = true;
                        }
                    }
                }
                else if (gp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    return 1;
                }
            }
            return -1;
        }

        private void newViewCapture(RhinoView view, string path)
        {
            var view_capture = new ViewCapture
            {
                Width = view.ActiveViewport.Size.Width,
                Height = view.ActiveViewport.Size.Height,
                ScaleScreenItems = false,
                DrawAxes = false,
                DrawGrid = false,
                DrawGridAxes = false,
                TransparentBackground = false
            };

            string name = Path.GetFileNameWithoutExtension(path);
            string viewName = view.ActiveViewport.Name;
            var bitmap = view_capture.CaptureToBitmap(view);

            string docpath = Directory.GetParent(path).FullName;
            string[] getFiles = Directory.GetFiles(docpath);

            int largestPrefix = 0;

            foreach (string file in getFiles)
            {
                int filePrefix = 0;
                string checkName = name + "_Weight";
                string fileName = Path.GetFileNameWithoutExtension(file);
                bool flag = fileName.Contains(checkName);

                if (flag)
                {
                    filePrefix = 1;
                    if (filePrefix > largestPrefix)
                    {
                        largestPrefix = filePrefix;
                    }

                    string[] fileNamePieces = fileName.Split('-');
                    if (fileNamePieces.Length > 1)
                    {
                        string possiblePrefix = fileNamePieces[fileNamePieces.Length - 1];

                        flag = int.TryParse(possiblePrefix, out filePrefix);
                    }
                    if (filePrefix >= largestPrefix)
                    {
                        largestPrefix = filePrefix + 1;
                    }
                }
            }
                string prefix = "";

            if (largestPrefix > 0)
            {
                prefix = "-" + largestPrefix.ToString();
            }


            if (null != bitmap)
            {
                //string filename = name + "_" + viewName.Substring(0,1) + "V" + ".jpeg";
                string filename = name + "_Weight" + prefix + ".jpeg";
                string finalPath = Path.Combine(docpath, filename);
                bitmap.Save(finalPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        public List<Guid> GetBrepsTotalVolume(Rhino.RhinoDoc doc)
        {
            List<Guid> ObjGuid = new List<Guid>();

            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            double totalVolume = 0;
            string[] valuesType = new string[] { "Platinum", "24K", "22K", "20K", "18K", "14K", "10K", "Silver", "Palladium" };
            double[] valuesCoeficients = new double[] { 0.0207, 0.01932, 0.0178, 0.01642, 0.0152, 0.0134, 0.0119, 0.01036, 0.01202 };

            go.SetCommandPrompt("Select objects to group");
            go.AcceptUndo(true);
            go.EnablePreSelect(true, true);
            go.EnableSelPrevious(true);
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Brep | Rhino.DocObjects.ObjectType.Mesh;
            OptionToggle onlySolids = new OptionToggle(JHandsPlugin.Instance.onlySolids, "Off", "On");

            while (true)
            {
                //go.ClearCommandOptions();
                var DToggle = go.AddOptionToggle("OnlySolids", ref onlySolids);
                var go_result = go.GetMultiple(1, 0);
                var option = go.Option();
                if (null != option)
                {
                    if (option.Index == DToggle)
                    {
                        JHandsPlugin.Instance.onlySolids = !JHandsPlugin.Instance.onlySolids;
                        onlySolids.CurrentValue = JHandsPlugin.Instance.onlySolids;
                    }
                }
                else if (go.CommandResult() == Rhino.Commands.Result.Failure)
                {
                    return null;
                }
                else if (go.CommandResult() == Rhino.Commands.Result.Success)
                {
                    break;
                }
            }

            foreach (var objref in go.Objects())
            {
                ObjectType currentObj = objref.Geometry().ObjectType;
                if (currentObj == ObjectType.Brep)
                {
                    var brep = objref.Brep();
                    if (JHandsPlugin.Instance.onlySolids)
                    {
                        if (brep.IsSolid)
                        {
                            totalVolume += brep.GetVolume();
                            ObjGuid.Add(objref.ObjectId);
                        }
                        else
                        {
                            TextDot dot = new TextDot("not a solid", brep.GetBoundingBox(true).Center);
                            RhinoDoc.ActiveDoc.Objects.AddTextDot(dot);
                        }
                    }
                    else
                    {
                        totalVolume += brep.GetVolume();
                        ObjGuid.Add(objref.ObjectId);
                    }
                }
                else if (currentObj == ObjectType.Mesh)
                {
                    var mesh = objref.Mesh();
                    if (JHandsPlugin.Instance.onlySolids)
                    {
                        if (mesh.IsSolid)
                        {
                            totalVolume += mesh.Volume();
                            ObjGuid.Add(objref.ObjectId);
                        }
                        else
                        {
                            TextDot dot = new TextDot("not a solid", mesh.GetBoundingBox(true).Center);
                            RhinoDoc.ActiveDoc.Objects.AddTextDot(dot);
                        }
                    }
                    else
                    {
                        totalVolume += mesh.Volume();
                        ObjGuid.Add(objref.ObjectId);
                    }
                }
            }

            doc.Views.Redraw();

            /*
            calculation = "Volume     " + "\t" + Math.Round(totalVolume, 2).ToString() + " cubic " + doc.ModelUnitSystem.ToString().ToLower();
            calculation += "\n";
            */

            List<string> weightValues = new List<string>();

            for (int i = 0; i < valuesType.Length; i++)
            {
                double result = Math.Round(valuesCoeficients[i] * totalVolume, 2);
                string calculation = valuesType[i] + " " + result.ToString() + "gr";
                weightValues.Add(calculation);
            }

            JHandsPlugin.Instance.BrepDisplay.SetText(weightValues.ToArray());
            JHandsPlugin.Instance.BrepDisplay.SetColors(new System.Drawing.Color[] {System.Drawing.Color.Black});

            //Dialogs.ShowTextDialog(calculation, "Weight by volume");

            if (totalVolume >= 0)
            {
                return ObjGuid;
            }

            return null;
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            List<Guid> ObjGuid = GetBrepsTotalVolume(doc);

            if(ObjGuid == null)
            {
                return Result.Failure;
            }

            doc.Objects.UnselectAll();

            //DEFINÍ EL DISPLAY MODE RENDER
            DisplayModeDescription displayMode = Rhino.Display.DisplayModeDescription.FindByName("Rendered");
            displayMode.DisplayAttributes.FillMode = DisplayPipelineAttributes.FrameBufferFillMode.SolidColor;
            //displayMode.DisplayAttributes.SetFill(System.Drawing.Color.FromArgb(230, 230, 230));
            displayMode.DisplayAttributes.SetFill(System.Drawing.Color.White);
            DisplayModeDescription.UpdateDisplayMode(displayMode);

            List<DisplayModeDescription> originalDisplayModes = new List<DisplayModeDescription>();

            ViewTable viewTable = doc.Views;
            foreach (RhinoView view in viewTable)
            {
                originalDisplayModes.Add(view.ActiveViewport.DisplayMode);
                view.ActiveViewport.DisplayMode = displayMode;
                view.ActiveViewport.ConstructionAxesVisible = false;
                view.ActiveViewport.ConstructionGridVisible = false;
                //view.DisplayPipeline.Draw2dText("this", System.Drawing.Color.White, new Point2d(50, 50), true, 25);
            }
            JHandsPlugin.Instance.BrepDisplay.Enabled = true;

            //HIDE EVERYTHING
            List<Rhino.DocObjects.RhinoObject> allHidden = new List<Rhino.DocObjects.RhinoObject>();
            ObjectTable objectTable = doc.Objects;
            foreach (RhinoObject obj in objectTable)
            {
                bool add = true;
                Guid objGuid = obj.Id;
               
                foreach(Guid id in ObjGuid) //loopear todas las GUID de los objetos seleccionados
                {
                    if (id == objGuid)
                    {
                        add = false;
                    }

                }

                if (add)
                {
                    allHidden.Add(obj);
                }
            }

            foreach (RhinoObject obj in allHidden)
            {
                doc.Objects.Hide(obj, false);
            }

            ObjectAttributes att = new ObjectAttributes();
            var mat = new Rhino.DocObjects.Material
            {
                Name = "Grey",
                DiffuseColor = System.Drawing.Color.Gray,
                SpecularColor = System.Drawing.Color.White
            };
            var matobj = RenderMaterial.CreateBasicMaterial(mat, doc);
            doc.RenderMaterials.Add(matobj);
            att.RenderMaterial = matobj;

            List<ObjectAttributes> oldAtt = new List<ObjectAttributes>();

            foreach (Guid id in ObjGuid)
            {
                var obj = doc.Objects.FindId(id);
                ObjectAttributes olda = obj.Attributes.Duplicate();
                oldAtt.Add(olda);
                doc.Objects.ModifyAttributes(id, att, true);
                obj.CommitChanges();
            }

            doc.Views.Redraw();

            GemstonesExit("Weight Calculation");

            if (JHandsPlugin.Instance.RevertChanges)
            {
                //VIEWPORT REVERT
                displayMode.DisplayAttributes.FillMode = DisplayPipelineAttributes.FrameBufferFillMode.DefaultColor;
                DisplayModeDescription.UpdateDisplayMode(displayMode);
                int viewCount = 0;

                foreach (RhinoView view in viewTable)
                {
                    view.ActiveViewport.DisplayMode = originalDisplayModes[viewCount];
                    view.ActiveViewport.ConstructionAxesVisible = true;
                    view.ActiveViewport.ConstructionGridVisible = true;
                    viewCount++;
                }

                //HIDE REVERT
                foreach (RhinoObject obj in allHidden)
                {
                    doc.Objects.Show(obj, false);
                }

                //REVERT ATTRIBUTES
                int attIndex = 0;
                foreach (Guid id in ObjGuid)
                {
                    var obj = doc.Objects.FindId(id);
                    doc.Objects.ModifyAttributes(obj, oldAtt[attIndex], true);
                    obj.CommitChanges();
                    
                    attIndex++;
                }
            }

            //BORRAR LISTA DE CASOS Y COLORES
            string[] emptytext = { "" };
            JHandsPlugin.Instance.BrepDisplay.SetText(emptytext);
            System.Drawing.Color[] emptycolors = { System.Drawing.Color.Black };
            JHandsPlugin.Instance.BrepDisplay.SetColors(emptycolors);
            JHandsPlugin.Instance.BrepDisplay.Enabled = false;

            return Result.Success;
        }
    }
}