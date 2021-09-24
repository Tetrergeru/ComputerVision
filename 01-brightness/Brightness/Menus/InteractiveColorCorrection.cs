using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace GraphFunc.Menus
{
    public class InteractiveColorCorrection : IMenu
    {
        private Form _form;
        private PictureBox[] _colorImages = new PictureBox[3];

        private List<Point> _linearInterpolation = new List<Point>();
        private List<Point> _splineInterpolationPoints = new List<Point>();

        private bool _linearActive = true;

        public InteractiveColorCorrection()
        {
            for (var i = 0; i < 3; i++)
            {
                var colorImage = new PictureBox()
                {
                    Width = 256,
                    Height = 256,
                    Top = 376,
                    Left = 50 + (256 + 50) * i,
                };
                _colorImages[i] = colorImage;
            }

            _colorImages[1].MouseClick += (sender, args) =>
            {
                _linearActive = true;
                _linearInterpolation = args.Button == MouseButtons.Right
                    ? _linearInterpolation.Where(p => Distance(p, args.Location) > 20).ToList()
                    : _linearInterpolation.Append(args.Location).OrderBy(p => p.X).ToList();
                DrawLinear(_form);
            };

            _colorImages[2].MouseClick += (sender, args) =>
            {
                _linearActive = false;
                _splineInterpolationPoints = args.Button == MouseButtons.Right
                    ? _splineInterpolationPoints.Where(p => Distance(p, args.Location) > 20).ToList()
                    : _splineInterpolationPoints.Append(args.Location).OrderBy(p => p.X).ToList();

                DrawSpline(_form);
            };
        }

        public string Name() => "Interactive";

        public void Add(Form form)
        {
            _form = form;
            foreach (var img in _colorImages)
                form.Controls.Add(img);
            Update(form);
        }

        public void Remove(Form form)
        {
            foreach (var img in _colorImages)
                form.Controls.Remove(img);
        }

        public void Update(Form form)
        {
            DrawLinear(form);
            DrawSpline(form);
        }

        private void DrawLinear(Form form)
        {
            _colorImages[1].Image = new Bitmap(_colorImages[1].Width, _colorImages[1].Height);
            var g = Graphics.FromImage(_colorImages[1].Image);
            var pen = new Pen(Color.Black, 2);
            g.Clear(Color.Orange);
            var p0 = new Point(0, _colorImages[1].Height);
            foreach (var p in _linearInterpolation)
            {
                g.DrawLine(pen, p0, p);
                p0 = p;
            }

            g.DrawLine(pen, p0, new Point(_colorImages[1].Width, 0));

            Func<double, double> func = LinearFunc;

            if (_linearActive)
                _colorImages[0].Image = form.image.Scale(_colorImages[1].Width, _colorImages[1].Height)
                    .Select(color => Color.FromArgb(
                            Program.ToByte(func(color.R / 256.0) * 256),
                            Program.ToByte(func(color.G / 256.0) * 256),
                            Program.ToByte(func(color.B / 256.0) * 256)
                        )
                    );
        }

        private void DrawSpline(Form form)
        {
            Console.WriteLine($">>{_splineInterpolationPoints.Count}");
            _colorImages[2].Image = new Bitmap(_colorImages[2].Width, _colorImages[2].Height);
            var points = new[] {new Point(0, _colorImages[2].Height)}
                .Concat(_splineInterpolationPoints)
                .Append(new Point(_colorImages[2].Width, 0))
                .ToList();
            var interpolation = MathNet.Numerics.Interpolate.CubicSpline(
                points.Select(p => (double) p.X),
                points.Select(p => (double) p.Y)
            );

            var g = Graphics.FromImage(_colorImages[2].Image);
            var pen = new Pen(Color.Black, 2);
            g.Clear(Color.Orange);

            var point0 = new Point(0, (int) interpolation.Interpolate(0));
            for (var i = 1; i < _colorImages[2].Image.Width; i++)
            {
                var point1 = new Point(i, (int) interpolation.Interpolate(i));
                g.DrawLine(pen, point0, point1);
                point0 = point1;
            }

            if (!_linearActive)
                _colorImages[0].Image = form.image.Scale(_colorImages[0].Width, _colorImages[0].Height)
                    .Select(color => Color.FromArgb(
                            Program.ToByte(256 - interpolation.Interpolate(color.R)),
                            Program.ToByte(256 - interpolation.Interpolate(color.G)),
                            Program.ToByte(256 - interpolation.Interpolate(color.B))
                        )
                    );
        }

        private double LinearFunc(double x)
        {
            var points = new[] {new Point(0, _colorImages[1].Height)}
                .Concat(_linearInterpolation)
                .Append(new Point(_colorImages[1].Width, 0))
                .ToList();
            var idx = points.FindIndex(p => p.X > x);

            var inter = InterpolateLine(
                points[idx - 1],
                points[idx],
                x * 256
            );
            return inter / 256;
        }

        private double InterpolateLine(Point a, Point b, double x)
            => (double) x * ((double) b.Y - a.Y) / -((double) b.X - a.X);

        private static double Distance(Point a, Point b)
            => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }
}