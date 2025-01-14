﻿using Rhino;
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
using System.Data.SqlTypes;
using System.Drawing;


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
            RhinoApp.WriteLine("Material migration routine starts... doc.Materials method is being used");

            bool flag = false;

            /*var dialog = new SelectFolderDialog();
            dialog.ShowDialog(RhinoEtoApp.MainWindow);
            string folderPath = dialog.Directory;*/

            string folderPath = @"D:\Dropbox\Models\_Archive\_Assets";

            if(!Directory.Exists(folderPath))
            {
                flag = true;
                RhinoApp.WriteLine("Couldn't find the folder D:\\Dropbox\\Models\\_Archive\\_Assets");
                return Result.Failure;
            }

            /*

            BitmapTable BMTable = doc.Bitmaps;

            int amountofbitmaps = BMTable.Count;

            if(amountofbitmaps <= 0)
            {
                RhinoApp.WriteLine("There are no bitmaps in this file");
                return Result.Failure;
            }
            else
            {

                BMTable.ExportToFiles(folderPath, 1);
                BitmapEntry aBitmap = BMTable.FindIndex(0);
                aBitmap.
                for (int i = 0; i < amountofbitmaps; i++)
                {
                    Bitmap aBitmap = ;
                }
            }
            */

           

            int amountofmaterials = doc.Materials.Count;

            if (amountofmaterials == 0)
            {
                RhinoApp.WriteLine("There are no materials in this file");
                return Result.Failure;
            }
            else
            {
                //doc.RenderMaterials.BeginChange(RenderContent.ChangeContexts.Script);
                for (int i = 0; i < amountofmaterials; i++)
                {
                    RhinoApp.WriteLine(i.ToString() + "- ");
                    Material aMaterial = doc.Materials[i];
                    
                    Texture aTexture = aMaterial.GetBitmapTexture();

                    string texturefilePath = aTexture.FileName;
                    string texturefileName = Path.GetFileName(texturefilePath);

                    string newtexturefilePath = Path.Combine(folderPath, texturefileName);

                    if (File.Exists(newtexturefilePath))
                    {
                        RhinoApp.WriteLine(texturefileName + " already exists");
                    }
                    else
                    {
                        try
                        {
                            File.Copy(texturefilePath, newtexturefilePath, false);

                        }
                        catch
                        {
                            RhinoApp.WriteLine("Couldn't copy " + texturefileName);
                            flag = true;
                            continue;
                        }
                        RhinoApp.WriteLine(texturefileName + " was copied to chosen folder");
                    }

                    Material newMaterial = new Material();
                    newMaterial.Name = i.ToString();
                    Texture newTexture = new Texture();
                    newTexture.FileName = newtexturefilePath;
                    newMaterial.SetBitmapTexture(newTexture);
                    newMaterial.CommitChanges();

                    bool changed = doc.Materials.Modify(newMaterial, i, true);
                    if (!changed)
                    {
                        RhinoApp.WriteLine("Couldn't set " + texturefileName + " to material, please do so manually");
                    }
                    else
                    {
                        RhinoApp.WriteLine(texturefileName + " was set succesfully");
                    }

                    newMaterial.CommitChanges();
                    
                    /*
                    bool deleted = doc.Materials.Delete(newMaterial);

                    if (deleted)
                    {
                        RhinoApp.WriteLine("was deleted");
                    }
                    else
                    {
                        RhinoApp.WriteLine("could not be deleted");
                    }*/

                }
            }

            //doc.RenderMaterials.EndChange();

            if (!flag)
            {
                RhinoApp.WriteLine("Material migration was succesful");
                return Result.Success;
            }
            else
            {
                return Result.Failure;
            }

            /*
           //Material logic


           int amountofmaterials = doc.RenderMaterials.Count;

           if (amountofmaterials == 0)
           {
               RhinoApp.WriteLine("There are no materials in this file");
               return Result.Failure;
           }
           else
           {
               doc.RenderMaterials.BeginChange(RenderContent.ChangeContexts.Script);
               for (int i = 0; i < amountofmaterials; i++)
               {
                   RenderMaterial aMaterial = doc.RenderMaterials[i];
                   aMaterial.BeginChange(RenderContent.ChangeContexts.Script);
                   RenderTexture aTexture = aMaterial.GetTextureFromUsage(RenderMaterial.StandardChildSlots.Diffuse);
                   aTexture.BeginChange(RenderContent.ChangeContexts.Script);
                   string texturefilePath = aTexture.Filename;
                   string texturefileName = Path.GetFileName(texturefilePath);

                   //string newtexturefilePath = Path.Combine(folderPath, "texture_"+i.ToString()+Path.GetExtension(texturefilePath));

                   string newtexturefilePath = Path.Combine(folderPath, texturefileName);

                   if (File.Exists(newtexturefilePath))
                   {
                       RhinoApp.WriteLine(i.ToString() + "- " + texturefileName + " already exists");
                   }
                   else
                   {
                       try
                       {
                           File.Copy(texturefilePath, newtexturefilePath, false);

                       }
                       catch
                       {
                           RhinoApp.WriteLine(i.ToString() + "- Couldn't copy " + texturefileName);
                           flag = true;
                           continue;
                       }
                       RhinoApp.WriteLine(i.ToString() + "- " + texturefileName + " was copied to chosen folder");
                   }

                   aTexture.Filename = newtexturefilePath;
                   aTexture.EndChange();
                   aMaterial.EndChange();
               }
           }

           doc.RenderMaterials.EndChange();

           if (!flag)
           {
               RhinoApp.WriteLine("Material migration was succesful");
               return Result.Success;
           }
           else
           {
               return Result.Failure;
           }
           */
        }
    }
}
