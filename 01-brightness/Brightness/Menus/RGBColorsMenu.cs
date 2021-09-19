using System.Drawing;
using System.Windows.Forms;

namespace GraphFunc.Menus
{
    public class RGBColorsMenu : IMenu
    {
        private static Color ExtractColor(int color, Color fullColor)
        {
            switch (color)
            {
                case 0:
                    return Color.FromArgb(fullColor.R, 0, 0);
                case 1:
                    return Color.FromArgb( 0,fullColor.G, 0);
                default:
                    return Color.FromArgb(0, 0, fullColor.B);
            }
        }

        private static Bitmap ExtractColor(Bitmap image, int color)
        {
            switch (color)
            {
                case 0:
                    return FastBitmap.Select(image,cl => (cl.r, 0, 0));
                case 1:
                    return FastBitmap.Select(image,cl => (0, cl.g, 0));
                default:
                    return FastBitmap.Select(image,cl => (0, 0, cl.b));
            }
        }

        private static int[] PlotColor(Bitmap image, int color)
        {
            var result = new int[256];
            
            switch (color)
            {
                case 0:
                    FastBitmap.Select(image, cl =>
                    {
                        result[cl.r]++;
                        return (0, 0, 0);
                    });
                    break;
                case 1:
                    FastBitmap.Select(image, cl => 
                    {
                        result[cl.g]++;
                        return (0, 0, 0);
                    });
                    break;
                default:
                    FastBitmap.Select(image, cl => 
                    {
                        result[cl.b]++;
                        return (0, 0, 0);
                    });
                    break;
            }
            
            return result;
        }

        private readonly PictureBox[] _colorImages = new PictureBox[3];

        private PictureBox cim;
        
        private readonly PictureBox[] _colorPlots = new PictureBox[3];

        public RGBColorsMenu()
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
                cim = colorImage;
                _colorImages[i] = colorImage;
                var colorPlot = new PictureBox()
                {
                    Width = 256,
                    Height = 256,
                    Top = 652,
                    Left = 50 + (256 + 50) * i,
                };
                _colorPlots[i] = colorPlot;
            }
        }

        public void Add(Form form)
        {
            foreach (var img in _colorImages)
                form.Controls.Add(img);
            foreach (var plot in _colorPlots)
                form.Controls.Add(plot);
            Update(form);
        }

        public void Update(Form form)
        {
            for (var i = 0; i < 3; i++)
                _colorImages[i].Image = ExtractColor(form.image, i).Scale(256, 256);
            for (var i = 0; i < 3; i++)
                _colorPlots[i].Image = Program.DrawPlot(PlotColor(form.image, i), ExtractColor(i, Color.White), 256);
        }

        public void Remove(Form form)
        {
            foreach (var img in _colorImages)
                form.Controls.Remove(img);
            foreach (var plot in _colorPlots)
                form.Controls.Remove(plot);
        }

        public string Name() => "RGB Colors";
    }
}