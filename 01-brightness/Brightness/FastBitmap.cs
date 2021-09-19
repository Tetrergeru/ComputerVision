using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;

namespace GraphFunc
{
    public unsafe class FastBitmap : System.IDisposable
    {
        private readonly Bitmap _source;

        private readonly BitmapData _bData;

        private readonly byte* _scan0;
        
        public int Count => _source.Height * _source.Width;

        public (byte r, byte g, byte b) GetI(int i)
        {
            var data = _scan0 + i * 4;
            return (data[0], data[1], data[2]);
        }

        public void SetI(int i, (byte r, byte g, byte b) cl)
        {
            var data = _scan0 + i * 4;
            (data[0], data[1], data[2]) = cl;
            data[3] = 255;
        }

        public FastBitmap(Bitmap bitmap)
        {
            _source = bitmap;
            _bData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            _scan0 = (byte*) _bData.Scan0.ToPointer();
        }
        
        public void Dispose()
        {
            _source.UnlockBits(_bData);
        }

        public static void ForEach(Bitmap source, Action<(byte r, byte g, byte b)> action)
        {
            var bitmap = source.Scale(source.Width, source.Height);
            var length = bitmap.Height * bitmap.Width * 4;
            var bData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite,
                bitmap.PixelFormat);

            var scan0 = (byte*) bData.Scan0.ToPointer();
            for (var i = 0; i < length; i += 4)
            {
                var data = scan0 + i;
                action((data[0], data[1], data[2]));
            }

            bitmap.UnlockBits(bData);
        }

        public static Bitmap Select(Bitmap source, Func<(byte r, byte g, byte b), (byte r, byte g, byte b)> transform)
        {
            var bitmap = source.Scale(source.Width, source.Height);
            var length = bitmap.Height * bitmap.Width * 4;
            var bData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite,
                bitmap.PixelFormat);

            var scan0 = (byte*) bData.Scan0.ToPointer();
            for (var i = 0; i < length; i += 4)
            {
                var data = scan0 + i;
                (data[0], data[1], data[2]) = transform((data[0], data[1], data[2]));
            }

            bitmap.UnlockBits(bData);
            return bitmap;
        }
    }
}