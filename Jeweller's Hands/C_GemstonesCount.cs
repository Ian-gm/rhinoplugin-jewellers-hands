using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Eto.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

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

        private void BakeTextDot(Point3d point, string number, ObjectAttributes att)
        {
            Rhino.Geometry.TextDot textDot = new Rhino.Geometry.TextDot(number, point);
            RhinoDoc.ActiveDoc.Objects.AddTextDot(textDot, att);
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            double configurationResult = GemstonesCountOptions("Choose the Gemstone Count configuration");

            if (configurationResult == -1) { return Result.Failure; }

            Layer GemLayerRoot = Rhino.RhinoDoc.ActiveDoc.Layers.FindName("Gems");

            if (GemLayerRoot == null)
            {
                RhinoApp.WriteLine("This Rhino file doesn't contain any gemstones");
                return Result.Failure;
            }

            Layer[] GemLayerChildren = GemLayerRoot.GetChildren();

            List<string> GemTypeNames = new List<string>();
            List<Layer> notEmptyGemLayerChildren = new List<Layer>();

            foreach (Layer GemLayer in GemLayerChildren)
            {
                bool LayerIsEmpty = true;

                foreach (Rhino.DocObjects.RhinoObject obj in Rhino.RhinoDoc.ActiveDoc.Objects.FindByLayer(GemLayer))
                {
                    if (obj.Name == "Gem")
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

            List<string> dictionaryCaseKeys = new List<string>();
            List<Point3d> allPoints = new List<Point3d>();
            var dictionaryTypeHues = new Dictionary<string, float>();
            var dictionaryCasePts = new Dictionary<string, List<Point3d>>(); //Key = Type+Case (ex: ASC 2x10) _ Value = as many points as found.
            var dictionaryCaseObj = new Dictionary<string, List<RhinoObject>>(); //Key = Type+Case (ex: ASC 2x10) _ Value = as many objects as found.


            GemTypeNames.Sort();

            int indexForeachTypeCount = 1; //only needed to keep track of the foreach index


            foreach (string GemTypeName in GemTypeNames)
            {
                int gemTypeAmount = 0;

                float gemTypeHue = (359 / GemTypeNames.Count) * indexForeachTypeCount;

                dictionaryTypeHues.Add(GemTypeName, gemTypeHue);


                foreach (Layer GemLayer in notEmptyGemLayerChildren)
                {
                    string[] GemLayerName = GemLayer.Name.Split(' ');

                    if (GemLayerName[0] == GemTypeName)
                    {
                        string dictionaryKey = GemLayerName[0] + " " + GemLayerName[2].Trim();
                        dictionaryCaseKeys.Add(dictionaryKey);

                        List<Point3d> casePts = new List<Point3d>();
                        List<RhinoObject> caseObj = new List<RhinoObject>();

                        foreach (Rhino.DocObjects.RhinoObject obj in Rhino.RhinoDoc.ActiveDoc.Objects.FindByLayer(GemLayer))
                        {
                            if (obj.Name == "Gem")
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

            double size = 1.55;

            Rhino.Display.Text3d text3d = new Rhino.Display.Text3d("");
            text3d.VerticalAlignment = TextVerticalAlignment.Top;
            text3d.HorizontalAlignment = TextHorizontalAlignment.Left;
            text3d.Height = size;


            Layer tagLayer = new Layer();
            tagLayer.Name = "Tags";
            int tagLayerIndex = doc.Layers.Add(tagLayer);
            Guid tagLayerGuid = doc.Layers.FindIndex(tagLayerIndex).Id;
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
            Point3d Corner = allPointsBB.GetCorners()[1];
            Vector3d Diagonal = Corner - allPointsBB.Center;
            Corner += Diagonal*0.25;

            Transform movetoCorner = Transform.Translation(((Vector3d)Corner));
            caseTextDotPt.Transform(movetoCorner);
            hatch.Transform(movetoCorner);
            pXY.Transform(movetoCorner);
            text3d.TextPlane = pXY;

            foreach (string gemType in GemTypeNames)
            {
                List<string> gemTypeCases = dictionaryCaseKeys.FindAll((s => s.Contains(gemType)));
                int totalTypeCases = gemTypeCases.Count;
                int caseCount = 1;
                int gemTypeAmount = 0;

                hatch.Translate(nextLine);
                caseTextDotPt.Transform(nextLineT);

                foreach (string gemCase in gemTypeCases)
                {
                    List<Point3d> casePoints = dictionaryCasePts[gemCase];
                    gemTypeAmount += casePoints.Count;
                }
                text3d.Text += gemType + " = " + gemTypeAmount + "\n";
                gemTotalAmount += gemTypeAmount;

                foreach (string gemCase in gemTypeCases)
                {
                    List<Point3d> casePoints = dictionaryCasePts[gemCase];
                    List<RhinoObject> caseObjects = dictionaryCaseObj[gemCase];

                    float coef = (((float)1 / totalTypeCases) * caseCount);
                    float hue = dictionaryTypeHues[gemType];
                    float sat = ((float)0.3 + (coef * (float)0.7));
                    //float sat = 1; //(1 / totalTypeCases) * caseCount;
                    float bri = ((float)0.3 + (coef * (float)0.7));
                    Eto.Drawing.Color color = new ColorHSB(hue, sat, bri).ToColor();
                    System.Drawing.Color caseColor = System.Drawing.Color.FromArgb(color.Rb, color.Gb, color.Bb);


                    Layer newTagLayer = new Layer();
                    newTagLayer.Name = generalCaseCount.ToString() + ": " + gemCase;
                    newTagLayer.Color = caseColor;
                    newTagLayer.ParentLayerId = tagLayerGuid;
                    int layerIndex = doc.Layers.Add(newTagLayer);
                    Rhino.DocObjects.ObjectAttributes att = new Rhino.DocObjects.ObjectAttributes();
                    att.LayerIndex = layerIndex;

                    hatch.Translate(nextLine);
                    caseTextDotPt.Transform(nextLineT);
                    //doc.Objects.AddHatch(hatch, att);
                    TextDot caseTextDot = new TextDot(generalCaseCount.ToString(), caseTextDotPt);
                    doc.Objects.AddTextDot(caseTextDot, att);

                    foreach (Point3d point in casePoints)
                    {
                        if(JHandsPlugin.Instance.BakeTextDot)
                        {
                            BakeTextDot(point, generalCaseCount.ToString(), att);
                        }
                    }
                    foreach (RhinoObject obj in caseObjects)
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

                    double caseAmount = casePoints.Count;

                    string[] gemCaseName = gemCase.Split(' ');
                    text3d.Text += /*generalCaseCount.ToString() + */ "    " + gemCaseName[1] + " = " + caseAmount + "\n";

                    caseCount++;
                    generalCaseCount++;
                }
            }

            text3d.Text = "Gemstones = " + gemTotalAmount.ToString() + "\n" + text3d.Text;

            ObjectAttributes textatt = new ObjectAttributes();
            textatt.LayerIndex = tagLayerIndex;
            
            doc.Objects.AddText(text3d, textatt);
            doc.Views.Redraw();

            return Result.Success;
        }
    }
}