using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Markup;


namespace JewellersHands
{
    public class C_Gemstones : Rhino.Commands.Command
    {
        public C_Gemstones()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static C_Gemstones Instance { get; private set; }
        /*public static Rhino.Commands.Result CommandLineOptions(Rhino.RhinoDoc doc)
        {
            // For this example we will use a GetPoint class, but all of the custom
            // "Get" classes support command line options.
            Rhino.Input.Custom.GetPoint gp = new Rhino.Input.Custom.GetPoint();
            gp.SetCommandPrompt("GetPoint with options");

            // set up the options
            Rhino.Input.Custom.OptionInteger intOption = new Rhino.Input.Custom.OptionInteger(1, 1, 99);
            Rhino.Input.Custom.OptionDouble dblOption = new Rhino.Input.Custom.OptionDouble(2.2, 0, 99.9);
            Rhino.Input.Custom.OptionToggle boolOption = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            string[] listValues = new string[] { "Item0", "Item1", "Item2", "Item3", "Item4" };

            gp.AddOptionInteger("Integer", ref intOption);
            gp.AddOptionDouble("Double", ref dblOption);
            gp.AddOptionToggle("Boolean", ref boolOption);
            int listIndex = 3;
            int opList = gp.AddOptionList("List", listValues, listIndex);

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above
                Rhino.Input.GetResult get_rc = gp.Get();
                if (gp.CommandResult() != Rhino.Commands.Result.Success)
                    return gp.CommandResult();

                if (get_rc == Rhino.Input.GetResult.Point)
                {
                    doc.Objects.AddPoint(gp.Point());
                    doc.Views.Redraw();
                    Rhino.RhinoApp.WriteLine("Command line option values are");
                    Rhino.RhinoApp.WriteLine(" Integer = {0}", intOption.CurrentValue);
                    Rhino.RhinoApp.WriteLine(" Double = {0}", dblOption.CurrentValue);
                    Rhino.RhinoApp.WriteLine(" Boolean = {0}", boolOption.CurrentValue);
                    Rhino.RhinoApp.WriteLine(" List = {0}", listValues[listIndex]);
                }
                else if (get_rc == Rhino.Input.GetResult.Option)
                {
                    if (gp.OptionIndex() == opList)
                        listIndex = gp.Option().CurrentListOptionIndex;
                    continue;
                }
                break;
            }
            return Rhino.Commands.Result.Success;
        }
        */
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
            string[] gemNameandData;
            string[] dataSeparator = { "," };
            string[] gemData;
            string gemName;

            eachGemstone = dump.Split(typeSeparator, 100, StringSplitOptions.RemoveEmptyEntries);

            for(int i = 1; i <= eachGemstone.Length-1; i++)
            {
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
                        return gp.Number();
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
                        return gp.Number() * value * 0.01;
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
                        return gp.Number() * value * 0.01;
                    }
                    else if (optionIndex == 2) //Number
                    {
                        return gp.Number();
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
                findfilename = file.LastIndexOf("\\");

                gemList.Add(file.Substring(findfilename + 1));
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
                Rhino.FileIO.File3dm.TableTypeFilter.ObjectTable, Rhino.FileIO.File3dm.ObjectTypeFilter.Brep);

            File3dmObjectTable gemObjects = gemFile.Objects;

            foreach(File3dmObject gemObject in gemObjects)
            { 
                if(gemObject.Geometry.HasBrepForm)
                {
                    gemBrep = Brep.TryConvertBrep(gemObject.Geometry);
                    gemColor = gemObject.Attributes.ObjectColor;

                }
                else
                {
                    RhinoApp.WriteLine("The Gem chose does not contain a Brep in it's .3dm file");
                    return Result.Failure;
                }
            }

            if (gemBrep == null)
            {
                RhinoApp.Write("The Gem could not be loaded");
                return Result.Failure;
            }

            JHandsPlugin.Instance.BrepDisplay.Enabled = true;
            JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });

            //Gems data as dictionary. Key = Filename. Values = (int) type / (string) Abbreviation

            //Dictionary
            var dict = ReadGemstonesData(strGemstonesDataPath);

            Tuple<int, string> gemData;

            string gemName = gemFileName.Substring(0, gemFileName.Length - 4);

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
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)diameter));

                height = PickPercentage("Depth of Gem", diameter);
                if (height < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var heightScale = Rhino.Geometry.Transform.Scale(XY, 1, 1, height);
                gemBrep.Transform(heightScale);
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
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)sizeY));

                sizeX = PickValue("Top Width of Gem");
                if (sizeX < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var sizeXScale = Rhino.Geometry.Transform.Scale(XY, sizeX, 1, 1);
                gemBrep.Transform(sizeXScale);
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
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)sizeX2));

                height = PickPercentage("Depth of Gem", sizeX);
                if (height < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var heightScale = Rhino.Geometry.Transform.Scale(XY, 1, 1, height);
                gemBrep.Transform(heightScale);
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
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)sizeY));

                sizeX = PickValue("Width of Gem");
                if (sizeX < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var sizeXScale = Rhino.Geometry.Transform.Scale(XY, sizeX, 1, 1);
                gemBrep.Transform(sizeXScale);
                JHandsPlugin.Instance.BrepDisplay.SetObjects(new Brep[] { gemBrep });
                RandomMessage(((int)sizeX));

                height = PickPercentage("Depth of Gem", sizeX);
                if (height < 0) { JHandsPlugin.Instance.BrepDisplay.Enabled = false; return Result.Failure; }
                var heightScale = Rhino.Geometry.Transform.Scale(XY, 1, 1, height);
                gemBrep.Transform(heightScale);
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
                doc.Objects.AddBrep(gemBrep, gemAtt);
            }
            doc.Views.Redraw();
            RhinoApp.WriteLine("Baked a brep", EnglishName);

            // ---
            return Result.Success;
        }
    }
}
