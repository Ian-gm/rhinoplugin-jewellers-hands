using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.FileIO;

namespace JewellersHands
{
    public class HistoryWithRhinoObject : Command
    {
        private const int HISTORY_VERSION = 20131107;

        public HistoryWithRhinoObject()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static HistoryWithRhinoObject Instance { get; private set; }

        public override string EnglishName => "MyRhinoCommand2";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            RhinoObject BakedC;

            File3dm gemFile = File3dm.Read(@"D:\OneDrive - Universidad Torcuato Di Tella\01_Proyectos\04_plugins\Jeweller's Hands\bin\Debug\net48\Gems\Cabochon.3dm",
                Rhino.FileIO.File3dm.TableTypeFilter.ObjectTable, File3dm.ObjectTypeFilter.Curve);

            File3dmObjectTable gemObjects = gemFile.Objects;

            Curve gemRail = null;
            Curve gemProfile = null;

            foreach (File3dmObject gemObject in gemObjects)
            {
                if (gemObject.Geometry.ObjectType == ObjectType.Curve)
                {
                    if(gemObject.Name == "Profile")
                    {
                        gemProfile = (Curve)gemObject.Geometry;
                    }
                    else if(gemObject.Name == "Rail")
                    {
                        gemRail = (Curve)gemObject.Geometry;
                    }
                }
            }

            Plane XY = Plane.WorldXY;
            float sizeX = 2;

            var sizeXScale = Rhino.Geometry.Transform.Scale(XY, sizeX, 1, 1);
            gemProfile.MakeDeformable();
            gemProfile.Transform(sizeXScale);
            bool deform = gemRail.MakeDeformable();
            NurbsCurve gemRailNurbs = gemRail.ToNurbsCurve();
            gemRailNurbs.Transform(sizeXScale);


            //PROFILE
            Guid profileId = doc.Objects.Add(gemProfile);
            doc.Objects.Select(profileId);

            ObjRef profileObjref;

            const Rhino.DocObjects.ObjectType filter = Rhino.DocObjects.ObjectType.Curve;
            Result rp = Rhino.Input.RhinoGet.GetOneObject("select a curve", false, filter, out profileObjref);

            Rhino.Geometry.Curve profileCurve = profileObjref.Curve();

            doc.Objects.UnselectAll();

            //RAIL
            Guid railId = doc.Objects.Add(gemRailNurbs);
            doc.Objects.Select(railId);

            ObjRef railObjref;

            Result rr = Rhino.Input.RhinoGet.GetOneObject("select a curve", false, filter, out railObjref);

            Rhino.Geometry.Curve railCurve = railObjref.Curve();


            NurbsSurface revolveSurface = NurbsSurface.CreateRailRevolvedSurface(profileCurve, railCurve, new Line(profileCurve.PointAtStart, profileCurve.PointAtEnd), true);
            //Surface newExtrude = Surface.CreateExtrusion(curve, new Vector3d(0, 0, 1));

            // Create a history record
            Rhino.DocObjects.HistoryRecord history = new Rhino.DocObjects.HistoryRecord(this, HISTORY_VERSION);
            WriteHistory(history, profileObjref, railObjref);

            doc.Objects.AddBrep(revolveSurface.ToBrep(), null, history, false);

            doc.Views.Redraw();

            return Result.Success;
        }

        protected override bool ReplayHistory(Rhino.DocObjects.ReplayHistoryData replay)
        {
            Rhino.DocObjects.ObjRef profileObjref = null;

            Rhino.DocObjects.ObjRef railObjref = null;

            if (!ReadHistory(replay, ref profileObjref, ref railObjref))
                return false;

            Rhino.Geometry.Curve profileCurve = profileObjref.Curve();
            if (null == profileCurve)
                return false;

            Rhino.Geometry.Curve railCurve = railObjref.Curve();
            if (null == railCurve)
                return false;

            NurbsSurface revolveSurface = NurbsSurface.CreateRailRevolvedSurface(profileCurve, railCurve, new Line(profileCurve.PointAtStart, profileCurve.PointAtEnd), true);

            replay.Results[0].UpdateToBrep(revolveSurface.ToBrep(), null);

            return true;
        }

        private bool ReadHistory(Rhino.DocObjects.ReplayHistoryData replay, ref Rhino.DocObjects.ObjRef profileObjref, ref Rhino.DocObjects.ObjRef railObjref)
        {
            if (HISTORY_VERSION != replay.HistoryVersion)
                return false;

            profileObjref = replay.GetRhinoObjRef(0);
            if (null == profileObjref)
                return false;

            railObjref = replay.GetRhinoObjRef(1);
            if (null == railObjref)
                return false;

            return true;
        }

        private bool WriteHistory(Rhino.DocObjects.HistoryRecord history, Rhino.DocObjects.ObjRef profileObjref, Rhino.DocObjects.ObjRef railObjref)
        {
            if (!history.SetObjRef(0, profileObjref))
                return false;
            if(!history.SetObjRef(1, railObjref))
                return false;

            return true;
        }
    }
}