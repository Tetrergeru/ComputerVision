using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GraphFunc
{
    public static class Utils
    {
        public static byte ToByte(double v)
        {
            var i = (int) v;
            if (i > 255)
                return 255;
            if (i < 0)
                return 0;
            return (byte) i;
        }

        public static Bitmap Scale(this Bitmap image, int width, int height)
        {
            var b = new Bitmap(width, height);
            var drawer = Graphics.FromImage(b);
            drawer.DrawImage(image, new Rectangle(0, 0, b.Width, b.Height));
            return b;
        }

        public static Bitmap DrawPlot(IReadOnlyList<int> data, Color color, int height)
        {
            var bitmap = new Bitmap(data.Count, height);
            var drawer = Graphics.FromImage(bitmap);
            double max = data.Max();
            drawer.Clear(Color.Wheat);
            for (var i = 0; i < data.Count; i++)
                drawer.DrawLine(new Pen(color), i, 0, i, (int) (height * (data[i] / max)));
            return bitmap;
        }
    }
}