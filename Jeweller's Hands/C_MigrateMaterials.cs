using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Render;
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
using System.Windows.Input;
using Eto.Forms;
using Rhino.UI;


namespace JewellersHands
{
    public class C_MigrateMaterials : Rhino.Commands.Command
    {

        public C_MigrateMaterials()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static C_MigrateMaterials Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "JH_MigrateMaterials";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var dialog = new SelectFolderDialog();

            dialog.ShowDialog(RhinoEtoApp.MainWindow);

            string folderPath = dialog.Directory;

            int amountofmaterials = doc.RenderMaterials.Count;

            if (amountofmaterials != 0)
            {
                doc.RenderMaterials.BeginChange(RenderContent.ChangeContexts.Script);
                for (int i = 0; i < amountofmaterials; i++)
                {
                    RenderMaterial aMaterial = doc.RenderMaterials[i];
                    aMaterial.BeginChange(RenderContent.ChangeContexts.Script);
                    RenderTexture aTexture = aMaterial.GetTextureFromUsage(RenderMaterial.StandardChildSlots.Diffuse);
                    aTexture.BeginChange(RenderContent.ChangeContexts.Script);
                    string texturefilePath = aTexture.Filename;

                    string newtexturefilePath = Path.Combine(folderPath, "texture_"+i.ToString()+Path.GetExtension(texturefilePath));

                    File.Copy(texturefilePath, newtexturefilePath, true);
                    
                    aTexture.Filename = newtexturefilePath;
                    RhinoApp.WriteLine(aMaterial.Name + " = " + texturefilePath);
                    aTexture.EndChange();
                    aMaterial.EndChange();
                }
            }

            doc.RenderMaterials.EndChange();

            //RhinoApp.WriteLine("Pick obj fotr material migrate", EnglishName);

            return Result.Success;
        }
    }
}
