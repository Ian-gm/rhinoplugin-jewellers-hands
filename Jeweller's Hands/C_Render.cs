using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Render;
using Rhino.Render.ChangeQueue;
using Rhino.UI;

namespace JewellersHands
{
    public class C_Render : Rhino.Commands.Command
    {
        public C_Render()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_Render Instance { get; private set; }

        public override string EnglishName => "JH_Render";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //READ ALL GEMS
            List<Guid> allGemsId = new List<Guid>();

            Layer GemLayerRoot = Rhino.RhinoDoc.ActiveDoc.Layers.FindName("Gems");

            if (GemLayerRoot == null)
            {
                RhinoApp.WriteLine("This Rhino file doesn't contain any gemstones");
                return Result.Failure;
            }

            Layer[] GemLayerChildren = GemLayerRoot.GetChildren(); //Todas las sublayers de "Gems"

            foreach(Layer layer in GemLayerChildren)
            {
                RhinoObject[] objectsInGemLayers = doc.Objects.FindByLayer(layer);
                foreach(RhinoObject obj in objectsInGemLayers)
                {
                    if(obj.Name == "Gem")
                    {
                        allGemsId.Add(obj.Id);
                    }
                }
            }

            //DEFINÍ EL DISPLAY MODE RENDER
            DisplayModeDescription displayMode = Rhino.Display.DisplayModeDescription.FindByName("Raytraced");

            List<DisplayModeDescription> originalDisplayModes = new List<DisplayModeDescription>();

            ViewTable viewTable = doc.Views;
            foreach (RhinoView view in viewTable)
            {
                originalDisplayModes.Add(view.ActiveViewport.DisplayMode);
                view.ActiveViewport.DisplayMode = displayMode;
                view.ActiveViewport.ConstructionAxesVisible = false;
                view.ActiveViewport.ConstructionGridVisible = false;
            }
            JHandsPlugin.Instance.BrepDisplay.Enabled = true;

            //HIDE ALL OBJECTS EXCEPT GEMS
            List<Rhino.DocObjects.RhinoObject> allHidden = new List<Rhino.DocObjects.RhinoObject>();
            ObjectTable objectTable = doc.Objects;
            foreach (RhinoObject obj in objectTable)
            {
                bool add = true;
                foreach (Guid gemId in allGemsId)
                {
                    if(obj.Id == gemId)
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

            //CHANGE GEMS COLOURS
            List<ObjectAttributes> oldAtt = new List<ObjectAttributes>();
            foreach (Guid id in allGemsId)
            {
                var obj = doc.Objects.FindId(id);
                ObjectAttributes olda = obj.Attributes.Duplicate();
                oldAtt.Add(olda);
            }

            //setGemMaterial(doc, allGemsId, new Color4f(1, 0, 0, 0));

            //setGemMaterial(doc, allGemsId, new Color4f(0, 1, 0, 0));

            RenderOptions("Render Options", doc, allGemsId);

            
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
            foreach (Guid id in allGemsId)
            {
                var obj = doc.Objects.FindId(id);
                doc.Objects.ModifyAttributes(obj, oldAtt[attIndex], true);
                obj.CommitChanges();

                attIndex++;
            }
            
            return Result.Success;
        }

        private double RenderOptions(string prompt, RhinoDoc doc, List<Guid> allGemsId)
        {
            //Choose a value
            Rhino.Input.Custom.GetOption gp = new Rhino.Input.Custom.GetOption();
            OptionColor opt = new OptionColor(System.Drawing.Color.White);
            gp.SetCommandPrompt(prompt);
            gp.AddOption("Capture");
            gp.AddOptionColor("GemColor", ref opt);

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
                        RhinoView view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;
                        string path = Rhino.RhinoDoc.ActiveDoc.Path;
                        newViewCapture(view, path);
                    }
                    else if(gp.OptionIndex() == 2) //COLOR
                    {
                        System.Drawing.Color cP = opt.CurrentValue;
                        Color4f color = Color4f.FromArgb(cP.A, cP.R, cP.G, cP.B);
                        setGemMaterial(doc, allGemsId, color);
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
                string checkName = name + "_Render";
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
                string filename = name + "_Render" + ".jpeg";
                string finalPath = Path.Combine(docpath, filename);
                bitmap.Save(finalPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        private void setGemMaterial(RhinoDoc doc, List<Guid> allGemsId, Color4f gemColor)
        {
            //System.Drawing.Color colorSystem = System.Drawing.Color.FromArgb();

            int matindex = doc.Materials.Find("GemMaterial", true);

            if(matindex < 0)
            {
                var mat = new Rhino.DocObjects.Material();

                mat.ToPhysicallyBased();

                mat.Name = "GemMaterial";
                mat.DiffuseColor = gemColor.AsSystemColor();
                mat.PhysicallyBased.BaseColor = gemColor;
                mat.PhysicallyBased.Roughness = 0;

                //mat.PhysicallyBased.Opacity = 0.5; //tira una espuma blanca no entiendoooo
                mat.PhysicallyBased.Specular = 1;

                mat.PhysicallyBased.OpacityRoughness = 0;
                mat.PhysicallyBased.OpacityIOR = 0; //
                mat.PhysicallyBased.ReflectiveIOR = 2;

                //mat.PhysicallyBased.Alpha = 0;

                //

                /*
                mat.PhysicallyBased.SubsurfaceScatteringColor = gemColor;
                mat.PhysicallyBased.Subsurface = 0.5;
                mat.PhysicallyBased.SubsurfaceScatteringRadius = 0.5;
                */

                mat.CommitChanges();

                matindex = doc.Materials.Add(mat);
            }
            else
            {
                var mat = doc.Materials.FindIndex(matindex);
                mat.DiffuseColor = gemColor.AsSystemColor();
                mat.PhysicallyBased.BaseColor = gemColor;

                mat.CommitChanges();
            } 

            foreach (Guid id in allGemsId)
            {
                var obj = doc.Objects.FindId(id);
                obj.Attributes.MaterialIndex = matindex;
                obj.Attributes.MaterialSource = ObjectMaterialSource.MaterialFromObject;
                obj.CommitChanges();
            }

            doc.Views.Redraw();
        }
    }
}