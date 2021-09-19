using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace GraphFunc
{
    public unsafe class FastBitmap : System.IDisposable
    {
        private readonly Bitmap _source;

        public readonly int Width;

        public readonly int Height;

        private readonly int _bytesPerPixel;

        private readonly BitmapData _bData;

        private readonly byte* _scan0;

        public FastBitmap(Bitmap bitmap)
        {
            Width = bitmap.Width;
            Height = bitmap.Height;
            _source = bitmap;
            _bData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                bitmap.PixelFormat
            );
            _bytesPerPixel = _bData.Stride / _bData.Width;
            _scan0 = (byte*) _bData.Scan0.ToPointer();
        }

        public int Count => _source.Height * _source.Width;

        public Color GetI(int i)
        {
            var data = _scan0 + i * _bytesPerPixel;
            return Color.FromArgb(data[2], data[1], data[0]);
        }

        public void SetI(int i, Color cl)
        {
            var data = _scan0 + i * _bytesPerPixel;
            (data[2], data[1], data[0]) = (cl.R, cl.G, cl.B);
            data[3] = 255;
        }

        public void SetPixel(Point p, Color cl)
            => SetI(p.X + p.Y * Width, cl);

        public Color GetPixel(Point p)
            => GetI(p.X + p.Y * Width);
        
        public Color this[int x, int y]
        {
            get => GetPixel(new Point(x, y));
            set => SetPixel(new Point(x, y), value);
        }

        public void Dispose()
        {
            _source.UnlockBits(_bData);
        }

        public static void ForEach(Bitmap source, Action<Color> action)
        {
            var next = source.Scale(source.Width, source.Height);
            using (var bitmap = new FastBitmap(next))
            {
                for (var i = 0; i < bitmap.Count; i += 1)
                {
                    action(bitmap.GetI(i));
                }
            }
        }

        public static Bitmap Select(Bitmap source, Func<Color, Color> transform)
        {
            var next = source.Scale(source.Width, source.Height);
            using (var bitmap = new FastBitmap(next))
            {
                for (var i = 0; i < bitmap.Count; i += 1)
                {
                    bitmap.SetI(i, transform(bitmap.GetI(i)));
                }
            }
            return next;
        }
    }
}