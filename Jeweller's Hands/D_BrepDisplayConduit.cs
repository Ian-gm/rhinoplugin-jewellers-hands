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
        Brep gemBrep { get; set; }

        public D_BrepDisplayConduit()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets any Brep from the Rhino Model, extracts the Analysis Mesh, and sets Vertex Colors.
        /// </summary>
        public void SetObjects(Brep newGemBrep)
        {

            gemBrep = newGemBrep;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
            
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            if (JHandsPlugin.Instance.PreviewGems)
            {
                DisplayMaterial material = new DisplayMaterial();
                material.Diffuse = System.Drawing.Color.Gray;
                material.Specular = System.Drawing.Color.Silver;
                material.Emission = System.Drawing.Color.White;
                material.Transparency = 0.5;

                base.PostDrawObjects(e);
                if (null != gemBrep)
                {
                    e.Display.DrawBrepShaded(gemBrep, material);
                    e.Display.DrawBrepWires(gemBrep, System.Drawing.Color.Black);
                }
            }
        }
    }
}

