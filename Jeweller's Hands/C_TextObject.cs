using Eto;
using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Rhino.DocObjects.Font;
using static System.Net.Mime.MediaTypeNames;

namespace JewellersHands
{

    /// <summary>
    /// A command that simulates the native Text command.
    /// </summary>
    public class C_TextObject : Rhino.Commands.Command
    {
        public C_TextObject()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        public static C_TextObject Instance { get; private set; }

        public override string EnglishName => "JH_TextObject";

        public TextEntity finalTextObject;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            JHandsPlugin.Instance.BrepDisplay.Enabled = true;

            var form = new TextObjectForm();
            var rc = form.ShowModal(RhinoEtoApp.MainWindow);

            if(rc == Result.Cancel) 
            {
                JHandsPlugin.Instance.BrepDisplay.Enabled = false;
                return Result.Cancel;
            }

            var gp = new GetTextPosition(finalTextObject);
            gp.SetCommandPrompt("Set Text Location");
            gp.Get();
            if (gp.CommandResult() != Result.Success)
            {
                return Result.Cancel;
            }

            finalTextObject.Plane = new Rhino.Geometry.Plane(gp.Point(), new Vector3d(0,0,1));
            ObjectAttributes objectAttributes = new ObjectAttributes();
            Layer fontLayer = new Layer();
            fontLayer.Name = finalTextObject.Font.FamilyName;
            int layerIndex = doc.Layers.Add(fontLayer);
            objectAttributes.LayerIndex = layerIndex;
            doc.Objects.AddText(finalTextObject, objectAttributes);
            
            JHandsPlugin.Instance.BrepDisplay.Enabled = false;
            return Result.Success;
        }
    }

    public class GetTextPosition : GetPoint
    {
        private TextEntity finalTextObject;

        public GetTextPosition(TextEntity textObject)
        {
            finalTextObject = textObject;
            RhinoApp.WriteLine(finalTextObject.TextHeight.ToString());
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            Rhino.Geometry.Plane P = new Rhino.Geometry.Plane(e.CurrentPoint, new Vector3d(0, 0, 1));
            finalTextObject.Plane = P;
            //e.Display.DrawText(finalTextObject, System.Drawing.Color.Black);
            e.Display.Draw3dText(
                finalTextObject.PlainText,
                System.Drawing.Color.Black,
                P,
                finalTextObject.TextHeight,
                finalTextObject.Font.FamilyName,
                false,
                false,
                TextHorizontalAlignment.Left,
                TextVerticalAlignment.Top);
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }
    }

    internal class TextObjectForm : Dialog<Rhino.Commands.Result>
    {
        private RichTextArea textarea1;
        private FontPicker fontPicker;
        public TextEntity textObject;
       
        public bool onStepper = false;

        public TextObjectForm()
        {
            Title = "Text Object";
            Resizable = true;
            textObject = new TextEntity();
            textarea1 = new RichTextArea() { Width = 250, Text = "New Text"};
            textObject.RichText = "New Text";
            if(JHandsPlugin.Instance.PickedFont != null)
            {
                textObject.Font = new Rhino.DocObjects.Font(JHandsPlugin.Instance.PickedFont.FamilyName);
            }
            textarea1.Focus();

            var sep1 = new TestSeparator { Text = "Text" };
            var sep2 = new TestSeparator { Text = "Font and Style" };

            if(JHandsPlugin.Instance.PickedFont!= null)
            {
                fontPicker = new FontPicker(JHandsPlugin.Instance.PickedFont);
            }
            else
            {
                fontPicker = new FontPicker();
            }
            
            fontPicker.ValueChanged += FontPicker_ValueChanged;

            DefaultButton = new Button { Text = "OK" };
            DefaultButton.Click += (sender, e) => { C_TextObject.Instance.finalTextObject = textObject; Close(Rhino.Commands.Result.Success); } ;

            AbortButton = new Button { Text = "C&ancel" };
            AbortButton.Click += (sender, e) => Close(Rhino.Commands.Result.Cancel);

            var buttons = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items = { null, DefaultButton, AbortButton }
            };

            Content = new StackLayout
            {
                Padding = new Padding(10),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Items =
                {
                    new StackLayoutItem(sep1, HorizontalAlignment.Stretch),
                    new TableLayout
                    {
                        Padding = 10,
                        Rows = { textarea1 }
                    },
                    new StackLayoutItem(sep2, HorizontalAlignment.Stretch),
                    new TableLayout
                    {
                        Padding = 10,
                        Rows = { fontPicker }
                    },
                    null,
                    buttons
                }
            };

            textarea1.TextChanged += Textarea1_TextChanged;
        }

        private void FontPicker_ValueChanged(object sender, EventArgs e)
        {
            textObject.Font = new Rhino.DocObjects.Font(fontPicker.Value.FamilyName);
            JHandsPlugin.Instance.PickedFont = fontPicker.Value;
            textarea1.Font = fontPicker.Value;
        }

        private void Textarea1_TextChanged(object sender, EventArgs e)
        {
            textObject.RichText = textarea1.Text;
        }
    }

    // ETO
}