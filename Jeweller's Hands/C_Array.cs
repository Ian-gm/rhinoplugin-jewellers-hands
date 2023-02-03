using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Eto.Drawing;
using Eto.Forms;
using JewellersHands;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.UI;

namespace JewellersHands
{
    /// <summary>
    /// Utility class to handle array arguments and calculate offsets.
    /// </summary>
    class SampleCsArrayArgs
    {
        // Curve reference
        public Curve Curve { get; set; }

        // Gem reference
        public Brep Gem { get; set; }
        // Gem base point
        public Point3d basePoint { get; set; }

        // Object counts
        public int CountX { get; set; }

        // Distance between objects
        public double DistanceX { get; set; }

        public double ScaleX { get; set; }

        // The transformation plane
        public Rhino.Geometry.Plane Plane { get; set; }

        // The offsets to be used during transformation
        public List<Rhino.Geometry.Vector3d> Offsets;

        /// <summary>
        /// Constructor
        /// </summary>
        public SampleCsArrayArgs()
        {
            CountX = 1;
            ScaleX = 1;
            DistanceX = 0.0;
            Plane = Rhino.Geometry.Plane.WorldXY;
            Offsets = new List<Rhino.Geometry.Vector3d>();
        }

        /// <summary>
        /// Ensures that the capacity of the offset list is valid
        /// </summary>
        public void ValidateOffsets()
        {
            if (CountX < 1) CountX = 1;
            int offsetCount = CountX;
            if (Offsets.Count != offsetCount)
            {
                Offsets.Clear();
                Offsets.Capacity = offsetCount;
                Offsets.AddRange(Enumerable.Repeat(Rhino.Geometry.Vector3d.Zero, offsetCount));
            }
        }

        /// <summary>
        /// Resets the offset list
        /// </summary>
        public void ResetOffsets()
        {
            ValidateOffsets();
            for (int i = 0; i < Offsets.Count; i++)
                Offsets[i] = Rhino.Geometry.Vector3d.Zero;
        }

        public void FlipCurve()
        {
            Curve.Reverse();
            Curve.Domain = new Interval(0, 1);
        }

        /// <summary>
        /// Calculates the offsets (to be used later during transformation)
        /// </summary>
        public void CalculateOffsets()
        {
            Point3d[] gemBBCorners = Gem.GetBoundingBox(true).GetCorners();
            double gemRadius = gemBBCorners[0].DistanceTo(gemBBCorners[1]) / 2; //Gemstones radius

            Curve newCurve = Curve;
            Curve[] curveIntersection = null;
            Point3d[] pointIntersection = null;

            Point3d startPoint = newCurve.PointAtStart;
            Point3d endPoint = newCurve.PointAtEnd;
            List<Point3d> arrayIntersections = new List<Point3d>();
            arrayIntersections.Add(startPoint);
            int iteration = 0;
            double currentRadius = gemRadius;
            double nextRadius = gemRadius * ScaleX;

            bool availableSpace = startPoint.DistanceTo(endPoint) > (gemRadius + DistanceX + nextRadius);

            while (availableSpace)
            {
                startPoint = newCurve.PointAtStart;
                Sphere sphere = new Sphere(startPoint, currentRadius + DistanceX + nextRadius);
                bool gotIntersection = Intersection.CurveBrep(newCurve, sphere.ToBrep(), 0.1, out curveIntersection, out pointIntersection);

                if (pointIntersection != null & gotIntersection)
                {
                    Point3d newPoint = pointIntersection[0];
                    arrayIntersections.Add(newPoint);
                    double intersectionParameter;
                    newCurve.ClosestPoint(newPoint, out intersectionParameter);
                    newCurve = newCurve.Trim(intersectionParameter, 1);
                    newCurve.Domain = new Interval(0, 1);
                    iteration++;
                }
                else
                {
                    break;
                }

                currentRadius = nextRadius;
                nextRadius *= ScaleX;

                startPoint = newCurve.PointAtStart;
                endPoint = newCurve.PointAtEnd;
                availableSpace = startPoint.DistanceTo(endPoint) > (currentRadius + DistanceX + nextRadius);
            }

            CountX = arrayIntersections.Count;

            ValidateOffsets();

            int index = 0;

            for (int x = 0; x < CountX; x++)
            {
                Rhino.Geometry.Vector3d offset = new Vector3d(arrayIntersections[x]);

                Offsets[index++] = (offset.IsValid) ? offset : Rhino.Geometry.Vector3d.Zero;
            }
        }

