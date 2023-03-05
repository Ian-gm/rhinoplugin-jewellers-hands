using Rhino;
using Rhino.Geometry;
using System;
using Eto;

namespace JewellersHands
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class JHandsPlugin : Rhino.PlugIns.PlugIn
    {
        public D_BrepDisplayConduit BrepDisplay { get; private set; }

        public bool Mensajitos;

        //Public variables of C_Gemstones
        public bool PreviewGems = false;
        public bool PreviewArray = true;
        public bool PreviewMirror = false;

        //Public variables of C_GemstonesCount
        public bool BakeTextDot = false;
        public bool ChangeGemstonesColor = true;

        //Public variables of C_MirrorTrim
        public int Xmirror = 0;
        public int Ymirror = 0;

        public Eto.Drawing.Font PickedFont = null;

        public JHandsPlugin()
        {
            Instance = this;

            BrepDisplay = new D_BrepDisplayConduit();

            Mensajitos = false;

            PreviewGems = false;
            PreviewArray = true;
        }

        ///<summary>Gets the only instance of the GemstonePlugin plug-in.</summary>
        public static JHandsPlugin Instance { get; private set; }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.
    }
}