using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace JewellersHands
{
    public class HistoryWithObjRef : Command
    {

        private const int HISTORY_VERSION = 20131107;

        public HistoryWithObjRef()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static HistoryWithObjRef Instance { get; private set; }

        public override string EnglishName => "XHistoryFiestTrial";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            ObjRef objref;

            const Rhino.DocObjects.ObjectType filter = Rhino.DocObjects.ObjectType.Curve;
            Result rc = Rhino.Input.RhinoGet.GetOneObject("select a curve", false, filter, out objref);

            Rhino.Geometry.Curve curve = objref.Curve();

            Surface newExtrude = Surface.CreateExtrusion(curve, new Vector3d(0, 0, 1));

            // Create a history record
            Rhino.DocObjects.HistoryRecord history = new Rhino.DocObjects.HistoryRecord(this, HISTORY_VERSION);
            WriteHistory(history, objref);

            doc.Objects.AddSurface(newExtrude, null, history, false);

            doc.Views.Redraw();

            return Result.Success;
        }

        protected override bool ReplayHistory(Rhino.DocObjects.ReplayHistoryData replay)
        {
            Rhino.DocObjects.ObjRef objref = null;

            if (!ReadHistory(replay, ref objref))
                return false;

            Rhino.Geometry.Curve curve = objref.Curve();
            if (null == curve)
                return false;

            Surface newExtrude = Surface.CreateExtrusion(curve, new Vector3d(0, 0, 1));

            replay.Results[0].UpdateToSurface(newExtrude, null);

            return true;
        }

        private bool ReadHistory(Rhino.DocObjects.ReplayHistoryData replay, ref Rhino.DocObjects.ObjRef objref)
        {
            if (HISTORY_VERSION != replay.HistoryVersion)
                return false;

            objref = replay.GetRhinoObjRef(0);
            if (null == objref)
                return false;

            return true;
        }

        private bool WriteHistory(Rhino.DocObjects.HistoryRecord history, Rhino.DocObjects.ObjRef objref)
        {
            if (!history.SetObjRef(0, objref))
                return false;

            return true;
        }
    }
}