        public Brep[] RunArray()
        {
            Brep[] GemBreps = new Brep[CountX];

            for (int i = 0; i < Offsets.Count; i++)
            {
                // Skip the first one...
                if (!Offsets[i].IsZero)
                {
                    Rhino.Geometry.Transform xform = Rhino.Geometry.Transform.Translation(Offsets[i] - new Vector3d(basePoint));
                    Rhino.Geometry.Transform sform = Rhino.Geometry.Transform.Scale(basePoint, 1);
                    if (i > 0)
                    {
                        sform = Rhino.Geometry.Transform.Scale(basePoint, Math.Pow(ScaleX, i));
                    }
                    
                    if (xform.IsValid && xform != Rhino.Geometry.Transform.Identity)
                    {
                        Rhino.Geometry.Brep dupBrep = Gem.DuplicateBrep();
                        dupBrep.Transform(sform);
                        dupBrep.Transform(xform);
                        if (dupBrep.IsValid)
                            GemBreps[i] = dupBrep;
                    }
                }
            }
            return GemBreps;
        }
    }
    // ETO

    internal class CoffeeDialog : Dialog<Rhino.Commands.Result>
    {
        public Slider sliderScale = new Slider 
        { 
            MaxValue = 2000, 
            MinValue = 10, 
            Value = 1000 
        };
        public NumericStepper stepperScale = new Eto.Forms.NumericStepper
        {
            DecimalPlaces = 2,
            MinValue = 0.01,
            MaxValue = 2,
            Value = 1,
        };
        public Slider sliderDistance = new Slider 
        { 
            MaxValue = 2000, 
            MinValue = 0, 
            Value = 1000 
        };
        public NumericStepper stepperDistance = new Eto.Forms.NumericStepper
        {
            DecimalPlaces = 1,
            MinValue = 0,
            MaxValue = 20,
            Value = 10,
        };

        public bool onStepper = false;

        public SampleCsArrayArgs innerargs { get; set; }

        public CoffeeDialog(SampleCsArrayArgs args)
        {
            innerargs = args;
            
            Title = "Array Gemstone";
            Resizable = true;

            var previewCheckbox = new CheckBox { Text = "Preview", Checked = JHandsPlugin.Instance.PreviewArray };
            var sep0 = new TestSeparator { Text = "Scale factor" };
            var sep1 = new TestSeparator { Text = "Distance between gems" };
            var sep2 = new TestSeparator { Text = "Curve" };
            var flipCheckbox = new CheckBox { Text = "Flip", Checked = false };

            DefaultButton = new Button { Text = "OK" };
            DefaultButton.Click += (sender, e) => Close(Rhino.Commands.Result.Success);

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
                  new TableLayout
                  {
                    Padding = 10,
                    Rows = {previewCheckbox}
                  },
                  new StackLayoutItem(sep0, HorizontalAlignment.Stretch),
                  new TableLayout
                  {
                    Padding = 10,
                    Rows = {stepperScale, sliderScale }
                  },
                  new StackLayoutItem(sep1, HorizontalAlignment.Stretch),
                  new TableLayout
                  {
                    Padding = 10,
                    Rows = {stepperDistance, sliderDistance }
                  },
                  new StackLayoutItem(sep2, HorizontalAlignment.Stretch),
                  new TableLayout
                  {
                    Padding = 10,
                    Rows = {flipCheckbox}
                  },
                  null,
                  buttons
                }
            };

            previewCheckbox.CheckedChanged += PreviewCheckbox_CheckedChanged;
            sliderScale.ValueChanged += SliderScale_ValueChanged;
            stepperScale.ValueChanged += StepperScale_ValueChanged;
            sliderDistance.ValueChanged += SliderDistance_ValueChanged;
            stepperDistance.ValueChanged += StepperDistance_ValueChanged;
            flipCheckbox.CheckedChanged += FlipCheckbox_CheckedChanged;

