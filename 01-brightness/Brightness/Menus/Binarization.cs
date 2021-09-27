using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Windows.Forms;

namespace GraphFunc.Menus
{
    public class Binarization : IMenu
    {
        private Form _form;
        private readonly PictureBox[] _colorImages = new PictureBox[3];

        private readonly PictureBox _localOtsuBox;
        private Point? _localOtsuPoint;

        private readonly PictureBox _recursiveOtsuBox;

        private Bitmap _grayImage;
        private readonly HScrollBar _thresholdBar;

        public Binarization()
        {
            for (var i = 0; i < _colorImages.Length; i++)
            {
                var colorImage = new PictureBox
                {
                    Width = 256,
                    Height = 256,
                    Top = 376,
                    Left = 50 + (256 + 50) * i,
                };
                _colorImages[i] = colorImage;
            }

            _thresholdBar = new HScrollBar
            {
                Width = 256,
                Height = 15,
                Top = 376 + 256,
                Left = 50 + (256 + 50) * 1,
                Minimum = 20,
                Maximum = 110,
                SmallChange = 10,
                LargeChange = 10,
            };
            _thresholdBar.ValueChanged += (sender, args) => MakeSimpleBinarization(_form);

            _localOtsuBox = new PictureBox
            {
                Width = 256,
                Height = 256,
                Top = 376 + 256 + 20,
                Left = 50 + (256 + 50) * 1,
            };
            _localOtsuBox.MouseClick += (sender, args) =>
            {
                _localOtsuPoint = args.Button == MouseButtons.Right ? (Point?) null : args.Location;
                LocalOtsuBinarization(_form);
            };

            _recursiveOtsuBox = new PictureBox
            {
                Width = 256,
                Height = 256,
                Top = 376 + 256 + 20,
                Left = 50 + (256 + 50) * 2,
            };
        }

        public void Add(Form form)
        {
            _form = form;
            foreach (var img in _colorImages)
                form.Controls.Add(img);
            form.Controls.Add(_thresholdBar);
            form.Controls.Add(_localOtsuBox);
            form.Controls.Add(_recursiveOtsuBox);
            Update(form);
        }

        public void Update(Form form)
        {
            _form = form;
            MakeGrayScale(form);
            MakeSimpleBinarization(form);
            OtsuBinarization(form);
            LocalOtsuBinarization(form);
            HierarchicalOtsuBinarization(form);
        }

        private void MakeGrayScale(Form form)
        {
            _grayImage = form.image.Select(color =>
            {
                var avg = Program.ToByte((color.R + color.G + color.B) / 3.0);
                return Color.FromArgb(avg, avg, avg);
            });
            _colorImages[0].Image = _grayImage.Scale(_colorImages[0].Width, _colorImages[0].Height);
        }

        private void MakeSimpleBinarization(Form form)
        {
            var depth = _thresholdBar.Value / 10;
            _colorImages[1].Image =
                _grayImage.Select(color =>
                {
                    var avg = Program.ToByte(
                        Math.Round(
                            Math.Round(color.R / 256.0 * depth - 0.5) *
                            (256.0 / (depth - 1))
                        )
                    );
                    return Color.FromArgb(avg, avg, avg);
                }).Scale(_colorImages[1].Width, _colorImages[1].Height);
        }

        private void OtsuBinarization(Form form)
        {
            var histogram = Program.EmptyHist<int>();
            _grayImage.ForEach(color => { histogram[color.R] += 1; });
            var maxIdx = Otsu(histogram);

            Console.WriteLine($"Otsu thresh: {maxIdx}");

            _colorImages[2].Image =
                _grayImage.Select(color => color.R > maxIdx
                    ? Color.White
                    : Color.Black
                ).Scale(_colorImages[2].Width, _colorImages[2].Height);
        }

