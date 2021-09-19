using System;
using System.Drawing;
using System.Windows.Forms;

namespace GraphFunc.Menus
{
    public class ShadesOfGrayMenu : IMenu
    {
        private static Bitmap GrayShade1(Bitmap b)
        {
            return FastBitmap.Select(b, cl =>
            {
                var br = (byte)Math.Ceiling(0.3 * cl.R + 0.59 * cl.G + 0.11 * cl.B);
                return Color.FromArgb(br, br, br);
            });
        }
        
        private static Bitmap GrayShade2(Bitmap b)
        {
            return FastBitmap.Select(b, cl =>
            {
                var br = (byte)Math.Ceiling(0.21 * cl.R + 0.72 * cl.G + 0.07 * cl.B);
                return Color.FromArgb(br, br, br);
            });
        }

        private static Bitmap GrayDiff(Bitmap b1, Bitmap b2)
        {
            var res = new Bitmap(b1.Width, b1.Height);

            using (var fb1 = new FastBitmap(b1))
            using (var fb2 = new FastBitmap(b2))
            using (var fb3 = new FastBitmap(res))
            {
                for (var i = 0; i < fb1.Count; i++)
                {
                    var diff = (byte) Math.Abs(fb1.GetI(i).R - fb2.GetI(i).R);
                    fb3.SetI(i, Color.FromArgb(diff, diff, diff));
                }
            }

            var (min, max) = ((byte) 255, (byte) 0);
            FastBitmap.ForEach(res, cl =>
            {
                if (cl.R > max)
                    max = cl.R;
                if (cl.R < min)
                    min = cl.R;
            });
            
            return FastBitmap.Select(res, cl =>
            {
                var t = (byte) ((cl.R - min) / (double)(max - min) * 255);
                return Color.FromArgb(t, t, t);
            });;
        }

        private static int[] CalcGrayIntensity(Bitmap image)
        {
            var result = new int[256];
            for (var i = 0; i < image.Width; i++)
            for (var j = 0; j < image.Height; j++)
                result[image.GetPixel(i, j).R]++;
            return result;
        }

        private readonly PictureBox _gray1, _gray2, _plot1, _plot2, _diff;
        
        public ShadesOfGrayMenu()
        {
            _gray1 = new PictureBox()
            {
                Width = 256,
                Height = 256,
                Top = 376,
                Left = 50,
            };
            _plot1 = new PictureBox()
            {
                Width = 256,
                Height = 256,
                Top = 652,
                Left = 50,
            };
            _gray2 = new PictureBox()
            {
                Width = 256,
                Height = 256,
                Top = 376,
                Left = 356,
            };
            _plot2 = new PictureBox()
            {
                Width = 256,
                Height = 256,
                Top = 652,
                Left = 356,
            };
            _diff = new PictureBox()
            {
                Width = 256,
                Height = 256,
                Top = 376,
                Left = 662,
            };
        }

        public void Add(Form form)
        {
            Console.WriteLine("Adding");
            form.Controls.Add(_gray1);
            form.Controls.Add(_gray2);
            form.Controls.Add(_plot1);
            form.Controls.Add(_plot2);
            form.Controls.Add(_diff);
            Update(form);
        }

        public void Update(Form form)
        {
            var g1 =  GrayShade1(form.image).Scale(256, 256);
            _gray1.Image = g1;
            _plot1.Image = Program.DrawPlot(CalcGrayIntensity(g1), Color.Gray, 256);
            var g2 =  GrayShade2(form.image).Scale(256, 256);
            _gray2.Image = g2;
            _plot2.Image = Program.DrawPlot(CalcGrayIntensity(g2), Color.Gray, 256);
            _diff.Image = GrayDiff(g1, g2).Scale(256, 256);
        }

        public void Remove(Form form)
        {
            form.Controls.Remove(_plot1);
            form.Controls.Remove(_plot2);
            form.Controls.Remove(_gray1);
            form.Controls.Remove(_gray2);
            form.Controls.Remove(_diff);
        }

        public string Name() => "Grays";
    }
}