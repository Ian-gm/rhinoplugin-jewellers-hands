﻿using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Windows.Markup;


namespace JewellersHands
{
    public class C_Gemstones : Rhino.Commands.Command
    {

        private const int HISTORY_VERSION = 20131107;


        public C_Gemstones()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static C_Gemstones Instance { get; private set; }

        private int ChooseGemstone(List<string> gemList)
        {
            //Choose Gemstone
            int gemIndex;
            // For this example we will use a GetPoint class, but all of the custom
            // "Get" classes support command line options.
            Rhino.Input.Custom.GetOption gp = new Rhino.Input.Custom.GetOption();
            gp.SetCommandPrompt("Choose a gemstones");

            // set up the options
            // for (int i = 0; i <= (gemNames.Length-1); i++)
            gemList.ForEach(delegate (string gemName)
            {
                gp.AddOption(gemName.Substring(0, gemName.Length - 4));
            });

            
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
                else if (gp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    gemIndex = gp.OptionIndex();
                    return gemIndex;
                }
                break;
            }
            return -1;
        }

        private Dictionary<string, Tuple<int, string>> ReadGemstonesData(string path)
        {
            var dict = new Dictionary<string, Tuple<int, string>>();

            string dump = File.ReadAllText(path);

            string[] typeSeparator = { "\n" };
            string[] eachGemstone;
            string[] nameSeparator = { ":" };
            string[] dataSeparator = { "," };           
            string gemName;

            eachGemstone = dump.Split(typeSeparator, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i <= eachGemstone.Length - 1; i++)
            {
                string[] gemNameandData;
                string[] gemData;

                gemNameandData = eachGemstone[i].Split(nameSeparator, 2, StringSplitOptions.RemoveEmptyEntries);
                gemName = gemNameandData[0].Trim();
                gemData = gemNameandData[1].Split(dataSeparator, 2, StringSplitOptions.RemoveEmptyEntries);
                
                dict.Add(gemName, new Tuple<int, string>(int.Parse(gemData[0]), gemData[1].Trim()));
            }

            return dict;
        }

