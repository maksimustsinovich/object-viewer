using System.Drawing;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;
using ObjectViewer.Model;
using ObjectViewer.Transformation;

namespace ObjectViewer.Render;

public abstract class WavefrontObjectBaseRenderer
{
    public static WavefrontObject UpdateWavefrontObject(
    WavefrontObject wavefrontObject,
    Vector3 translation, Vector3 rotation, float scale,
    Vector3 eye, Vector3 target, Vector3 up,
    float zNear, float zFar,
    float width, float height)
    {
        var newWavefrontObject = new WavefrontObject
        {
            VertexTextures = wavefrontObject.VertexTextures,
            Faces = wavefrontObject.Faces
        };

        var worldMatrix = MatrixTransformation.CreateWorldMatrix(translation, rotation, scale);
        var viewMatrix = MatrixTransformation.CreateViewMatrix(eye, target, up);
        var projectionMatrix = MatrixTransformation.CreateProjectionMatrix(width, height, zNear, zFar);
        var viewportMatrix = MatrixTransformation.CreateViewportMatrix(width, height);

        var finalMatrix = worldMatrix * viewMatrix * projectionMatrix * viewportMatrix;

        if (!Matrix4x4.Invert(worldMatrix * viewMatrix, out var invertedWorldMatrix))
        {
            throw new InvalidOperationException("Matrix inversion failed for normal matrix calculation.");
        }
        var normalMatrix = Matrix4x4.Transpose(invertedWorldMatrix);

        var oldVertices = wavefrontObject.Vertices;
        var newVertices = new Vertex[oldVertices.Length];

        var worldCords = new Vector3[oldVertices.Length];
        for (var i = 0; i < oldVertices.Length; i++)
        {
            var v = Vector4.Transform(oldVertices[i].Vector, finalMatrix);
            v = v / v.W;
            newVertices[i] = new Vertex(v);

            worldCords[i] = Vector4.Transform(oldVertices[i].Vector, worldMatrix).AsVector3();
        }
        newWavefrontObject.Vertices = newVertices;
        newWavefrontObject.WorldCords = worldCords;

        var oldNormals = wavefrontObject.VertexNormals;
        var newNormals = new VertexNormal[oldNormals.Length];
        for (var i = 0; i < oldNormals.Length; i++)
        {
            var normal = oldNormals[i].Vector;

            var transformedNormal = Vector3.Transform(normal, normalMatrix);

            transformedNormal = Vector3.Normalize(transformedNormal);

            newNormals[i] = new VertexNormal(transformedNormal);
        }
        newWavefrontObject.VertexNormals = newNormals;

        return newWavefrontObject;
    }

    public static void DrawBackground(WriteableBitmap bitmap, Color backgroundColor, Color gridColor)
    {
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;

        ClearBitmap(bitmap, backgroundColor);

        const int gridSize = 20;

        unsafe
        {
            bitmap.Lock();
            try
            {
                var buffer = (int*)bitmap.BackBuffer;

                for (var x = 0; x < width; x += gridSize)
                {
                    DrawLine(buffer, width, height, x, 0, x, height, ColorToIntBgra(gridColor));
                }

                for (var y = 0; y < height; y += gridSize)
                {
                    DrawLine(buffer, width, height, 0, y, width, y, ColorToIntBgra(gridColor));
                }
            }
            finally
            {
                bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                bitmap.Unlock();
            }
        }
    }

