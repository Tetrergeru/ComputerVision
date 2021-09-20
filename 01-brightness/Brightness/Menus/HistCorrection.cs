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
        private readonly PictureBox[] _colorImages = new PictureBox[2];

        private List<double[]> _histograms = new bool[256]
            .Select(x => new double[3])
            .ToList();

        public HistCorrection()
        {
            for (var i = 0; i < 2; i++)
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
            Normalization(form);
            Equalization(form);
        }

        private void EvalHistograms(Form form)
        {
            FastBitmap.ForEach(form.image, color =>
            {
                _histograms[color.R][0] += 1;
                _histograms[color.G][1] += 1;
                _histograms[color.B][2] += 1;
            });
        }

        private void Normalization(Form form)
        {
            _colorImages[0].Image = form.image.Scale(_colorImages[0].Width, _colorImages[0].Height);
        }

        private void Equalization(Form form)
        {
            _colorImages[1].Image = form.image.Scale(_colorImages[1].Width, _colorImages[1].Height);
        }

        public string Name() => "Hist correction";
    }
}