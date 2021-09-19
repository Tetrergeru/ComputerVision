using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Web;

namespace GraphFunc.Menus
{
    public class RGBHSVMenu : IMenu
    {
        private PictureBox ResultPicture;

        private NumericUpDown[] ParamControllers;

        private Button UpdateButton;

        private Label[] NameLabels;

        private static Bitmap Adjust(Bitmap image, decimal h, decimal s, decimal v)
        {
            return FastBitmap.Select(image, cl =>
            {
                var newCl = Change_HSV(cl, h, s, v);
                return (newCl.R, newCl.G, newCl.B);
            });
        }

        private static Color Change_HSV((byte R, byte G, byte B) c, decimal h, decimal s, decimal v)
        {
            double R = (double)(c.R) / 255;
            double B = (double)(c.B) / 255;
            double G = (double)(c.G) / 255;

            double H, S, V, MAX, MIN;
            //
            MAX = Math.Max(Math.Max(R, G), B);
            MIN = Math.Min(Math.Min(R, G), B);
            V = MAX;

            if (MAX == 0)
                S = 0;
            else
                S = 1 - MIN / MAX;

            if (MAX == R && G >= B)
                H = 60 * (G - B) / (MAX - MIN);
            else
                if (MAX == R && G < B)
                    H = 60 * (G - B) / (MAX - MIN) + 360;
            else
                if (MAX == G)
                    H = 60 * (B - R) / (MAX - MIN) + 120;
            else
                if (MAX == B)
                    H = 60 * (R - G) / (MAX - MIN) + 240;
            else
                H = 0;

            //
            H += (double)h;
            if (H < 0)
                H += 360;
            if (H > 360)
                H -= 360;
            S += (double)s;
            if (S < 0)
                S += 1;
            if (S > 1)
                S -= 1;
            V += (double)v;
            if (V < 0)
                V += 1;
            if (V > 1)
                V -= 1;

            //
            double Hi = Math.Floor(H / 60) % 6;
            double f = H / 60 - Math.Floor(H / 60);
            double p = V * (1 - S);
            double q = V * (1 - f * S);
            double t = V * (1 - (1 - f) * S);
            switch (Hi)
            {
                case 0:
                    R = V; G = t; B = p;
                    break;
                case 1.0:
                    R = q; G = V; B = p;
                    break;
                case 2.0:
                    R = p; G = V; B = t;
                    break;
                case 3.0:
                    R = p; G = q; B = V;
                    break;
                case 4.0:
                    R = t; G = t; B = V;
                    break;
                case 5.0:
                    R = V; G = p; B = q;
                    break;
                default:
                    break;
            }

            return Color.FromArgb((int)(255 * R), (int)(255 * G), (int)(255 * B));
        }

        public RGBHSVMenu()
        {
            ParamControllers = new NumericUpDown[3];
            NameLabels = new Label[3];
            var res = new PictureBox()
            {
                Width = 256,
                Height = 256,
                Top = 380,
                Left = 300,
            };
            ResultPicture = res;

            for (var i = 0; i < 3; i++)
            {
                string str = "";
                switch (i)
                {
                    case 0: 
                        str = "H";
                        break;
                    case 1:
                        str = "S";
                        break;
                    case 2:
                        str = "V";
                        break;
                    default:
                        break;
                }
                var text = new NumericUpDown()
                {
                    Value = 0,
                    Width = 60,
                    Left = 50,
                    Top = 380 + i * 40,
                    DecimalPlaces = 3,
                    Increment = 0.001M,
                    Minimum =  -1,
                    Maximum = 1,
                };
                var name = new Label()
                {
                    Text = str,
                    Width = 25,
                    Left = 20,
                    Top = 380 + i * 40,
                };
                ParamControllers[i] = text;
                NameLabels[i] = name;
            }
            ParamControllers[0].Maximum = 360;
            ParamControllers[0].Minimum = -360;
            ParamControllers[0].Increment = 1;
            ParamControllers[0].Value = 0;
            var but = new Button()
            {
                Text = "Update",
                Width = 100,
                Left = 150,
                Top = 420,
            };
            UpdateButton = but;

        }

        public void Add(Form form)
        {
            form.Controls.Add(ResultPicture);

            for (var i = 0; i < 3; i++)
            {
                form.Controls.Add(ParamControllers[i]);
                form.Controls.Add(NameLabels[i]);
            }
            UpdateButton.Click += (o, e) =>
            {
                Update(form);
            };
            form.Controls.Add(UpdateButton);
            Update(form);
        }

        public void Update(Form form)
        {
            ResultPicture.Image = Adjust(form.image, ParamControllers[0].Value, ParamControllers[1].Value, ParamControllers[2].Value).Scale(256, 256);;
            ResultPicture.Image.Save("result1.png");
            Console.WriteLine("Yoosh!");
        }

        public void Remove(Form form)
        {
            form.Controls.Remove(ResultPicture);
            for (var i = 0; i < 3; i++)
            {
                form.Controls.Remove(ParamControllers[i]);
                form.Controls.Remove(NameLabels[i]);
            }
            form.Controls.Remove(UpdateButton);
        }

        public string Name() => "RGB/HSV";
    }
}