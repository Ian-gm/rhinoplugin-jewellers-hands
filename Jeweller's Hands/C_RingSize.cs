using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;

namespace JewellersHands
{
    public class C_RingSize : Rhino.Commands.Command
    {
        public C_RingSize()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_RingSize Instance { get; private set; }

        private double[] PickValue()
        {
            // For this example we will use a GetPoint class, but all of the custom
            // "Get" classes support command line options.
            Rhino.Input.Custom.GetNumber gp = new Rhino.Input.Custom.GetNumber();
            
            int optionIndex = 1;
            double ISOvalue;
            double radius;
            double diameter;

            gp.AddOption("ISO");
            gp.AddOption("Diameter");
            gp.AddOption("Radius");

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
                    optionIndex = gp.OptionIndex();

                    if (optionIndex == 1) //ISO
                    {
                        gp.SetCommandPrompt("Pick Ring ISO");
                        gp.SetLowerLimit(0, false);
                    }
                    else if (optionIndex == 2) //diameter
                    {
                        gp.SetCommandPrompt("Pick Ring Diameter");
                        gp.SetLowerLimit(11.63, false);
                    }
                    else if (optionIndex == 3) //radius
                    {
                        gp.SetCommandPrompt("Pick Ring Radius");
                        gp.SetLowerLimit(11.63/2, false);
                    }
                }
                else if(get_rc == Rhino.Input.GetResult.Number)
                {
                    //North American standard ISO to Diameter = (ISO * 0.8128) + 11.63. source: https://en.wikipedia.org/wiki/Ring_size
                    if (optionIndex == 1) //ISO
                    {
                        ISOvalue = gp.Number();
                        diameter = Math.Round((ISOvalue * 0.8128) + 11.63, 2);
                        radius = diameter / 2;
                    
                        return new[] { ISOvalue, radius };
                    }
                    else if (optionIndex == 2) //diameter
                    {
                        diameter = gp.Number();
                        radius = diameter / 2;
                        ISOvalue = Math.Round((diameter - 11.63) / 0.8128, 2);

                        return new[] { ISOvalue, radius };
                    }
                    else if (optionIndex == 3) //radius
                    {
                        radius = gp.Number();
                        diameter = radius * 2;
                        ISOvalue = Math.Round((diameter - 11.63) / 0.8128, 2);

                        return new[] { ISOvalue, radius };
                    }
                }
            }
            return null;
        }

        public override string EnglishName => "JH_RingSize";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.

            double[] ringSize = PickValue();
            if(ringSize == null)
            {
                return Result.Cancel;
            }

            Rhino.Geometry.Circle ringCircle = new Rhino.Geometry.Circle(Plane.WorldZX, ringSize[1]);
            ObjectAttributes circleAtt = new ObjectAttributes();
            int layerIndex = doc.Layers.Add("Ring Size " + ringSize[0].ToString(), System.Drawing.Color.FromArgb(128, 0, 0));
            if(layerIndex == -1)
            {
                layerIndex = doc.Layers.FindName("Ring Size " + ringSize[0]).Index;
            }
            circleAtt.LayerIndex = layerIndex;
            circleAtt.Name = "Ring Size " + ringSize[0];

            doc.Objects.AddCircle(ringCircle, circleAtt);
            doc.Views.Redraw();

            return Result.Success;
        }
    }
}