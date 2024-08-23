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
using Rhino.Render;
using Rhino.UI;
using static Rhino.DocObjects.DimensionStyle;

namespace JewellersHands
{
    public class C_GemstonesCount : Rhino.Commands.Command
    {
        public C_GemstonesCount()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_GemstonesCount Instance { get; private set; }

        public override string EnglishName => "JH_GemstonesCount";
        private double GemstonesCountOptions(string prompt)
        {
            //Choose a value
            Rhino.Input.Custom.GetOption gp = new Rhino.Input.Custom.GetOption();
            Rhino.Input.Custom.OptionToggle bakeTextDot = new Rhino.Input.Custom.OptionToggle(JHandsPlugin.Instance.BakeTextDot, "Off", "On");
            Rhino.Input.Custom.OptionToggle changeGemstonesColor = new Rhino.Input.Custom.OptionToggle(JHandsPlugin.Instance.ChangeGemstonesColor, "Off", "On");
            //Rhino.Input.Custom.OptionToggle topCapture = new Rhino.Input.Custom.OptionToggle(true, "", "");
            gp.SetCommandPrompt(prompt);
            gp.AddOptionToggle("BakeTextDots", ref bakeTextDot);
            gp.AddOptionToggle("ChangeGemstonesColor", ref changeGemstonesColor);
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
                    if (gp.OptionIndex() == 1) //BakeTextDot
                    {
                        if (JHandsPlugin.Instance.BakeTextDot)
                        {
                            JHandsPlugin.Instance.BakeTextDot = false;
                            bakeTextDot.CurrentValue = false;
                        }
                        else
                        {
                            JHandsPlugin.Instance.BakeTextDot = true;
                            bakeTextDot.CurrentValue = true;
                        }
                    }
                    else if (gp.OptionIndex() == 2) //ChangeGemstonesColor
                    {
                        if (JHandsPlugin.Instance.ChangeGemstonesColor)
                        {
                            JHandsPlugin.Instance.ChangeGemstonesColor = false;
                            changeGemstonesColor.CurrentValue = false;
                        }
                        else
                        {
                            JHandsPlugin.Instance.ChangeGemstonesColor = true;
                            changeGemstonesColor.CurrentValue = true;
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

        private void BakeTextDot(Point3d point, string number, ObjectAttributes att)
        {
            Rhino.Geometry.TextDot textDot = new Rhino.Geometry.TextDot(number, point);
            RhinoDoc.ActiveDoc.Objects.AddTextDot(textDot, att);
        }

        private List<System.Drawing.Color> CreateColors(int totalAmount)
        {
            float h = 360;
            float[] hues = {0,
                            //(float)0.10 * h,
                            (float)0.20 * h,
                            /*(float)0.30 * h,*/
                            (float)0.35 * h,
                            (float)0.50 * h,
                            //(float)0.60 * h,
                            (float)0.75 * h,
                            (float)0.82 * h,
                            (float)0.90 * h,
                            };

            //Create color

            int amountofcycles = (totalAmount + hues.Length - (totalAmount % hues.Length)) / hues.Length;
            float satvalue = (float)0.5 / amountofcycles;

            List<System.Drawing.Color> caseColors = new List<System.Drawing.Color>();

            for (int i = 0; i < totalAmount + 1; i++)
            {
                int cycle = ((i / hues.Length) - (i / hues.Length % 1));
                float hue = hues[(i) % hues.Length];
                if(cycle % 2 == 1)
                {
                    hue += (float)0.05 * h;
                }

                float sat = ((float)1);
                float bri = 1 - ((float)satvalue * cycle);
                Eto.Drawing.Color color = new ColorHSB(hue, sat, bri).ToColor();
                System.Drawing.Color caseColor = System.Drawing.Color.FromArgb(color.Rb, color.Gb, color.Bb);

                caseColors.Add(caseColor);
            }

            return caseColors;
        }

        private void newViewCapture(RhinoView view, string path)
        {
            float textSize = JHandsPlugin.Instance.BrepDisplay.caseTextSize;
            /*
            float coef1 = ((float)480 / (float)view.ActiveViewport.Size.Height);
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
            };
            */

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
                string checkName = name + "_Gemlist";
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
                    if(fileNamePieces.Length > 1)
                    {
                        string possiblePrefix = fileNamePieces[fileNamePieces.Length-1];
                        
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
                prefix =  "-" + largestPrefix.ToString();
            }

            if (null != bitmap)
            {
                string filename = name + "_Gemlist" + prefix + ".jpeg";
                string finalPath = Path.Combine(docpath, filename);
                bitmap.Save(finalPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }


            JHandsPlugin.Instance.BrepDisplay.SetTextSize((int)textSize);
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Layer GemLayerRoot = Rhino.RhinoDoc.ActiveDoc.Layers.FindName("Gems");

            if (GemLayerRoot == null)
            {
                RhinoApp.WriteLine("This Rhino file doesn't contain any gemstones");
                return Result.Failure;
            }

            Layer[] GemLayerChildren = GemLayerRoot.GetChildren(); //Todas las sublayers de "Gems"

            List<string> GemTypeNames = new List<string>(); //Los tipos (Ej: ASC, BD, etc.)
            List<Layer> notEmptyGemLayerChildren = new List<Layer>();

            foreach (Layer GemLayer in GemLayerChildren)
            {
                bool LayerIsEmpty = true;

                if (GemLayer.IsVisible)
                {
                    foreach (Rhino.DocObjects.RhinoObject obj in Rhino.RhinoDoc.ActiveDoc.Objects.FindByLayer(GemLayer))
                    {
                        if (obj.Name == "Gem" && !obj.IsHidden)
                        {
                            LayerIsEmpty = false;
                            break;
                        }
                    }

                    if (!LayerIsEmpty)
                    {
                        string[] GemLayerName = GemLayer.Name.Split(' ');
                        if (!GemTypeNames.Contains(GemLayerName[0]))
                        {
                            GemTypeNames.Add(GemLayerName[0]);
                        }

                        notEmptyGemLayerChildren.Add(GemLayer);
                    }
                }
            }

            List<string> dictionaryCaseKeys = new List<string>(); //Los casos únicos (ej: ASC 2X10)
            List<Point3d> allPoints = new List<Point3d>();
            var dictionaryTypeHues = new Dictionary<string, float>();
            var dictionaryCasePts = new Dictionary<string, List<Point3d>>(); //Key = Type+Case (ex: ASC 2x10) _ Value = as many points as found.
            var dictionaryCaseObj = new Dictionary<string, List<RhinoObject>>(); //Key = Type+Case (ex: ASC 2x10) _ Value = as many objects as found.

            GemTypeNames.Sort();

            int indexForeachTypeCount = 1; //only needed to keep track of the foreach index

            foreach (string GemTypeName in GemTypeNames)
            {
                //int gemTypeAmount = 0;

                float gemTypeHue = (359 / GemTypeNames.Count) * indexForeachTypeCount;
                dictionaryTypeHues.Add(GemTypeName, gemTypeHue);

                foreach (Layer GemLayer in notEmptyGemLayerChildren)
                {
                    string[] GemLayerName = GemLayer.Name.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                    if (GemLayerName[0] == GemTypeName)
                    {

                        string dictionaryKey = "";
                        if(GemLayerName.Length > 1)
                        {
                            dictionaryKey = GemLayerName[0] + " " + GemLayerName[1].Trim();
                        }
                        else
                        {
                            dictionaryKey = GemLayerName[0] + " —";
                        }
                        dictionaryCaseKeys.Add(dictionaryKey);

                        List<Point3d> casePts = new List<Point3d>();
                        List<RhinoObject> caseObj = new List<RhinoObject>();

                        foreach (Rhino.DocObjects.RhinoObject obj in Rhino.RhinoDoc.ActiveDoc.Objects.FindByLayer(GemLayer))
                        {
                            if (obj.Name == "Gem" && !obj.IsHidden)
                            {
                                Point3d textDotPt = Brep.TryConvertBrep(obj.Geometry).GetBoundingBox(true).Center;
                                allPoints.Add(textDotPt);
                                casePts.Add(textDotPt);

                                caseObj.Add(obj);
                            }
                        }
                        dictionaryCasePts.Add(dictionaryKey, casePts);
                        dictionaryCaseObj.Add(dictionaryKey, caseObj);
                    }
                }

                indexForeachTypeCount++; //only needed to keep track of the foreach index
            }

            //List<System.Drawing.Color> caseColors = CreateColors(dictionaryCaseKeys.Count);

            List<System.Drawing.Color> caseColors = new List<System.Drawing.Color> {
                                        System.Drawing.Color.FromArgb(235, 205, 140),
                                        System.Drawing.Color.FromArgb(98, 152, 74),
                                        System.Drawing.Color.FromArgb(245, 179, 180),
                                        System.Drawing.Color.FromArgb(67, 135, 206),
                                        System.Drawing.Color.FromArgb(171, 202, 206),
                                        System.Drawing.Color.FromArgb(141, 84, 154),
                                        System.Drawing.Color.FromArgb(245, 154, 68),
                                        System.Drawing.Color.FromArgb(196, 69, 54),
                                        System.Drawing.Color.FromArgb(8, 61, 119),
                                        System.Drawing.Color.FromArgb(215, 208, 213),
                                        System.Drawing.Color.FromArgb(181, 139, 171),
                                        System.Drawing.Color.FromArgb(119, 131, 178),
            };


            if(dictionaryCaseKeys.Count > 12)
            {
                List<System.Drawing.Color> extraColors = CreateColors(dictionaryCaseKeys.Count - 12);
                foreach(System.Drawing.Color color in extraColors)
                {
                    caseColors.Add(color);
                }
            }

            double size = 1.55;

            Rhino.Display.Text3d text3d = new Rhino.Display.Text3d("");
            text3d.VerticalAlignment = TextVerticalAlignment.Top;
            text3d.HorizontalAlignment = TextHorizontalAlignment.Left;
            text3d.Height = size;

            if (doc.Layers.FindName("Tags") != null)
            {
                doc.Layers.Purge(doc.Layers.FindName("Tags").Index, true); 
            }
            Layer tagLayer = new Layer();
            tagLayer.Name = "Tags";
            int tagLayerIndex = doc.Layers.Add(tagLayer);
            Guid tagLayerGuid = doc.Layers.FindIndex(tagLayerIndex).Id;
            tagLayer.IsVisible = false;
            int generalCaseCount = 1;

            Plane pXY = Plane.WorldXY;
            Circle circleC = new Circle(pXY, (size / 2) * 1.5);
            circleC.Center = new Point3d(size * 0.4, -size / 2, -0.1);
            Curve circle = circleC.ToNurbsCurve();
            Hatch hatch = Rhino.Geometry.Hatch.Create(circle, 0, 0, 1, 1)[0];

            Vector3d nextLine = new Vector3d(0, -size * 1.6, 0);
            Transform nextLineT = Transform.Translation(nextLine);

            int gemTotalAmount = 0;

            Point3d caseTextDotPt = circleC.Center;

            BoundingBox allPointsBB = new BoundingBox(allPoints);
            Point3d Corner = allPointsBB.GetCorners()[2];
            Vector3d Diagonal = Corner - allPointsBB.Center;
            Corner += Diagonal * 0.25;

            Transform movetoCorner = Transform.Translation(((Vector3d)Corner));
            caseTextDotPt.Transform(movetoCorner);
            hatch.Transform(movetoCorner);
            pXY.Transform(movetoCorner);
            text3d.TextPlane = pXY;

            //HIDE EVERYTHING
            List<Rhino.DocObjects.RhinoObject> allHidden = new List<Rhino.DocObjects.RhinoObject>();
            ObjectTable objectTable = doc.Objects;
            foreach(RhinoObject obj in objectTable)
            {
                bool add = true;
                int layindex = obj.Attributes.LayerIndex;
                Layer foundlayer = doc.Layers.FindIndex(layindex);
                Guid parentid = foundlayer.ParentLayerId;

                if(obj.Name == "Gem")
                {
                    add = false;
                }
                /*
                if(parentid != null)
                {
                    Layer parentfoundlayer = doc.Layers.FindId(parentid);
                    if(parentfoundlayer != null)
                    {
                        if (parentfoundlayer.Name.Equals("Gems"))
                        {
                            
                        }
                    }
                }
                */
            if (add)
                {
                    allHidden.Add(obj);
                }
            }

            foreach(RhinoObject obj in allHidden)
            {
                doc.Objects.Hide(obj, false);
            }

            List<Rhino.DocObjects.RhinoObject> allText = new List<Rhino.DocObjects.RhinoObject>();
            List<string>allCases = new List<string>();
            List<Rhino.DocObjects.Layer> allLayers = new List<Rhino.DocObjects.Layer>();
            foreach (string gemType in GemTypeNames)
            {
                //List<string> gemTypeCases = dictionaryCaseKeys.FindAll(s => s.StartsWith(gemType));
                List<string> gemTypeCases = new List<string>();
                foreach (string caseKey in dictionaryCaseKeys)
                {
                    string[] gemCaseName = caseKey.Split(' ');
                    if (gemType.Equals(gemCaseName[0]))
                    {
                        gemTypeCases.Add(caseKey);
                    }
                }
                int totalTypeCases = gemTypeCases.Count;
                int caseCount = 1;
                int gemTypeAmount = 0;

                //RENGLÓN PARA CADA TIPO
                /*
                hatch.Translate(nextLine);
                caseTextDotPt.Transform(nextLineT);
                */

                //CONTAR LA CANTIDAD TOTAL ADENTRO DEL TIPO
                /* 
                foreach (string gemCase in gemTypeCases)
                {
                    List<Point3d> casePoints = dictionaryCasePts[gemCase];
                    gemTypeAmount += casePoints.Count;
                }
                text3d.Text += gemType + " = " + gemTypeAmount + "\n";
                gemTotalAmount += gemTypeAmount;*/

                foreach (string gemCase in gemTypeCases)
                {
                    List<Point3d> casePoints = dictionaryCasePts[gemCase];
                    List<RhinoObject> caseObjects = dictionaryCaseObj[gemCase];

                    System.Drawing.Color caseColor = caseColors[(generalCaseCount - 1) % caseColors.Count];

                    //LAYERS DE COLORES
                    Layer newTagLayer = new Layer();
                    newTagLayer.Name = generalCaseCount.ToString() + ": " + gemCase;
                    newTagLayer.Color = caseColor;
                    newTagLayer.ParentLayerId = tagLayerGuid;
                    int layerIndex = doc.Layers.Add(newTagLayer);
                    Rhino.DocObjects.ObjectAttributes att = new Rhino.DocObjects.ObjectAttributes();
                    att.LayerIndex = layerIndex;
                    allLayers.Add(newTagLayer);

                    //BAKE DE TEXT DOT 
                    if (JHandsPlugin.Instance.BakeTextDot)
                    {
                        hatch.Translate(nextLine);
                        caseTextDotPt.Transform(nextLineT);
                        //doc.Objects.AddHatch(hatch, att);
                        TextDot caseTextDot = new TextDot(generalCaseCount.ToString(), caseTextDotPt);
                        doc.Objects.AddTextDot(caseTextDot, att);
                    }

                    //TEXTO DEL CÁLCULO
                    double caseAmount = casePoints.Count;
                    string caseSize = "";
                    string[] gemCaseName = gemCase.Split(' ');


                    if(gemCaseName.Length < 1)
                    {
                        caseSize = "-";
                    }
                    else
                    {
                        string[] caseSizes = gemCaseName[1].Split('x');
                        
                        if (gemType == "RND")
                        {
                            caseSize = "⌀" + caseSizes[0];
                        }
                        else if (caseSizes.Length == 4)
                        {
                            caseSize = caseSizes[0] + "x" + caseSizes[1] + "x" + caseSizes[2];
                        }
                        else if (caseSizes.Length <= 3)
                        {
                            caseSize = caseSizes[0] + "x" + caseSizes[1];
                        }
                        else
                        {
                            caseSize = gemCaseName[1];
                        }
                    }

                    // text3d.Text += "    " + caseAmount + " " + gemType + " " + caseSize + "\n"; //VERSIÓN VIEJA ADITIVA
                    pXY.Transform(nextLineT);
                    text3d.TextPlane = pXY;

                    //VERSIÓN VIEJA PARA VARIAS LINEAS
                    //text3d.Text = "    " + caseAmount + " " + gemType + " " + caseSize + "\n";

                    Plane newXY = pXY.Clone();

                    ObjectAttributes textatt = new ObjectAttributes();
                    textatt.LayerIndex = layerIndex;
                    textatt.ColorSource = ObjectColorSource.ColorFromLayer;
                    Transform nextData = Transform.Translation(new Vector3d(5, 0, 0));

                    newXY.Transform(nextData);
                    text3d.TextPlane = newXY;
                    text3d.Text = caseAmount.ToString();
                    text3d.HorizontalAlignment = TextHorizontalAlignment.Right;
                    Guid textObj = doc.Objects.AddText(text3d, textatt);
                    allText.Add(doc.Objects.Find(textObj));

                    nextData = Transform.Translation(new Vector3d(2, 0, 0));
                    newXY.Transform(nextData);
                    text3d.TextPlane = newXY;
                    text3d.Text = gemType.ToString();
                    text3d.HorizontalAlignment = TextHorizontalAlignment.Left;
                    textObj = doc.Objects.AddText(text3d, textatt);
                    allText.Add(doc.Objects.Find(textObj));

                    nextData = Transform.Translation(new Vector3d(6, 0, 0));
                    newXY.Transform(nextData);
                    text3d.TextPlane = newXY;
                    text3d.Text = caseSize.ToString();
                    textObj = doc.Objects.AddText(text3d, textatt);
                    allText.Add(doc.Objects.Find(textObj));

                    foreach (Point3d point in casePoints) //TEXT DOTS SOBRE GEMAS
                    {
                        if (JHandsPlugin.Instance.BakeTextDot)
                        {
                            BakeTextDot(point, generalCaseCount.ToString(), att);
                        }
                    }
                    foreach (RhinoObject obj in caseObjects) //CAMBIO DE COLOR DE GEMAS
                    {
                        if (JHandsPlugin.Instance.ChangeGemstonesColor)
                        {
                            obj.Attributes.ObjectColor = caseColor;
                            obj.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
                            obj.CommitChanges();
                        }
                        else
                        {
                            obj.Attributes.ColorSource = ObjectColorSource.ColorFromLayer;
                            obj.CommitChanges();
                        }
                    }


                    string newCase = caseAmount + " " + gemType + " " + caseSize;
                    allCases.Add(newCase);
                    caseCount++;
                    generalCaseCount++;
                }
            }

            //TEXTO MONOLÍTICO CON TODA LA DATA
            /*
            text3d.Text = "Gemstones = " + gemTotalAmount.ToString() + "\n" + text3d.Text;

            ObjectAttributes textatt = new ObjectAttributes();
            textatt.LayerIndex = tagLayerIndex;
            textatt.ColorSource = ObjectColorSource.ColorFromLayer;
            

            doc.Objects.AddText(text3d, textatt);
            */

            //DEFINÍ EL DISPLAY MODE SHADED
            DisplayModeDescription displayMode = Rhino.Display.DisplayModeDescription.FindByName("Shaded");
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

            doc.Layers[tagLayerIndex].IsVisible = false;
            //doc.Layers.fin
            JHandsPlugin.Instance.BrepDisplay.SetText(allCases.ToArray());
            JHandsPlugin.Instance.BrepDisplay.SetColors(caseColors.ToArray());

            doc.Views.Redraw();

            GemstonesExit("Gemstone Count");

            string rhinoPath = doc.Path;

            if (JHandsPlugin.Instance.TakeViewCapture)
            {
                foreach (RhinoView view in viewTable)
                {
                    newViewCapture(view, rhinoPath);
                }
            }
            string[] emptytext = { "" };
            JHandsPlugin.Instance.BrepDisplay.SetText(emptytext);
            System.Drawing.Color[] emptycolors = { System.Drawing.Color.Black };
            JHandsPlugin.Instance.BrepDisplay.SetColors(emptycolors);
            JHandsPlugin.Instance.BrepDisplay.Enabled = false;


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
                    doc.Objects.Show(obj,false);
                }

                //GEM REVERT
                foreach (string gemCase in dictionaryCaseKeys)
                {
                    List<RhinoObject> caseObjects = dictionaryCaseObj[gemCase];

                    foreach (RhinoObject obj in caseObjects) //CAMBIO DE COLOR DE GEMAS
                    {
                        obj.Attributes.ColorSource = ObjectColorSource.ColorFromLayer;
                        obj.CommitChanges();
                    }
                }

                doc.Layers.Purge(doc.Layers.FindName("Tags").Index, false);
                /*
                bool currented = doc.Layers.SetCurrentLayerIndex(currentLayer.Index, true);
                //DELETE TAGS LAYER
                foreach (RhinoObject txt in allText)
                {
                    doc.Objects.Delete(txt);
                }

                foreach(Layer intagLayer in allLayers)
                {
                    doc.Layers.Purge(intagLayer.Index, false);
                }
                bool deleted = doc.Layers.Purge(tagLayer.Index, false);
                */
            }
            else
            {
                doc.Layers[tagLayerIndex].IsVisible = true;
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}