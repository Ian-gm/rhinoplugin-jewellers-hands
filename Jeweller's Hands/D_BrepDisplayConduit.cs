using Eto.Drawing;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Security.Cryptography;

namespace JewellersHands
{
    public class D_BrepDisplayConduit : DisplayConduit
    {
        public static D_BrepDisplayConduit Instance { get; private set; }
        Brep[] gemBreps { get; set; }
        Curve[] curvePreviews { get; set; }
        String[] caseTexts { get; set; }
        public int caseTextSize { get; set; }

        System.Drawing.Color[] caseColors { get; set; }

        public D_BrepDisplayConduit()
        {
            Instance = this;
            caseTextSize = 25;
        }

        /// <summary>
        /// Gets any Brep from the Rhino Model, extracts the Analysis Mesh, and sets Vertex Colors.
        /// </summary>
        public void SetObjects(Brep[] newGemBreps)
        {
            gemBreps = newGemBreps;
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void SetCurves(Curve[] newCurves)
        {
            curvePreviews = newCurves;
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void SetText(string[] newCases)
        {
            caseTexts = newCases;
        }

        public void SetColors(System.Drawing.Color[] newColors)
        {
            caseColors = newColors;
        }

        public void SetTextSize(int newTextSize)
        {
            caseTextSize = newTextSize;
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            base.DrawForeground(e);
            var bounds = e.Viewport.Bounds;
           
            double xMargin = bounds.Right;
            double largestLength = 0;
            List<double> margins = new List<double>();
            int amountOfPieces = 0;

            List <string[]> caseTextsPieces = new List<string[]>();

            if(caseTexts == null)
            {
                return;
            }

            foreach (string newCase in caseTexts)
            {
                string[] casePieces = newCase.Split(' ');

                int itemAmount = casePieces.Length;

                if(itemAmount > amountOfPieces)
                {
                    amountOfPieces = itemAmount;
                }

                caseTextsPieces.Add(casePieces);
            }

            for (int i = 0; i < amountOfPieces; i++)
            {
                largestLength = 0;
                double pieceLength = 0;

                foreach (string[] casePieces in caseTextsPieces)
                {
                    /*g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    pieceLength = g.MeasureString(casePieces[i], System.Drawing.FontFamily.GenericSerif.Name, int.MaxValue, StringFormat.GenericTypographic);*/

                    if(casePieces.Length < i)
                    {
                        return;
                    }
                    pieceLength = casePieces[i].Length;
                    if (pieceLength > largestLength)
                    {
                        largestLength = pieceLength;
                    }
                }

                margins.Add(largestLength);
            }


            List<double> finalMargins = new List<double>();
            for (int i = 0; i < margins.Count; i++)
            {
                double sum = 0;
                for (int j = i; j < margins.Count; j++)
                {
                    sum += margins[j] * caseTextSize;
                }
                finalMargins.Add(sum);
            }

            int index = 0;

            foreach (string[] casePieces in caseTextsPieces)
            {
                double yCoordinate = caseTextSize * (index + 1) * 1.15;
                for (int piece = amountOfPieces-1; piece >= 0; piece--)
                {
                    double xCoordinate = xMargin - finalMargins[piece];
                    string casePiece = casePieces[piece];
                    int number = 0;

                    if (piece == 0 && int.TryParse(casePiece, out number))
                    {
                        xCoordinate -= (casePiece.Length * caseTextSize * 0.55);
                    }
                    
                    var pt = new Rhino.Geometry.Point2d(xCoordinate, yCoordinate);
                    System.Drawing.Color caseColor = System.Drawing.Color.Black;
                    if (caseColors.Length > index)
                    {
                        caseColor = caseColors[index];
                    }
                    e.Display.Draw2dText(casePiece, caseColor, pt, false, caseTextSize); //System.Drawing.FontFamily.GenericMonospace.Name
                }
                index++;
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            bool run = false;

            Guid[] runningCommands = Rhino.Commands.Command.GetCommandStack();
            Guid? runningCommand = null;

            if (runningCommands != null && runningCommands.Length > 0)
            {
                runningCommand = runningCommands[runningCommands.Length - 1];
            }

            if (runningCommand == C_Gemstones.Instance.Id)
            {
                run = JHandsPlugin.Instance.PreviewGems;
            }
            else if (runningCommand == C_Array.Instance.Id)
            { 
                run = JHandsPlugin.Instance.PreviewArray;
            }
            else if (runningCommand == C_Mirror.Instance.Id)
            {
                run = JHandsPlugin.Instance.PreviewMirror;
            }
            else if (runningCommand == C_GemstonesCount.Instance.Id)
            {
                run = false;
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
                        if (null != gem)
                        {
                            e.Display.DrawBrepShaded(gem, material);
                            e.Display.DrawBrepWires(gem, System.Drawing.Color.Black);
                        }   
                    }
                }
                
                if (null != curvePreviews)
                {
                    foreach (Curve curve in curvePreviews)
                    {
                        if (null != curve)
                        {
                            e.Display.DrawCurve(curve, System.Drawing.Color.Red);
                        }
                    }
                }
                RhinoDoc.ActiveDoc.Views.Redraw();
            }
        }
    }
}