        private void LocalOtsuBinarization(Form form)
        {
            if (_localOtsuPoint == null)
            {
                _localOtsuBox.Image = _grayImage
                    .Scale(_localOtsuBox.Width, _localOtsuBox.Height);
                return;
            }

            var point = (Point) _localOtsuPoint;

            var hists = new[]
            {
                new[] {Program.EmptyHist<int>(), Program.EmptyHist<int>()},
                new[] {Program.EmptyHist<int>(), Program.EmptyHist<int>()}
            };

            _grayImage
                .Scale(_localOtsuBox.Width, _localOtsuBox.Height)
                .ForEach((color, x, y) => { hists[x > point.X ? 1 : 0][y > point.Y ? 1 : 0][color.R] += 1; });

            var thresholds = hists.Select(arr => arr.Select(Otsu).ToList()).ToList();

            _localOtsuBox.Image = _grayImage
                .Scale(_localOtsuBox.Width, _localOtsuBox.Height)
                .Select((color, x, y) =>
                    color.R > thresholds[x > point.X ? 1 : 0][y > point.Y ? 1 : 0]
                        ? Color.White
                        : Color.Black
                );

            var g = Graphics.FromImage(_localOtsuBox.Image);
            g.DrawEllipse(new Pen(Color.Red), point.X - 2, point.Y - 2, 5, 5);
        }

        private void HierarchicalOtsuBinarization(Form form)
        {
            var histogram = Program.EmptyHist<int>();
            _grayImage.ForEach(color => { histogram[color.R] += 1; });

            var thresholds = RecursiveOtsu(histogram).ToList();
            thresholds.Add(255);

            Console.WriteLine(string.Join(", ", thresholds));

            _recursiveOtsuBox.Image =
                _grayImage.Select(color =>
                    {
                        var idx = thresholds.FindIndex(x => x >= color.R);
                        var avg = Program.ToByte(idx * (256.0 / (thresholds.Count - 1)));
                        return Color.FromArgb(avg, avg, avg);
                    }
                ).Scale(_recursiveOtsuBox.Width, _recursiveOtsuBox.Height);
        }

        public void Remove(Form form)
        {
            foreach (var img in _colorImages)
                form.Controls.Remove(img);
            form.Controls.Remove(_thresholdBar);
            form.Controls.Remove(_localOtsuBox);
            form.Controls.Remove(_recursiveOtsuBox);
        }

        private IEnumerable<int> RecursiveOtsu(List<int> histogram)
        {
            var (threshold, sigma) = OtsuSigma(histogram);
            if (sigma < 100)
            {
                Console.WriteLine($"sigma: {sigma}");
                return new int[] { };
            }

            var left = RecursiveOtsu(histogram.Take(threshold).ToList());
            var right = RecursiveOtsu(histogram.Skip(threshold).ToList());
            return left.Append(threshold).Concat(right.Select(x => x + threshold));
        }

        private int Otsu(List<int> histogram)
        {
            var (res, _) = OtsuSigma(histogram);
            return res;
        }

        private (int, double) OtsuSigma(List<int> histogram)
        {
            var n = (double) histogram.Sum();
            var m = (double) histogram.Select((x, i) => x * i).Sum();

            var min = histogram.FindIndex(x => x != 0);
            var max = histogram.FindLastIndex(x => x != 0);

            if (min == max)
                return (-1, -1);

            var maxSigma = -1.0;
            var maxIdx = -1;
            var alpha = 0.0;
            var beta = 0.0;
            for (var t = min; t <= max; t++)
            {
                alpha += t * histogram[t];
                beta += histogram[t];
                var w1 = beta / n;
                var a = alpha / beta - (m - alpha) / (n - beta);
                var sigma = w1 * (1 - w1) * a * a;
                if (sigma > maxSigma)
                {
                    maxSigma = sigma;
                    maxIdx = t;
                }
            }

            return (maxIdx, maxSigma);
        }

        public string Name()
            => "Binarization";
    }
}