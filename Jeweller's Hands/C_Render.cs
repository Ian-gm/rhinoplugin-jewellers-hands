using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
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
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Render;
using Rhino.Render.ChangeQueue;
using Rhino.UI;

namespace JewellersHands
{
    [Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)]
    public class C_Render : Rhino.Commands.Command
    {
        public C_Render()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_Render Instance { get; private set; }

        public override string EnglishName => "JH_Render";

        private int OldMaterial(List<string> gemMatFiles, List<string> metalMatFiles, List<Guid> allGemsId, List<Guid> allMetalsId)
        {

            List<string> gemMatList = new List<string>();
            foreach (string file in gemMatFiles)
            {
                gemMatList.Add(Path.GetFileNameWithoutExtension(file));
            }

            List<string> metalMatList = new List<string>();
            foreach (string file in metalMatFiles)
            {
                metalMatList.Add(Path.GetFileNameWithoutExtension(file));
            }

            Rhino.Input.Custom.GetOption gp = new Rhino.Input.Custom.GetOption();
            gp.SetCommandPrompt("Choose a material");

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above
                var gemIndex = gp.AddOptionList("GemMaterial", gemMatList, 0);
                var metalIndex = gp.AddOptionList("MetalMaterial", metalMatList, 0);
                var capture = gp.AddOption("Capture");

                var get_rc = gp.Get();

                string smt = get_rc.ToString();

                int value = gp.OptionIndex();

                if (get_rc == Rhino.Input.GetResult.Option)
                {
                    var option = gp.Option();
                    if (option.Index == gemIndex)
                    {
                        int index = option.CurrentListOptionIndex;
                        string material = gemMatFiles[index];
                        SetMaterial(material, allGemsId);
                    }
                    else if(option.Index == metalIndex)
                    {
                        int index = option.CurrentListOptionIndex;
                        string material = metalMatFiles[index];
                        SetMaterial(material, allMetalsId);
                    }
                    else if(option.Index == capture)
                    {
                        RhinoView view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;
                        string path = Rhino.RhinoDoc.ActiveDoc.Path;
                        newViewCapture(view, path);
                    }
                    continue;
                }
                else if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                /*else if (gp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    tableIndex = gp.OptionIndex();
                    return tableIndex;
                }*/
                //break;
            }
            return -1;
        }

        private int ChooseMaterial(List<string> gemMatFiles, List<string> metalMatFiles, List<Guid> allGemsId, List<Guid> allMetalsId, DisplayModeDescription displayMode)
        {

            List<string> gemMatList = new List<string>();
            foreach (string file in gemMatFiles)
            {
                gemMatList.Add(Path.GetFileNameWithoutExtension(file));
            }

            List<string> metalMatList = new List<string>();
            foreach (string file in metalMatFiles)
            {
                metalMatList.Add(Path.GetFileNameWithoutExtension(file));
            }

            List<string> backgroundColors = new List<string>();
            List<System.Drawing.Color> systemColors = new List<System.Drawing.Color>();
            backgroundColors.Add("Black");
            systemColors.Add(System.Drawing.Color.Black);
            backgroundColors.Add("White");
            systemColors.Add(System.Drawing.Color.White);

            Rhino.Input.Custom.GetOption gp = new Rhino.Input.Custom.GetOption();
            gp.SetCommandPrompt("Choose a material");

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above
                var chooseGemMaterial = gp.AddOption("GemMaterial");
                var chooseMetalMaterial = gp.AddOption("MetalMaterial");
                var chooseBackground = gp.AddOption("BackgroundColour");
                var capture = gp.AddOption("Capture");

                var get_rc = gp.Get();

                if (get_rc == Rhino.Input.GetResult.Option)
                {
                    var option = gp.Option();
                    if (option.Index == chooseGemMaterial)
                    {
                        gp.ClearCommandOptions();
                        foreach(string name in gemMatList)
                        {
                            gp.AddOption(name);
                        }

                        gp.Get();
                        if (get_rc == Rhino.Input.GetResult.Option)
                        {
                            int index = gp.OptionIndex() - 1;
                            string material = gemMatFiles[index];
                            SetMaterial(material, allGemsId);
                        }

                        gp.ClearCommandOptions();
                        continue;
                    }
                    else if (option.Index == chooseMetalMaterial)
                    {
                        gp.ClearCommandOptions();
                        foreach (string name in metalMatList)
                        {
                            gp.AddOption(name);
                        }

                        gp.Get();
                        if (get_rc == Rhino.Input.GetResult.Option)
                        {
                            int index = gp.OptionIndex() -1;
                            string material = metalMatFiles[index];
                            SetMaterial(material, allMetalsId);
                        }

                        gp.ClearCommandOptions();
                        continue;
                    }
                    else if(option.Index == chooseBackground)
                    {
                        gp.ClearCommandOptions();
                        foreach (string name in backgroundColors)
                        {
                            gp.AddOption(name);
                        }

                        gp.Get();
                        if (get_rc == Rhino.Input.GetResult.Option)
                        {
                            int index = gp.OptionIndex() - 1;
                            System.Drawing.Color color = systemColors[index];
                            displayMode.DisplayAttributes.SetFill(color);
                            DisplayModeDescription.UpdateDisplayMode(displayMode);
                        }
                        RhinoDoc.ActiveDoc.Views.Redraw();
                        gp.ClearCommandOptions();
                        continue;
                    }
                    else if (option.Index == capture)
                    {
                        RhinoView view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;
                        string path = Rhino.RhinoDoc.ActiveDoc.Path;
                        newViewCapture(view, path);
                        gp.ClearCommandOptions();
                        continue;
                    }
                }
                else if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                /*else if (gp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    tableIndex = gp.OptionIndex();
                    return tableIndex;
                }*/
                //break;
            }
            return -1;
        }

        private void SetMaterial(string material, List<Guid> objId)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            //CREATE MATERIAL
            string matName = Path.GetFileNameWithoutExtension(material);

            int matIndex = 0;
            bool exists = false;
            RenderMaterialTable matRT = doc.RenderMaterials;
            for (int i = 0; i < matRT.Count; i++)
            {
                if (matRT[i].Name == matName)
                {
                    matIndex = i;
                    exists = true;
                }
            }

            if (!exists)
            {
                string rhinoCommand = String.Format("-_Materials \n _Options \n _LoadFromFile \n \"{0}\" \n _Enter \n _Enter", material);
                RhinoApp.RunScript(rhinoCommand, false);
                matIndex = matRT.Count - 1;
            }

            foreach (Guid id in objId)
            {
                var obj = doc.Objects.FindId(id);
                obj.Attributes.RenderMaterial = doc.RenderMaterials[matIndex];
                obj.CommitChanges();
            }

            doc.Views.Redraw();
        }

        public List<Guid> GetObjects(Rhino.RhinoDoc doc)
        {
            List<Guid> ObjGuid = new List<Guid>();

            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();

            go.SetCommandPrompt("Select metal objects");
            go.AcceptUndo(true);
            go.EnablePreSelect(true, true);
            go.EnableSelPrevious(true);
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Brep | Rhino.DocObjects.ObjectType.Mesh;

            while (true)
            {
                //go.ClearCommandOptions();
                var go_result = go.GetMultiple(1, 0);

                if (go_result == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    return null;
                }
                else if (go.CommandResult() == Rhino.Commands.Result.Failure)
                {
                    return null;
                }
                else if (go.CommandResult() == Rhino.Commands.Result.Success)
                {
                    foreach (var objref in go.Objects())
                    {
                        ObjGuid.Add(objref.ObjectId);
                    }
                    return ObjGuid;
                }
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //GET METAL OBJECTS

            List<Guid> allMetalsId = new List<Guid>();

            allMetalsId = GetObjects(doc);

            //READ ALL GEMS
            List<Guid> allGemsId = new List<Guid>();
            Layer GemLayerRoot = Rhino.RhinoDoc.ActiveDoc.Layers.FindName("Gems");

            /*
            if (GemLayerRoot == null)
            {
                RhinoApp.WriteLine("This Rhino file doesn't contain any gemstones");
                return Result.Failure;
            }
            */

            Layer[] GemLayerChildren = GemLayerRoot.GetChildren(); //Todas las sublayers de "Gems"

            foreach (Layer layer in GemLayerChildren)
            {
                RhinoObject[] objectsInGemLayers = doc.Objects.FindByLayer(layer);
                foreach (RhinoObject obj in objectsInGemLayers)
                {
                    if (obj.Name == "Gem")
                    {
                        allGemsId.Add(obj.Id);
                    }
                }
            }

            //DEFINÍ EL DISPLAY MODE RAYTRACED
            DisplayModeDescription displayMode = Rhino.Display.DisplayModeDescription.FindByName("Raytraced");
            displayMode.DisplayAttributes.FillMode = DisplayPipelineAttributes.FrameBufferFillMode.SolidColor;
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
                    if (obj.Id == gemId)
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

            //RESHOW METAL OBJECTS AND GET ATT
            List<ObjectAttributes> metalOldAtt = new List<ObjectAttributes>();
            foreach (Guid id in allMetalsId)
            {
                doc.Objects.Show(id, false);
                var obj = doc.Objects.FindId(id);
                ObjectAttributes olda = obj.Attributes.Duplicate();
                metalOldAtt.Add(olda);
            }

            //GET GEMS ORIGINAL ATT
            List<ObjectAttributes> gemOldAtt = new List<ObjectAttributes>();
            foreach (Guid id in allGemsId)
            {
                var obj = doc.Objects.FindId(id);
                ObjectAttributes olda = obj.Attributes.Duplicate();
                gemOldAtt.Add(olda);
            }

            doc.Views.Redraw();

            //READ GEM MATERIALS
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
            string strGemsPath = strWorkPath + "\\GemMaterials";

            string[] gemMatFiles = Directory.GetFiles(strGemsPath, "*.rmtl");

            //READ METAL MATERIALS
            string strMetalPath = strWorkPath + "\\MetalMaterials";

            string[] metalMatFiles = Directory.GetFiles(strMetalPath, "*.rmtl");

            //RUN COMMAND CONFIGURATION
            ChooseMaterial(gemMatFiles.ToList(), metalMatFiles.ToList(), allGemsId, allMetalsId, displayMode);

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
            foreach (Guid id in allMetalsId)
            {
                var obj = doc.Objects.FindId(id);
                doc.Objects.ModifyAttributes(obj, metalOldAtt[attIndex], true);
                obj.CommitChanges();

                attIndex++;
            }

            attIndex = 0;
            foreach (Guid id in allGemsId)
            {
                var obj = doc.Objects.FindId(id);
                doc.Objects.ModifyAttributes(obj, gemOldAtt[attIndex], true);
                obj.CommitChanges();

                attIndex++;
            }

            return Result.Success;
        }

        private void newViewCapture(RhinoView view, string path)
        {
            float textSize = JHandsPlugin.Instance.BrepDisplay.caseTextSize;
            /*float coef1 = ((float)480 / (float)view.ActiveViewport.Size.Height);
            float coef2 = ((float)640 / (float)view.ActiveViewport.Size.Width);
            float coef = coef1;
            if (coef1 < coef2)
            {
                coef = coef2;
            }
            float calculation = (float)textSize * coef;
            int newTextSize = (int)calculation;
            JHandsPlugin.Instance.BrepDisplay.SetTextSize((int)newTextSize);

            var view_capture = new ViewCapture
            {
                Width = 640,
                Height = 480,
                ScaleScreenItems = false,
                DrawAxes = false,
                DrawGrid = false,
                DrawGridAxes = false,
                TransparentBackground = false
            }; */
            string runCommand = "_Render";

            RhinoApp.RunScript(runCommand, false);

            /*
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

            var bitmap = view_capture.CaptureToBitmap(view);

            */
            string name = Path.GetFileNameWithoutExtension(path);
            string viewName = view.ActiveViewport.Name;

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


            string filename = name + "_Render" + prefix + ".jpeg";
            string finalPath = Path.Combine(docpath, filename);

            string saveCommand = "-_SaveRenderWindowAs \n \"" + finalPath + "\"";

            RhinoApp.RunScript(saveCommand, false);

            /*
            if (null != bitmap)
            {
                //string filename = name + "_" + viewName.Substring(0,1) + "V" + ".jpeg";
                string filename = name + "_Render" + prefix + ".jpeg";
                string finalPath = Path.Combine(docpath, filename);
                bitmap.Save(finalPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }*/

            JHandsPlugin.Instance.BrepDisplay.SetTextSize((int)textSize);
        }

        private void setGemMaterial(RhinoDoc doc, List<Guid> allGemsId, System.Drawing.Color cP)
        {
            int matindex = doc.Materials.Find("GemMaterial", true);

            var mat = new Rhino.DocObjects.Material();
            RenderMaterial matobj;

            if(matindex > 0)
            {
                //doc.Materials.DeleteAt(matindex);
                doc.RenderMaterials.Remove(doc.Materials.FindIndex(matindex).RenderMaterial);
            }

            mat = new Rhino.DocObjects.Material
            {
                Name = "GemMaterial",
                DiffuseColor = cP,
                SpecularColor = System.Drawing.Color.White
                    
            };

            mat.ToPhysicallyBased();

            Color4f gemColor = Color4f.FromArgb(cP.A, cP.R, cP.G, cP.B);
            mat.PhysicallyBased.BaseColor = gemColor;
            mat.PhysicallyBased.Roughness = 0;
            mat.PhysicallyBased.Specular = 1;
            mat.PhysicallyBased.OpacityRoughness = 0;
            mat.PhysicallyBased.OpacityIOR = 0; //
            mat.PhysicallyBased.ReflectiveIOR = 2;
            mat.CommitChanges();

            matindex = doc.Materials.Add(mat, false);

            foreach (Guid id in allGemsId)
            {
                var obj = doc.Objects.FindId(id);
                obj.Attributes.MaterialSource = ObjectMaterialSource.MaterialFromObject;
                obj.Attributes.MaterialIndex = matindex;
                //obj.Attributes.RenderMaterial = mat.RenderMaterial;
                
                obj.CommitChanges();
            }

            doc.Views.Redraw();
        }
    }
}