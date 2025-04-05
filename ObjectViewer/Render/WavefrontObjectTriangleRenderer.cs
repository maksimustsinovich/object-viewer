using System.Drawing;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;
using ObjectViewer.Model;

namespace ObjectViewer.Render;

public abstract class WavefrontObjectTriangleRenderer : WavefrontObjectBaseRenderer
{
    public static void DrawFilledTriangles(WavefrontObject wavefrontObject, WriteableBitmap writeableBitmap,
        Color color)
    {
        var lightDirection = Vector3.Normalize(new Vector3(0, 0, -1));

        var width = writeableBitmap.PixelWidth;
        var height = writeableBitmap.PixelHeight;

        var zBuffer = CreateZBuffer(width, height);
        
        writeableBitmap.Lock();
        unsafe
        {
            var pBackBuffer = (int*)writeableBitmap.BackBuffer;
            
            Parallel.ForEach(wavefrontObject.Faces, face =>
                {
                    var count = face.Items.Count;

                    if (count < 3) return;

                    for (var i = 0; i < count; i++)
                    {
                        var i1 = face.Items[i].Vertex;
                        var i2 = face.Items[(i + 1) % count].Vertex;
                        var i3 = face.Items[(i + 2) % count].Vertex;

                        if (i1 < 0 || i1 >= wavefrontObject.Vertices.Length ||
                            i2 < 0 || i2 >= wavefrontObject.Vertices.Length ||
                            i3 < 0 || i3 >= wavefrontObject.Vertices.Length)
                        {
                            continue;
                        }

                        var v1 = wavefrontObject.Vertices[i1];
                        var v2 = wavefrontObject.Vertices[i2];
                        var v3 = wavefrontObject.Vertices[i3];

                        var hasNormals = face.Items[i].VertexNormal >= 0 &&
                                         wavefrontObject.VertexNormals.Length > face.Items[i].VertexNormal;

                        Vector3 normal;
                        if (hasNormals)
                        {
                            var n1 = wavefrontObject.VertexNormals[face.Items[i].VertexNormal].Vector;
                            var n2 = wavefrontObject.VertexNormals[face.Items[(i + 1) % count].VertexNormal].Vector;
                            var n3 = wavefrontObject.VertexNormals[face.Items[(i + 2) % count].VertexNormal].Vector;

                            normal = (n1 + n2 + n3) / 3;
                        }
                        else
                        {
                            normal = ComputeNormal(
                                new Vector3(v1.Vector.X, v1.Vector.Y, v1.Vector.Z),
                                new Vector3(v2.Vector.X, v2.Vector.Y, v2.Vector.Z),
                                new Vector3(v3.Vector.X, v3.Vector.Y, v3.Vector.Z));
                        }

                        normal = Vector3.Normalize(normal);

                        var viewDirection = new Vector3(0, 0, -1);
                        if (!IsVisible(normal, viewDirection))
                        {
                            continue;
                        }

                        var intensity = Math.Max(0, Vector3.Dot(normal, -lightDirection));

                        var shadedColor = ApplyIntensity(color, intensity);

                        var bgra = ColorToIntBgra(shadedColor);

                        var x1 = (int)Math.Round(v1.Vector.X);
                        var y1 = (int)Math.Round(v1.Vector.Y);
                        var z1 = v1.Vector.Z;

                        var x2 = (int)Math.Round(v2.Vector.X);
                        var y2 = (int)Math.Round(v2.Vector.Y);
                        var z2 = v2.Vector.Z;

                        var x3 = (int)Math.Round(v3.Vector.X);
                        var y3 = (int)Math.Round(v3.Vector.Y);
                        var z3 = v3.Vector.Z;

                        RasterizeTriangle(pBackBuffer, zBuffer, width, height, x1, y1, x2, y2, x3, y3, z1, z2, z3,
                            bgra);
                    }
                }
            );
        }

        writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        writeableBitmap.Unlock();
    }

