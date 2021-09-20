using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GraphFunc.Menus
{
    public class HistCorrection : IMenu
    {
        private Form _form;
        private readonly PictureBox[] _colorImages = new PictureBox[3];

        private Bitmap _grayImage;

        private static List<int> EmptyHist => new bool[256].Select(x => 0).ToList();

        private List<int> _histogram = EmptyHist;

        public HistCorrection()
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
        }

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
            EvalHistograms(form);

            Normalization(form);
            Equalization(form);
        }

        private void EvalHistograms(Form form)
        {
            _grayImage = FastBitmap
                .Select(form.image.Scale(_colorImages[0].Width, _colorImages[0].Height), color =>
                {
                    var avg = Program.ToByte((color.R + color.G + color.B) / 3.0);
                    return Color.FromArgb(avg, avg, avg);
                });
            _colorImages[0].Image = _grayImage;
            _histogram = EmptyHist;
            FastBitmap.ForEach(_grayImage, color => { _histogram[color.R] += 1; });
        }

        private void Normalization(Form form)
        {
            var lut = Normalize(_histogram);
            _colorImages[1].Image = FastBitmap
                .Select(_grayImage.Scale(_colorImages[0].Width, _colorImages[0].Height), color => Color.FromArgb(
                        Program.ToByte(lut[color.R]),
                        Program.ToByte(lut[color.G]),
                        Program.ToByte(lut[color.B])
                    )
                );
        }

        private List<int> Normalize(List<int> hist)
        {
            var result = EmptyHist;
            var left = hist.FindIndex(x => x != 0);
            var right = hist.FindLastIndex(x => x != 0) + 1;
            var step = 256.0 / (right - left);
            for (var i = 0; i < right - left; i++)
                result[left + i] = Program.ToByte(step * i);
            return result;
        }

        private void Equalization(Form form)
        {
            var lut = Equalize(_histogram);
            _colorImages[2].Image = FastBitmap
                .Select(_grayImage.Scale(_colorImages[2].Width, _colorImages[2].Height), color => Color.FromArgb(
                        Program.ToByte(lut[color.R]),
                        Program.ToByte(lut[color.G]),
                        Program.ToByte(lut[color.B])
                    )
                );
        }

        private List<int> Equalize(List<int> hist)
        {
            var q = (double) hist.Sum() / hist.Count;
            var lut = EmptyHist;
            var sum = 0.0;
            var idx = 0;
            foreach (var (v, i) in hist.Select((x, i) => (x, i)).Where(x => x.x != 0))
            {
                while (sum > v)
                {
                    idx++;
                    sum -= q;
                }

                sum += v;
                lut[i] = idx;
            }

            return lut;
        }

        public string Name() => "Hist correction";
    }
}