using System;
using System.Collections.Generic;
using System.Drawing;
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
                if (args.Button == MouseButtons.Right)
                {
                    _linearInterpolation = _linearInterpolation.Where(p => Distance(p, args.Location) > 5).ToList();
                    DrawLinear(_form);
                }
                else
                {
                    _linearInterpolation = _linearInterpolation.Append(args.Location).OrderBy(p => p.X).ToList();
                    DrawLinear(_form);
                }
            };
            
            _colorImages[2].MouseClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Right)
                {
                    _splineInterpolationPoints = _splineInterpolationPoints.Where(p => Distance(p, args.Location) > 5).ToList();
                    DrawLinear(_form);
                }
                else
                {
                    _splineInterpolationPoints = _splineInterpolationPoints.Append(args.Location).OrderBy(p => p.X).ToList();
                    DrawSpline(_form);
                }
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
            _colorImages[0].Image = FastBitmap
                .Select(form.image.Scale(_colorImages[1].Width, _colorImages[1].Height), color => Color.FromArgb(
                        (byte) (func(color.R / 256.0) * 256),
                        (byte) (func(color.G / 256.0) * 256),
                        (byte) (func(color.B / 256.0) * 256)
                    )
                );
        }

        private void DrawSpline(Form form)
        {
            _colorImages[2].Image = new Bitmap(_colorImages[2].Width, _colorImages[2].Height);
            var g = Graphics.FromImage(_colorImages[2].Image);
            var pen = new Pen(Color.Black, 2);
            g.Clear(Color.Orange);
            var p0 = new Point(0, _colorImages[2].Height);
            foreach (var p in _splineInterpolationPoints)
            {
                g.DrawLine(pen, p0, p);
                p0 = p;
            }
            g.DrawLine(pen, p0, new Point(_colorImages[2].Width, 0));
        }

        private double LinearFunc(double x)
        {
            if (_linearInterpolation.Count == 0)
                return x;
            var idx = -1;
            for (var i = 0; i < _linearInterpolation.Count; i++)
            {
                if (_linearInterpolation[i].X > x)
                {
                    idx = i;
                    break;
                }
            }

            var inter =
                idx == -1
                    ? InterpolateLine(_linearInterpolation[_linearInterpolation.Count - 1],
                        new Point(_colorImages[1].Width, 0), x * 256)
                    : idx == 0
                        ? InterpolateLine(new Point(0, _colorImages[1].Height),
                            _linearInterpolation[0], x * 256)
                        : InterpolateLine(_linearInterpolation[idx - 1],
                            _linearInterpolation[idx], x * 256);
            return 1 - inter / 256;
        }

        private double InterpolateLine(Point a, Point b, double x)
            => (double) x * ((double) b.Y - a.Y) / ((double) b.X - a.X);

        private static double Distance(Point a, Point b)
            => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }
}