    private static Color ApplyIntensity(Color color, float intensity)
    {
        return Color.FromArgb(
            color.A,
            (byte)Math.Min(255, color.R * intensity),
            (byte)Math.Min(255, color.G * intensity),
            (byte)Math.Min(255, color.B * intensity));
    }

    private static unsafe void RasterizeTriangle(
        int* buffer, float[] zBuffer, int width, int height,
        int x1, int y1, int x2, int y2, int x3, int y3,
        float z1, float z2, float z3, int color)
    {
        if (y1 > y2)
        {
            Swap(ref x1, ref x2);
            Swap(ref y1, ref y2);
            Swap(ref z1, ref z2);
        }

        if (y1 > y3)
        {
            Swap(ref x1, ref x3);
            Swap(ref y1, ref y3);
            Swap(ref z1, ref z3);
        }

        if (y2 > y3)
        {
            Swap(ref x2, ref x3);
            Swap(ref y2, ref y3);
            Swap(ref z2, ref z3);
        }

        var invSlope1 = (y2 != y1) ? (float)(x2 - x1) / (y2 - y1) : 0;
        var invSlope2 = (y3 != y1) ? (float)(x3 - x1) / (y3 - y1) : 0;

        for (var y = y1; y <= y2; y++)
        {
            var startX = (int)(x1 + (y - y1) * invSlope1);
            var endX = (int)(x1 + (y - y1) * invSlope2);
            var startZ = z1 + (y - y1) * ((z2 - z1) / (y2 - y1));
            var endZ = z1 + (y - y1) * ((z3 - z1) / (y3 - y1));

            if (startX > endX)
            {
                Swap(ref startX, ref endX);
                Swap(ref startZ, ref endZ);
            }

            for (var x = startX; x <= endX; x++)
            {
                var t = (endX == startX) ? 0 : (float)(x - startX) / (endX - startX);
                var z = startZ + t * (endZ - startZ);

                var index = y * width + x;

                if (x < 0 || x >= width || y < 0 || y >= height || index < 0 || index >= width * height)
                    continue;

                if (!(z < zBuffer[index])) continue;
                zBuffer[index] = z;
                Plot(buffer, width, height, x, y, color);
            }
        }

        invSlope1 = (y3 != y2) ? (float)(x3 - x2) / (y3 - y2) : 0;
        invSlope2 = (y3 != y1) ? (float)(x3 - x1) / (y3 - y1) : 0;

        for (var y = y2; y <= y3; y++)
        {
            var startX = (int)(x2 + (y - y2) * invSlope1);
            var endX = (int)(x1 + (y - y1) * invSlope2);
            var startZ = z2 + (y - y2) * ((z3 - z2) / (y3 - y2));
            var endZ = z1 + (y - y1) * ((z3 - z1) / (y3 - y1));

            if (startX > endX)
            {
                Swap(ref startX, ref endX);
                Swap(ref startZ, ref endZ);
            }

            for (var x = startX; x <= endX; x++)
            {
                var t = (endX == startX) ? 0 : (float)(x - startX) / (endX - startX);
                var z = startZ + t * (endZ - startZ);

                var index = y * width + x;

                if (x < 0 || x >= width || y < 0 || y >= height || index < 0 || index >= width * height)
                    continue;

                if (!(z < zBuffer[index])) continue;
                zBuffer[index] = z;
                Plot(buffer, width, height, x, y, color);
            }
        }
    }

    private static float[] CreateZBuffer(int width, int height)
    {
        var zBuffer = new float[width * height];
        for (var i = 0; i < zBuffer.Length; i++)
        {
            zBuffer[i] = float.MaxValue;
        }

        return zBuffer;
    }

    private static Vector3 ComputeNormal(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var edge1 = v2 - v1;
        var edge2 = v3 - v1;
        
        var vectorCross = Vector3.Cross(edge1, edge2);
        
        return Vector3.Normalize(-vectorCross);
    }

    private static bool IsVisible(Vector3 normal, Vector3 viewDirection)
    {
        return Vector3.Dot(normal, viewDirection) < 0;
    }
}