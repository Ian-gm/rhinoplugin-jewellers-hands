using System;
using Rhino;
using Rhino.Commands;

namespace JewellersHands
{
    public class C_Mensajitos : Rhino.Commands.Command
    {
        public C_Mensajitos()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static C_Mensajitos Instance { get; private set; }

        public override string EnglishName => "Mensajitos";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            if(JHandsPlugin.Instance.Mensajitos == false)
            {
                JHandsPlugin.Instance.Mensajitos = true;
                RhinoApp.WriteLine("Mensajitos are ON");
            }
            else
            {
                JHandsPlugin.Instance.Mensajitos = false;
                RhinoApp.WriteLine("Mensajitos are OFF");
            }
            
            return Result.Success;
        }
    }
}