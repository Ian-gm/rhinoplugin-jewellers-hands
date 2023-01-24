﻿using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Windows.Markup;

namespace JewellersHands
{
    public class C_Weight : Rhino.Commands.Command
    {
        public C_Weight()
        {
            Instance = this;
        }

        public static Rhino.Commands.Result GetBrepsTotalVolume(Rhino.RhinoDoc doc)
        {
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            double totalVolume = 0;
            string calculation = "";
            string[] valuesType = new string[] { "Platinum", "24K", "22K", "20K", "18K", "14K", "10K", "Silver" };
            double[] valuesCoeficients = new double[] { 4.03,  3.624, 3.338,  3.128,  2.918, 2.512, 2.226, 1.926};

            go.SetCommandPrompt("Select objects to group");
            go.AcceptUndo(true);
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Brep;
            go.GetMultiple(1, 0);

            if (go.CommandResult() != Rhino.Commands.Result.Success)
                return go.CommandResult();

            foreach (var objref in go.Objects())
            {
                var brep = objref.Brep();
                totalVolume += brep.GetVolume();
            }

            calculation = totalVolume + "mm3";

            for(int i = 0; i < valuesType.Length; i++)
            {
                calculation += "\n";
                calculation += valuesType[i] + " = " + (valuesCoeficients[i] * totalVolume).ToString() + "gr";
            }


            Dialogs.ShowTextDialog(calculation, "Weight by volume");

            if (totalVolume >= 0)
                return Rhino.Commands.Result.Success;
            return Rhino.Commands.Result.Failure;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_Weight Instance { get; private set; }

        public override string EnglishName => "JH_Weight";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            GetBrepsTotalVolume(doc);
            return Result.Success;
        }
    }
}