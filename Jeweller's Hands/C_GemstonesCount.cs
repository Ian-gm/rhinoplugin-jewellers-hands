using System;
using System.Collections.Generic;
using System.Drawing;
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

        private void BakeTextDot(Point3d point, string number, ObjectAttributes att)
        {
            Rhino.Geometry.TextDot textDot = new Rhino.Geometry.TextDot(number, point);
            RhinoDoc.ActiveDoc.Objects.AddTextDot(textDot, att);
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
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
            var dictionaryTypeHues = new Dictionary<string, float>();
            var dictionaryCasePts = new Dictionary<string, List<Point3d>>(); //Key = Type+Case (ex: ASC 2x10) _ Value = as many points as found.


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
                        foreach (Rhino.DocObjects.RhinoObject obj in Rhino.RhinoDoc.ActiveDoc.Objects.FindByLayer(GemLayer))
                        {
                            if (obj.Name == "Gem")
                            {
                                Point3d textDotPt = Brep.TryConvertBrep(obj.Geometry).GetBoundingBox(true).Center;
                                casePts.Add(textDotPt);
                            }
                        }

                        dictionaryCasePts.Add(dictionaryKey, casePts);

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
            circleC.Center = new Point3d(size*0.4, -size/2, -0.1);
            Curve circle = circleC.ToNurbsCurve();

            //doc.Objects.AddCurve(circle);
            Hatch hatch = Rhino.Geometry.Hatch.Create(circle, 0, 0, 1, 1)[0];

            Vector3d nextLine = new Vector3d(0, -size * 1.6, 0);
            Transform nextLineT = Transform.Translation(nextLine);

            int gemTotalAmount = 0;

            Point3d caseTextDotPt = circleC.Center;

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
                gemTotalAmount += gemTotalAmount;

                foreach (string gemCase in gemTypeCases)
                {
                    List<Point3d> casePoints = dictionaryCasePts[gemCase];
                    float coef = (((float)1 / totalTypeCases) * caseCount);
                    float hue = dictionaryTypeHues[gemType];
                    float sat = ((float)0.3 + (coef * (float)0.7));
                    //float sat = 1; //(1 / totalTypeCases) * caseCount;
                    float bri = ((float)0.3 + (coef * (float)0.7));
                    Eto.Drawing.Color color = new ColorHSB(hue, sat, bri).ToColor();

                    Layer newTagLayer = new Layer();
                    newTagLayer.Name = generalCaseCount.ToString() + ": " + gemCase;
                    newTagLayer.Color = System.Drawing.Color.FromArgb(color.Rb, color.Gb, color.Bb);
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
                        BakeTextDot(point, generalCaseCount.ToString(), att);
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