    protected static unsafe void DrawLine(
        int* buffer, int width, int height,
        int x0, int y0, int x1, int y1, int color)
    {
        var r = (byte)((color >> 16) & 0xFF);
        var g = (byte)((color >> 8) & 0xFF);
        var b = (byte)(color & 0xFF);
        var a = (byte)((color >> 24) & 0xFF);

        var steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
        if (steep)
        {
            (x0, y0) = (y0, x0);
            (x1, y1) = (y1, x1);
        }

        if (x0 > x1)
        {
            (x0, x1) = (x1, x0);
            (y0, y1) = (y1, y0);
        }

        float dx = x1 - x0;
        float dy = y1 - y0;
        var gradient = (dx == 0) ? 1.0f : dy / dx;

        var xEnd = MathF.Round(x0);
        var yEnd = y0 + gradient * (xEnd - x0);

        var xPixel1 = (int)xEnd;
        var yPixel1 = (int)yEnd;

        if (steep)
        {
            Plot(buffer, width, height, yPixel1, xPixel1, r, g, b, a, 1.0f - (yEnd - yPixel1));
            Plot(buffer, width, height, yPixel1 + 1, xPixel1, r, g, b, a, yEnd - yPixel1);
        }
        else
        {
            Plot(buffer, width, height, xPixel1, yPixel1, r, g, b, a, 1.0f - (yEnd - yPixel1));
            Plot(buffer, width, height, xPixel1, yPixel1 + 1, r, g, b, a, yEnd - yPixel1);
        }

        var interY = yEnd + gradient;

        xEnd = MathF.Round(x1);
        yEnd = y1 + gradient * (xEnd - x1);

        var xPixel2 = (int)xEnd;
        var yPixel2 = (int)yEnd;

        if (steep)
        {
            Plot(buffer, width, height, yPixel2, xPixel2, r, g, b, a, 1.0f - (yEnd - yPixel2));
            Plot(buffer, width, height, yPixel2 + 1, xPixel2, r, g, b, a, yEnd - yPixel2);
        }
        else
        {
            Plot(buffer, width, height, xPixel2, yPixel2, r, g, b, a, 1.0f - (yEnd - yPixel2));
            Plot(buffer, width, height, xPixel2, yPixel2 + 1, r, g, b, a, yEnd - yPixel2);
        }

        for (var x = xPixel1 + 1; x < xPixel2; x++)
        {
            if (steep)
            {
                Plot(buffer, width, height, (int)interY, x, r, g, b, a, 1.0f - (interY - (int)interY));
                Plot(buffer, width, height, (int)interY + 1, x, r, g, b, a, interY - (int)interY);
            }
            else
            {
                Plot(buffer, width, height, x, (int)interY, r, g, b, a, 1.0f - (interY - (int)interY));
                Plot(buffer, width, height, x, (int)interY + 1, r, g, b, a, interY - (int)interY);
            }

            interY += gradient;
        }
    }

    private static unsafe void Plot(int* buffer, int width, int height, int x, int y, byte r, byte g, byte b, byte a,
        float intensity)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        var index = y * width + x;
        var pixel = (byte*)(buffer + index);

        var dstB = pixel[0];
        var dstG = pixel[1];
        var dstR = pixel[2];
        var dstA = pixel[3];

        var dstRf = dstR / 255.0f;
        var dstGf = dstG / 255.0f;
        var dstBf = dstB / 255.0f;
        var dstAf = dstA / 255.0f;

        var srcRf = r / 255.0f;
        var srcGf = g / 255.0f;
        var srcBf = b / 255.0f;
        var srcAf = a / 255.0f;

        var outR = srcRf * intensity + (1 - intensity) * dstRf;
        var outG = srcGf * intensity + (1 - intensity) * dstGf;
        var outB = srcBf * intensity + (1 - intensity) * dstBf;
        var outA = srcAf * intensity + (1 - intensity) * dstAf;

        var newR = (byte)(outR * 255);
        var newG = (byte)(outG * 255);
        var newB = (byte)(outB * 255);
        var newA = (byte)(outA * 255);

        pixel[0] = newB;
        pixel[1] = newG;
        pixel[2] = newR;
        pixel[3] = newA;
    }

    protected static unsafe void Plot(int* buffer, int width, int height, int x, int y, int color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        var index = y * width + x;
        buffer[index] = color;
    }

    protected static int ColorToIntBgra(Color color)
    {
        return (color.B << 0) | (color.G << 8) | (color.R << 16) | (color.A << 24);
    }

    protected static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }

    private static void ClearBitmap(WriteableBitmap writeableBitmap, Color color)
    {
        var intColor = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

        writeableBitmap.Lock();

        try
        {
            unsafe
            {
                var pBackBuffer = (int*)writeableBitmap.BackBuffer;

                for (var i = 0; i < writeableBitmap.PixelHeight; i++)
                {
                    for (var j = 0; j < writeableBitmap.PixelWidth; j++)
                    {
                        *pBackBuffer++ = intColor;
                    }
                }
            }

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
        }
        finally
        {
            writeableBitmap.Unlock();
        }
    }
}