            stepperScale.GotFocus += StepperGotFocus;
            stepperScale.LostFocus += StepperLostFocus;
            stepperDistance.GotFocus += StepperGotFocus;
            stepperDistance.LostFocus += StepperLostFocus;
        }

        private void StepperGotFocus(object sender, EventArgs e)
        {
            onStepper = true;
        }
        private void StepperLostFocus(object sender, EventArgs e)
        {
            onStepper = false;
        }

        private void PreviewCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            JHandsPlugin.Instance.PreviewArray = !JHandsPlugin.Instance.PreviewArray;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }
        private void FlipCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            innerargs.FlipCurve();
            innerargs.CalculateOffsets();
            Brep[] previewBreps = innerargs.RunArray();
            JHandsPlugin.Instance.BrepDisplay.SetObjects(previewBreps);
        }
        private void StepperScale_ValueChanged(object sender, EventArgs e)
        {
            if (onStepper)
            {
                float scaleValue = (float)stepperScale.Value;
                sliderScale.Value = (int)scaleValue * 1000;
                RhinoApp.WriteLine("Scale slider value changed to" + scaleValue.ToString());
                OnValueChanged(scaleValue, 1);
            }
        }
        private void SliderScale_ValueChanged(object sender, EventArgs e)
        {
            if (!onStepper)
            {
                float scaleValue = (float)sliderScale.Value / 1000;
                stepperScale.Value = scaleValue;
                RhinoApp.WriteLine("Scale slider value changed to" + scaleValue.ToString());
                OnValueChanged(scaleValue, 1);
            }
        }
        private void StepperDistance_ValueChanged(object sender, EventArgs e)
        {
            if (onStepper)
            {
                float distanceValue = (float)stepperDistance.Value;
                sliderDistance.Value = (int)distanceValue * 100;
                RhinoApp.WriteLine("Distance slider value changed to" + distanceValue.ToString());
                OnValueChanged(distanceValue, 2);
            }  
        }
        private void SliderDistance_ValueChanged(object sender, EventArgs e)
        {
            if (!onStepper)
            {
                float distanceValue = (float)sliderDistance.Value / 100;
                stepperDistance.Value = distanceValue;
                RhinoApp.WriteLine("Distance slider value changed to" + distanceValue.ToString());
                OnValueChanged(distanceValue, 2);
            }
        }
        public void OnValueChanged(double value, int type)
        {
            if (type == 1) //scale
            {
                innerargs.ScaleX = value;
            }
            else if (type == 2) //distance
            {
                innerargs.DistanceX = value;
            }
            innerargs.CalculateOffsets();
            Brep[] previewBreps = innerargs.RunArray();
            JHandsPlugin.Instance.BrepDisplay.SetObjects(previewBreps);
        }
    }

    /// <summary>
    /// label with a line separator
    /// </summary>
    internal class TestSeparator : Panel
    {
        readonly Label m_label;
        readonly SampleCsDivider m_divider;

        public string Text
        {
            get { return m_label.Text; }
            set { m_label.Text = value; }
        }

        public Eto.Drawing.Color Color
        {
            get { return m_divider.Color; }
            set { m_divider.Color = value; }
        }

        public TestSeparator()
        {
            m_label = new Label();
            m_divider = new SampleCsDivider { Color = Colors.DarkGray };

            Content = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Spacing = 2,
                Items =
                {
                  m_label,
                  new StackLayoutItem(m_divider, true)
                }
            };
        }
    }

    /// <summary>
    /// Line separator
    /// </summary>
    internal class SampleCsDivider : Eto.Forms.Drawable
    {
        private Eto.Drawing.Color m_color;

        public Eto.Drawing.Color Color
        {
            get { return m_color; }
            set
            {
                if (m_color == value)
                    return;
                m_color = value;
                Invalidate();
            }
        }

        public Orientation Orientation => Width < Height
          ? Orientation.Vertical
          : Orientation.Horizontal;

        public SampleCsDivider()
        {
            m_color = Colors.DarkGray;
            Size = new Size(3, 3);
        }

        protected override void OnSizeChanged(System.EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        protected override void OnLoadComplete(System.EventArgs e)
        {
            base.OnLoadComplete(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var middle = new PointF(Size / 2);
            e.Graphics.FillRectangle(
              Color,
              Orientation == Orientation.Horizontal
                ? new RectangleF(0f, middle.Y, ClientSize.Width, 1)
                : new RectangleF(middle.Y, 0f, 1, ClientSize.Height));
        }
    }
    // ETO

    public class C_Array : Rhino.Commands.Command
    {
        public C_Array()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        public static C_Array Instance { get; private set; }

        public override string EnglishName => "JH_Array";

        public static Rhino.Commands.Result GetGem(Rhino.RhinoDoc doc, out Brep brep)
        {
            Rhino.Input.Custom.GetObject gemp = new Rhino.Input.Custom.GetObject();
            gemp.SetCommandPrompt("Get Gem");
            gemp.GeometryFilter = Rhino.DocObjects.ObjectType.Brep;
            gemp.GetMultiple(1, 1);
            gemp.EnablePreSelect(true, true);

            brep = null;

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above

                Rhino.Input.GetResult get_rc = gemp.Get();

                if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                else if (gemp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    foreach (var objref in gemp.Objects())
                    {
                        brep = objref.Brep();
                    }
                    if (brep.IsValid)
                    {
                        return Rhino.Commands.Result.Success;
                    }
                }
            }
            return Rhino.Commands.Result.Failure;
        }

        public static Rhino.Commands.Result GetBasePoint(Rhino.RhinoDoc doc, out Point3d basePoint)
        {
            Rhino.Input.Custom.GetPoint gemp = new Rhino.Input.Custom.GetPoint();
            gemp.SetCommandPrompt("Get Gem base point");

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above

                Rhino.Input.GetResult get_rc = gemp.Get();

                if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                else if (gemp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    basePoint = gemp.Point();
                    return Rhino.Commands.Result.Success;
                }
            }
            basePoint = new Point3d(0,0,0);
            return Rhino.Commands.Result.Failure;
        }

        public static Rhino.Commands.Result GetCurve(Rhino.RhinoDoc doc, out Curve curve)
        {
            Rhino.Input.Custom.GetObject gemp = new Rhino.Input.Custom.GetObject();
            gemp.SetCommandPrompt("Get Curve");
            gemp.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            gemp.GetMultiple(1, 1);
            gemp.EnablePreSelect(true, true);

            curve = null;

            while (true)
            {
                // perform the get operation. This will prompt the user to input a point, but also
                // allow for command line options defined above

                Rhino.Input.GetResult get_rc = gemp.Get();

                if (get_rc == Rhino.Input.GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Exit");
                    break;
                }
                else if (gemp.CommandResult() == Rhino.Commands.Result.Success)
                {
                    foreach (var objref in gemp.Objects())
                    {
                        curve = objref.Curve();
                    }
                    if (curve.IsValid)
                    {
                        return Rhino.Commands.Result.Success;
                    }
                }
            }
            return Rhino.Commands.Result.Failure;
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Define a unit Brep box
            Rhino.Geometry.Point3d min = new Rhino.Geometry.Point3d(0.0, 0.0, 0.0);
            Rhino.Geometry.Point3d max = new Rhino.Geometry.Point3d(1.0, 1.0, 1.0);
            Rhino.Geometry.BoundingBox bbox = new Rhino.Geometry.BoundingBox(min, max);
            Brep boxbrep = Rhino.Geometry.Brep.CreateFromBox(bbox);
            Brep brep = Rhino.Geometry.Brep.CreateFromBox(bbox);
            Point3d basePoint = new Point3d(0,0,0);
            Curve curve = null;

            // Create and define the arguments of the array
            SampleCsArrayArgs args = new SampleCsArrayArgs();

            if (GetGem(doc, out brep) != Result.Success)
            {
                return Result.Failure;
            }
            if (GetBasePoint(doc, out basePoint) != Result.Success)
            {
                return Result.Failure;
            }
            if (GetCurve(doc, out curve) != Result.Success)
            {
                return Result.Failure;
            }
            
            args.Gem = brep;
            args.basePoint = basePoint;
            curve.Domain = new Interval(0, 1);
            args.Curve = curve;
            args.DistanceX = 10;
            args.ScaleX = 1;
            args.CalculateOffsets();
            
            JHandsPlugin.Instance.BrepDisplay.Enabled = true;
            JHandsPlugin.Instance.PreviewArray = true;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

            Brep[] firstpreviewBreps = args.RunArray();
            JHandsPlugin.Instance.BrepDisplay.SetObjects(firstpreviewBreps);

            var form = new CoffeeDialog(args);
            var rc = form.ShowModal(RhinoEtoApp.MainWindow);
            
            JHandsPlugin.Instance.BrepDisplay.Enabled = false;

            if (rc == Result.Cancel)
            {
                return Result.Cancel;
            }

            Brep[] arrayBreps = args.RunArray();

            foreach(Brep arrayBrep in arrayBreps)
            {
                doc.Objects.AddBrep(arrayBrep);
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}