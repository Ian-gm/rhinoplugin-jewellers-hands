using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace JewellersHands
{
    public class D_BrepDisplayConduit : DisplayConduit
    {
        public static D_BrepDisplayConduit Instance { get; private set; }
        Brep[] gemBreps { get; set; }

        public D_BrepDisplayConduit()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets any Brep from the Rhino Model, extracts the Analysis Mesh, and sets Vertex Colors.
        /// </summary>
        public void SetObjects(Brep[] newGemBreps)
        {
            gemBreps = newGemBreps;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            bool run;

            Guid[] runningCommands = Rhino.Commands.Command.GetCommandStack();
            Guid runningCommand = runningCommands[runningCommands.Length-1];

            if (runningCommand == C_Gemstones.Instance.Id)
            {
                run = JHandsPlugin.Instance.PreviewGems;
            }
            else if (runningCommand == C_Array.Instance.Id)
            { 
                run = JHandsPlugin.Instance.PreviewArray;
            }
            else
            {
                run = false;
            }

            if(run)
            {
                DisplayMaterial material = new DisplayMaterial();
                material.Diffuse = System.Drawing.Color.Gray;
                material.Specular = System.Drawing.Color.Silver;
                material.Emission = System.Drawing.Color.White;
                material.Transparency = 0.5;

                base.PostDrawObjects(e);
                if (null != gemBreps)
                {
                    foreach(Brep gem in gemBreps) 
                    {
                        e.Display.DrawBrepShaded(gem, material);
                        e.Display.DrawBrepWires(gem, System.Drawing.Color.Black);
                    }
                }
            }
        }
    }
}