        private double PickValue(string prompt)
        {
            //Choose a value
            Rhino.Input.Custom.GetNumber gp = new Rhino.Input.Custom.GetNumber();
            Rhino.Input.Custom.OptionToggle previewGem = new Rhino.Input.Custom.OptionToggle(JHandsPlugin.Instance.PreviewGems, "Off", "On");
            gp.SetCommandPrompt(prompt);
            gp.AddOptionToggle("Preview", ref previewGem);

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
                        if (JHandsPlugin.Instance.PreviewGems)
                        {
                            JHandsPlugin.Instance.PreviewGems = false;
                            previewGem.CurrentValue = false;
                            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
                        }
                        else
                        {
                            JHandsPlugin.Instance.PreviewGems = true;
                            previewGem.CurrentValue = true;
                            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
                        }
                    }
                }
                else if (gp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    if (gp.Number() <= 0)
                    {
                        RhinoApp.WriteLine("Value can't be cero or negative");
                    }
                    else
                    {
                        return Math.Round(gp.Number(), 2);
                    }
                }
            }
            return -1;
        }

        private double PickPercentage(string prompt, double value)
        {
            //Choose a percentage
            Rhino.Input.Custom.GetNumber gp = new Rhino.Input.Custom.GetNumber();
            gp.SetCommandPrompt(prompt);

            //Rhino.Input.Custom.OptionDouble percentage = new Rhino.Input.Custom.OptionDouble(65, 1, 200);
            /*Rhino.Input.Custom.OptionDouble percentageOption = new Rhino.Input.Custom.OptionDouble(65, 0.1, 200);
            Rhino.Input.Custom.OptionDouble numberOption = new Rhino.Input.Custom.OptionDouble(10, 0.1, 100);*/
            Rhino.Input.Custom.OptionToggle previewGem = new Rhino.Input.Custom.OptionToggle(JHandsPlugin.Instance.PreviewGems, "Off", "On");

            gp.AddOption("Percentage");
            gp.SetDefaultNumber(65);
            gp.AddOption("Number");
            gp.AddOptionToggle("Preview", ref previewGem);
            gp.SetLowerLimit(0, true);

            int optionIndex = 2; //Default to number state

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
                else if (gp.GotDefault())
                {   
                    if(gp.Number() > 0)
                    {
                        RhinoApp.WriteLine("Default chosen = 65% of width");
                        return Math.Round(gp.Number() * value * 0.01, 2);
                    }
                }
                else if (get_rc == Rhino.Input.GetResult.Option) //State logic
                { 
                    if(gp.OptionIndex() == 1) //Percentage
                    {
                        gp.SetCommandPrompt("Pick a Percentage");
                        gp.SetDefaultNumber(65);
                    }
                    else if(gp.OptionIndex() == 2) //Number
                    {
                        gp.SetCommandPrompt("Pick a Number");
                        gp.ClearDefault();
                    }
                    else if(gp.OptionIndex() == 3) //Toggle
                    {
                        if (JHandsPlugin.Instance.PreviewGems)
                        {
                            JHandsPlugin.Instance.PreviewGems = false;
                            previewGem.CurrentValue = false;
                            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
                        }
                        else
                        {
                            JHandsPlugin.Instance.PreviewGems = true;
                            previewGem.CurrentValue = true;
                            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
                        }
                    }

                    if (gp.OptionIndex() != 3) //Toggle isn't a state
                    {
                        optionIndex = gp.OptionIndex();
                    }
                }
                else if (get_rc == Rhino.Input.GetResult.Number)
                {
                    if (optionIndex == 1) //Percentage
                    {
                        return Math.Round(gp.Number() * value * 0.01, 2);
                    }
                    else if (optionIndex == 2) //Number
                    {
                        return Math.Round(gp.Number(), 2);
                    }
                }
            }
            return -1;
        }

        private void RandomMessage(int indexNum)
        {
            if(JHandsPlugin.Instance.Mensajitos)
            {
                List<string> Messages = new List<string>() { "Nice choice!", "Cute <3", "(¬‿¬) woh", "Relindo te quedoooooo", "Sos tremendo disainer" };
                indexNum = indexNum % Messages.Count;
                RhinoApp.WriteLine(Messages[indexNum]);
            }
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "JH_Gemstones";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: start here modifying the behaviour of your command.
            // ---
           
            RhinoApp.WriteLine("Choose a Gem", EnglishName);

            Brep gemBrep = null;
            Curve gemCurve = null;
            
            System.Drawing.Color gemColor = System.Drawing.Color.FromArgb(255,0,0);

            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
            string strGemsPath = strWorkPath + "\\Gems";
            string strGemstonesDataPath = strWorkPath + "\\GemstonesData.txt";
            string[] gemFiles = Directory.GetFiles(strGemsPath,"*.3dm");
            
            List<string> gemList = new List<string>();

            int findfilename;
            foreach (string file in gemFiles)
            {
                string extension = Path.GetExtension(file);

                if (extension.Equals(".3dm"))
                {
                    findfilename = file.LastIndexOf("\\");

                    gemList.Add(file.Substring(findfilename + 1));
                }
            }


            int gemIndex = ChooseGemstone(gemList);

            if(gemIndex == -1)
            { 
                return  Result.Cancel;
            }
            string gemFileName = gemList[gemIndex - 1];

            if (gemIndex == -1)
            {
                return Result.Cancel;
            }

            File3dm gemFile = File3dm.Read(strGemsPath + "\\" + gemFileName,
                Rhino.FileIO.File3dm.TableTypeFilter.ObjectTable, Rhino.FileIO.File3dm.ObjectTypeFilter.Brep  & File3dm.ObjectTypeFilter.Curve);

            File3dmObjectTable gemObjects = gemFile.Objects;

            Curve gemRail = null;
            Curve gemProfile = null;

            foreach (File3dmObject gemObject in gemObjects)
            { 
                if(gemObject.Geometry.HasBrepForm)
                {
                    gemBrep = Brep.TryConvertBrep(gemObject.Geometry);
                    gemColor = gemObject.Attributes.ObjectColor;
                }
                else if(gemObject.Geometry.ObjectType == ObjectType.Curve)
                {

                    if (gemObject.Name == "Profile")
                    {
                        gemProfile = (Curve)gemObject.Geometry;
                    }
                    else if (gemObject.Name == "Rail")
                    {
                        Curve gemR = (Curve)gemObject.Geometry;
                        gemRail = gemR.ToNurbsCurve();
                    }
                    else
                    {
                        gemCurve = (Curve)gemObject.Geometry;
                    }
                }
            }

            if (gemBrep == null)
            {
                RhinoApp.Write("The Gem " + gemFileName + " could not be loaded");
                return Result.Failure;
            }

            JHandsPlugin.Instance.BrepDisplay.Enabled = true;
            JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });

            //Gems data as dictionary. Key = Filename. Values = (int) type / (string) Abbreviation

            //Dictionary
            var dict = ReadGemstonesData(strGemstonesDataPath);

            Tuple<int, string> gemData;

            string gemName = Path.GetFileNameWithoutExtension(gemFileName);

            if(dict.TryGetValue(gemName, out gemData))
            {
                gemData = dict[gemName];
            }
            else
            {
                RhinoApp.WriteLine($"{gemName}'s information is missing in the GemstonesData.txt file, please fill up it's information");
                return Result.Failure;
            }

            double sizeY;
            double sizeX;
            double height;
            double diameter;
            double sizeX2;
            double taper;

            double[] values;

            Plane XY = Plane.WorldXY;

            //Pick values and transform the Gem
            if (gemData.Item1 == 1) //Round Type
            {
                diameter = PickValue("Diameter of Gem");
                if (diameter < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var diameterScale = Rhino.Geometry.Transform.Scale(XY, diameter, diameter, 1);
                gemBrep.Transform(diameterScale);
                if(gemCurve != null)
                {
                    gemCurve.Transform(diameterScale);
                }
                if(gemProfile != null)
                {
                    gemProfile.Transform(diameterScale);
                }
                if(gemRail != null)
                {
                    gemRail.Transform(diameterScale);
                }
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)diameter));

                height = PickPercentage("Depth of Gem", diameter);
                if (height < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var heightScale = Rhino.Geometry.Transform.Scale(XY, 1, 1, height);
                gemBrep.Transform(heightScale);
                if(gemCurve != null)
                {
                    gemCurve.Transform(heightScale);
                }
                if (gemProfile != null)
                {
                    gemProfile.Transform(heightScale);
                }
                if (gemRail != null)
                {
                    gemRail.Transform(heightScale);
                }
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage((int)height);

                values = new double[] { diameter, height };
            }
            else if (gemData.Item1 == 3) //Tappered Type
            {
                sizeY = PickValue("Length of Gem");
                if (sizeY < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var sizeYScale = Rhino.Geometry.Transform.Scale(XY, 1, sizeY, 1);
                gemBrep.Transform(sizeYScale);
                if(gemCurve != null)
                {
                    gemCurve.Transform(sizeYScale);
                }
                if(gemProfile != null)
                {
                    gemProfile.Transform(sizeYScale);
                }
                if(gemRail != null)
                {
                    gemRail.Transform(sizeYScale);
                }
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)sizeY));

                sizeX = PickValue("Top Width of Gem");
                if (sizeX < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var sizeXScale = Rhino.Geometry.Transform.Scale(XY, sizeX, 1, 1);
                gemBrep.Transform(sizeXScale);
                if(gemCurve != null)
                {
                    gemCurve.Transform(sizeXScale);
                }
                if(gemProfile != null)
                {
                    gemProfile.Transform(sizeXScale);
                }
                if(gemRail != null)
                {
                    gemRail.Transform(sizeXScale);
                }
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)sizeX));

                sizeX2 = PickValue("Bottom Width of Gem");
                if (sizeX2 < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                taper = sizeX2 / sizeX;
                Rhino.Geometry.Morphs.TaperSpaceMorph taperTransform =
                new Rhino.Geometry.Morphs.TaperSpaceMorph(new Point3d(0, sizeY / 2, 0), new Point3d(0, -sizeY / 2, 0), 1, taper, true, true);
                var rotateXZ = Rhino.Geometry.Transform.RotationZYX(0, Math.PI / 2,0);
                var reverseRotateXZ = Rhino.Geometry.Transform.RotationZYX(0, -Math.PI / 2,0);
                gemBrep.Transform(rotateXZ);
                taperTransform.Morph(gemBrep);
                gemBrep.Transform(reverseRotateXZ);
                if (gemCurve != null)
                {
                    gemCurve.Transform(rotateXZ);
                    taperTransform.Morph(gemCurve);
                    gemCurve.Transform(reverseRotateXZ);
                }
                if(gemProfile != null)
                {
                    gemProfile.Transform(rotateXZ);
                    taperTransform.Morph(gemProfile);
                    gemProfile.Transform(reverseRotateXZ);
                }
                if(gemRail != null)
                {
                    gemRail.Transform(rotateXZ);
                    taperTransform.Morph(gemRail);
                    gemRail.Transform(reverseRotateXZ);
                }
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)sizeX2));

                height = PickPercentage("Depth of Gem", sizeX);
                if (height < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var heightScale = Rhino.Geometry.Transform.Scale(XY, 1, 1, height);
                gemBrep.Transform(heightScale);
                if(gemCurve != null)
                {
                    gemCurve.Transform(heightScale);
                }
                if(gemProfile!= null)
                {
                    gemProfile.Transform(heightScale);
                }
                if(gemRail!= null)
                {
                    gemRail.Transform(heightScale);
                }
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)height));

                values = new double[] { sizeX, sizeX2, sizeY, height };
            }
            else //if (gemData.Item1 == 2) //Default to Rectangle Type
            {
                sizeY = PickValue("Length of Gem");
                if (sizeY < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var sizeYScale = Rhino.Geometry.Transform.Scale(XY, 1, sizeY, 1);
                gemBrep.Transform(sizeYScale);
                if(gemCurve != null)
                {
                    gemCurve.Transform(sizeYScale);
                }
                if(gemProfile!= null)
                {
                    gemProfile.Transform(sizeYScale);
                }
                if(gemRail!=null)
                {
                    gemRail.Transform(sizeYScale);
                }
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)sizeY));

                sizeX = PickValue("Width of Gem");
                if (sizeX < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var sizeXScale = Rhino.Geometry.Transform.Scale(XY, sizeX, 1, 1);
                gemBrep.Transform(sizeXScale);
                if (gemCurve != null)
                {
                    gemCurve.Transform(sizeXScale);
                }
                if (gemProfile != null)
                {
                    gemProfile.Transform(sizeXScale);
                }
                if (gemRail != null)
                {
                    gemRail.Transform(sizeXScale);
                }
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)sizeX));

                height = PickPercentage("Depth of Gem", sizeX);
                if (height < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var heightScale = Rhino.Geometry.Transform.Scale(XY, 1, 1, height);
                gemBrep.Transform(heightScale);
                if (gemCurve != null)
                {
                    gemCurve.Transform(heightScale);
                }
                if (gemProfile != null)
                {
                    gemProfile.Transform(heightScale);
                }
                if (gemRail != null)
                {
                    gemRail.Transform(heightScale);
                }
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)height));

                values = new double[] { sizeX, sizeY, height };
            }


            JHandsPlugin.Instance.BrepDisplay.Enabled = false;

            if (gemBrep != null)
            {
                //Find Gems Layer
                int gemsLayerIndex;
                Layer gemsLayer = new Layer();

                if (doc.Layers.FindName("Gems") == null)
                {
                    gemsLayer.Name = "Gems";
                    doc.Layers.Add(gemsLayer);
                }

                gemsLayer = doc.Layers.FindName("Gems");
                gemsLayerIndex = gemsLayer.Index;

                //Create specific Gem sub-layer
                string[] gemAbbreviation = {"ASC", "BGT", "BLT", "CB", "CAD", "sCSN", "pCSN",
                                            "CSN", "EC", "HM", "HS", "KTE", "LZN", "MQ", "OV", 
                                            "PS", "PRL", "PR", "RD", "RND", "SHLD", "TP", "TR", "TRs" };

                string gemLayerName = gemData.Item2 + "  ";

                if (values.Length == 2)
                {
                    gemLayerName += values[0].ToString() 
                            + "x" + values[1].ToString();
                }
                else if(values.Length == 3)
                {
                    gemLayerName += values[1].ToString() 
                            + "x" + values[0].ToString() 
                            + "x" + values[2].ToString();
                }
                else
                {
                    gemLayerName += values[2].ToString()
                            + "x" + values[0].ToString()
                            + "x" + values[1].ToString()
                            + "x" + values[3].ToString();
                }

                Layer gemLayer = new Layer();
                gemLayer.Name = gemLayerName;
                gemLayer.ParentLayerId = gemsLayer.Id;
                gemLayer.Color = gemColor;
                int gemLayerIndex = doc.Layers.Add(gemLayer);
                if(gemLayerIndex == -1) { gemLayerIndex = doc.Layers.FindName(gemLayerName).Index; }

                ObjectAttributes gemAtt = new ObjectAttributes();
                gemAtt.Name = "Gem";
                gemAtt.LayerIndex = gemLayerIndex;

                ObjectAttributes crvAtt = new ObjectAttributes();
                crvAtt.LayerIndex = gemLayerIndex;

                Guid bakedBrep;
                Guid bakedProfile;
                Guid bakedRail;

                if (gemProfile != null && gemRail != null)
                {
                    //PROFILE
                    crvAtt.Name = "Profile";
                    bakedProfile = doc.Objects.Add(gemProfile, crvAtt);
                    doc.Objects.Select(bakedProfile);

                    Rhino.DocObjects.ObjRef profileObjref;

                    const Rhino.DocObjects.ObjectType filter = Rhino.DocObjects.ObjectType.Curve;
                    Result rp = Rhino.Input.RhinoGet.GetOneObject("select a curve", false, filter, out profileObjref);

                    Rhino.Geometry.Curve profileCurve = profileObjref.Curve();

                    doc.Objects.UnselectAll();

                    //RAIL
                    crvAtt.Name = "Rail";
                    bakedRail = doc.Objects.Add(gemRail, crvAtt);
                    doc.Objects.Select(bakedRail);

                    Rhino.DocObjects.ObjRef railObjref;

                    Result rr = Rhino.Input.RhinoGet.GetOneObject("select a curve", false, filter, out railObjref);

                    Rhino.Geometry.Curve railCurve = railObjref.Curve();

                    NurbsSurface revolveSurface = NurbsSurface.CreateRailRevolvedSurface(profileCurve, railCurve, new Line(profileCurve.PointAtStart, profileCurve.PointAtEnd), false);
                    //Surface newExtrude = Surface.CreateExtrusion(curve, new Vector3d(0, 0, 1));

                    // Create a history record
                    Rhino.DocObjects.HistoryRecord history = new Rhino.DocObjects.HistoryRecord(this, HISTORY_VERSION);
                    WriteHistory(history, profileObjref, railObjref);

                    bakedBrep = doc.Objects.AddBrep(revolveSurface.ToBrep(), gemAtt, history, false);

                    int groupIndex = doc.Groups.Add();
                    doc.Groups.AddToGroup(groupIndex, bakedBrep);
                    doc.Groups.AddToGroup(groupIndex, bakedProfile);
                    doc.Groups.AddToGroup(groupIndex, bakedRail);

                    doc.Objects.UnselectAll();
                }
                else
                {
                    bakedBrep = doc.Objects.AddBrep(gemBrep, gemAtt);

                    if (gemCurve != null)
                    {
                        crvAtt.Name = "Curve";
                        Guid bakedCurve = doc.Objects.AddCurve(gemCurve, crvAtt);

                        int groupIndex = doc.Groups.Add();

                        doc.Groups.AddToGroup(groupIndex, bakedBrep);
                        doc.Groups.AddToGroup(groupIndex, bakedCurve);
                    }
                }

                if (bakedBrep == null)
                {
                    RhinoApp.WriteLine("Couldn't bake brep", EnglishName);
                    return Result.Cancel;
                }
            }
            doc.Views.Redraw();
            RhinoApp.WriteLine("Baked a brep", EnglishName);

            // ---
            return Result.Success;
        }


        //HISTORY FUNCTIONS
        protected override bool ReplayHistory(Rhino.DocObjects.ReplayHistoryData replay)
        {
            Rhino.DocObjects.ObjRef profileObjref = null;

            Rhino.DocObjects.ObjRef railObjref = null;

            if (!ReadHistory(replay, ref profileObjref, ref railObjref))
                return false;

            Rhino.Geometry.Curve profileCurve = profileObjref.Curve();
            if (null == profileCurve)
                return false;

            Rhino.Geometry.Curve railCurve = railObjref.Curve();
            if (null == railCurve)
                return false;

            NurbsSurface revolveSurface = NurbsSurface.CreateRailRevolvedSurface(profileCurve, railCurve, new Line(profileCurve.PointAtStart, profileCurve.PointAtEnd), false);

            replay.Results[0].UpdateToBrep(revolveSurface.ToBrep(), null);

            return true;
        }

        private bool ReadHistory(Rhino.DocObjects.ReplayHistoryData replay, ref Rhino.DocObjects.ObjRef profileObjref, ref Rhino.DocObjects.ObjRef railObjref)
        {
            if (HISTORY_VERSION != replay.HistoryVersion)
                return false;

            profileObjref = replay.GetRhinoObjRef(0);
            if (null == profileObjref)
                return false;

            railObjref = replay.GetRhinoObjRef(1);
            if (null == railObjref)
                return false;

            return true;
        }

        private bool WriteHistory(Rhino.DocObjects.HistoryRecord history, Rhino.DocObjects.ObjRef profileObjref, Rhino.DocObjects.ObjRef railObjref)
        {
            if (!history.SetObjRef(0, profileObjref))
                return false;
            if (!history.SetObjRef(1, railObjref))
                return false;

            return true;
        }
    }
}
