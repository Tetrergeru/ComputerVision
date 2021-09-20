using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GraphFunc.Menus
{
    public class ColorCorrection : IMenu
    {
        private Form _form;
        private readonly PictureBox[] _colorImages = new PictureBox[3];

        private Point? _correctionCoordinates;
        private Color _correctionColor = Color.White;
        private readonly Button _colorButton;

        private readonly TextBox _funcBox;
        private readonly Button _funcApply;

        public ColorCorrection()
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

            _colorImages[0].MouseClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Right)
                    _correctionCoordinates = null;
                else
                    _correctionCoordinates = args.Location;
                Update(_form);
            };
            _colorButton = new Button()
            {
                Width = 256,
                Top = 376 + 256 + 10,
                Left = 50,
                BackColor = _correctionColor,
            };
            _colorButton.Click += (sender, args) =>
            {
                var dialog = new ColorDialog
                {
                    AllowFullOpen = true,
                    ShowHelp = true,
                    Color = _correctionColor
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _correctionColor = dialog.Color;
                    _colorButton.BackColor = _correctionColor;
                }

                Update(_form);
            };
            _funcBox = new TextBox
            {
                Width = 200,
                Top = 376 + 256 + 10,
                Left = 356 + 306,
                Text = "log(x)",
            };
            _funcApply = new Button
            {
                Width = 56,
                Top = 376 + 256 + 10,
                Left = 556 + 306,
                Text = "Apply",
            };
            _funcApply.Click += (sender, args) => { CorrectFunc(_form); };
        }

        public void Add(Form form)
        {
            _form = form;
            foreach (var img in _colorImages)
                form.Controls.Add(img);
            form.Controls.Add(_colorButton);
            form.Controls.Add(_funcApply);
            form.Controls.Add(_funcBox);
            Update(form);
        }

        public void Remove(Form form)
        {
            foreach (var img in _colorImages)
                form.Controls.Remove(img);
            form.Controls.Remove(_colorButton);
            form.Controls.Remove(_funcApply);
            form.Controls.Remove(_funcBox);
        }

        public void Update(Form form)
        {
            CorrectWithExample(form);
            GrayWorld(form);
            CorrectFunc(form);
        }

        private void CorrectWithExample(Form form)
        {
            var src = form.image.Scale(_colorImages[0].Width, _colorImages[0].Height);

            if (_correctionCoordinates == null)
            {
                _colorImages[0].Image = src;
                return;
            }

            var c = (Point) _correctionCoordinates;

            var dstColor = _colorButton.BackColor;
            var srcColor = src.GetPixel(c.X, c.Y);
            _colorImages[0].Image = FastBitmap
                .Select(form.image, color => Color.FromArgb(
                        Program.ToByte( (double) color.R / dstColor.R * srcColor.R),
                        Program.ToByte( (double) color.G / dstColor.G * srcColor.G),
                        Program.ToByte( (double) color.B / dstColor.B * srcColor.B)
                    )
                ).Scale(_colorImages[0].Width, _colorImages[0].Height);

            var g = Graphics.FromImage(_colorImages[0].Image);
            g.DrawEllipse(new Pen(Color.Red), c.X - 2, c.Y - 2, 4, 4);
        }

        private void GrayWorld(Form form)
        {
            var sum = (R: 0, G: 0, B: 0);
            FastBitmap.ForEach(form.image, color =>
            {
                sum.R += color.R;
                sum.G += color.G;
                sum.B += color.B;
            });

            var pixelsTotal = form.image.Width * form.image.Height;
            var avg = ((double) sum.R + sum.G + sum.B) / (pixelsTotal * 3);
            var avgColors = (
                R: (double) sum.R / pixelsTotal,
                G: (double) sum.G / pixelsTotal,
                B: (double) sum.B / pixelsTotal
            );
            _colorImages[1].Image = FastBitmap
                .Select(form.image, color => Color.FromArgb(
                        Program.ToByte( color.R * avg / avgColors.R),
                        Program.ToByte( color.G * avg / avgColors.G),
                        Program.ToByte( color.B * avg / avgColors.B)
                    )
                ).Scale(_colorImages[1].Width, _colorImages[1].Height);
        }

        private void CorrectFunc(Form form)
        {
            var func = GetFunc(_funcBox.Text);
            _colorImages[2].Image = FastBitmap
                .Select(form.image.Scale(_colorImages[1].Width, _colorImages[1].Height), color => Color.FromArgb(
                        Program.ToByte( func(color.R / 256.0) * 256),
                        Program.ToByte( func(color.G / 256.0) * 256),
                        Program.ToByte( func(color.B / 256.0) * 256)
                    )
                );
            Console.WriteLine("Func done");
        }

        public string Name() => "Color correction";

        private Func<double, double> GetFunc(string func)
        {
            var parser = new FuncParser(func);
            try
            {
                return parser.Parse();
            }
            catch
            {
                Console.WriteLine("Error compiling");
                return x => x;
            }
        }
    }
}