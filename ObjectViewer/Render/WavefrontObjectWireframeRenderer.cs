using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using ObjectViewer.Model;

namespace ObjectViewer.Render;

public abstract class WavefrontObjectWireframeRenderer : WavefrontObjectBaseRenderer
{
    public static void DrawWireframe(WavefrontObject wavefrontObject, WriteableBitmap writeableBitmap, Color color)
    {
        var bgra = ColorToIntBgra(color);

        writeableBitmap.Lock();

        unsafe
        {
            var pBackBuffer = (int*)writeableBitmap.BackBuffer;
            var width = writeableBitmap.PixelWidth;
            var height = writeableBitmap.PixelHeight;

            Parallel.ForEach(wavefrontObject.Faces, face =>
            {
                var count = face.Items.Count;

                if (count < 2) return;
                Parallel.For(0, count, i =>
                {
                    var i1 = face.Items[i].Vertex;
                    var i2 = face.Items[(i + 1) % count].Vertex;

                    if (i1 < 0 || i1 >= wavefrontObject.Vertices.Length ||
                        i2 < 0 || i2 >= wavefrontObject.Vertices.Length)
                    {
                        return;
                    }

                    var x0 = (int)Math.Round(wavefrontObject.Vertices[i1].Vector.X);
                    var y0 = (int)Math.Round(wavefrontObject.Vertices[i1].Vector.Y);
                    var x1 = (int)Math.Round(wavefrontObject.Vertices[i2].Vector.X);
                    var y1 = (int)Math.Round(wavefrontObject.Vertices[i2].Vector.Y);

                    DrawLine(pBackBuffer, width, height, x0, y0, x1, y1, bgra);
                });
            });
        }

        writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
        writeableBitmap.Unlock();
    }
}