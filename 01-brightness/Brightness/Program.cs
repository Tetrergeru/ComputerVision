using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GraphFunc.Menus;

namespace GraphFunc
{
    public static class Program
    {
        public static Bitmap Scale(this Bitmap image, int width, int height)
        {
            var b = new Bitmap(width, height);
            var drawer = Graphics.FromImage(b);
            drawer.DrawImage(image, new Rectangle(0, 0, b.Width, b.Height));
            return b;
        }

        public static Bitmap DrawPlot(int[] data, Color color, int height)
        {
            var bitmap = new Bitmap(data.Length, height);
            var drawer = Graphics.FromImage(bitmap);
            double max = data.Max();
            for (var i = 0; i < data.Length; i++)
                drawer.DrawLine(new Pen(color), i, 0, i, (int) (height * (data[i] / max)));
            return bitmap;
        }

        [STAThread]
        static void Main(string[] args)
        {
            var form = new Form(new List<IMenu>
            {
                new ShadesOfGrayMenu(), 
                new ColorCorrection(),
                new InteractiveColorCorrection(), 
                new HistCorrection(),
            });
            Application.Run(form);
        }
    }
}