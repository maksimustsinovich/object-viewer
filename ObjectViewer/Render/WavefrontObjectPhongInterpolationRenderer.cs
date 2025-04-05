using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;
using ObjectViewer.Model;

namespace ObjectViewer.Render;

public abstract class WavefrontObjectPhongInterpolationRenderer : WavefrontObjectTriangleRenderer
{
    public static void DrawFilledTriangles(
        WavefrontObject wavefrontObject,
        WriteableBitmap writeableBitmap,
        PhongLightingModel lightingModel,
        Vector3 eye)
    {
        var lightDirection = Vector3.Normalize(new Vector3(0, 0, -1));
        var width = writeableBitmap.PixelWidth;
        var height = writeableBitmap.PixelHeight;
        var zBuffer = CreateZBuffer(width, height);

        bool hasNormals = wavefrontObject.VertexNormals.Length > 0;

        if (!hasNormals)
        {
            ComputeVertexNormals(wavefrontObject);
        }

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

                    Vector3 n1, n2, n3;
                    if (hasNormals)
                    {
                        n1 = wavefrontObject.VertexNormals[face.Items[i].VertexNormal].Vector;
                        n2 = wavefrontObject.VertexNormals[face.Items[(i + 1) % count].VertexNormal].Vector;
                        n3 = wavefrontObject.VertexNormals[face.Items[(i + 2) % count].VertexNormal].Vector;
                    }
                    else
                    {
                        n1 = v1.Normal;
                        n2 = v2.Normal;
                        n3 = v3.Normal;
                    }

                    var x1 = (int)Math.Round(v1.Vector.X);
                    var y1 = (int)Math.Round(v1.Vector.Y);
                    var z1 = v1.Vector.Z;
                    var x2 = (int)Math.Round(v2.Vector.X);
                    var y2 = (int)Math.Round(v2.Vector.Y);
                    var z2 = v2.Vector.Z;
                    var x3 = (int)Math.Round(v3.Vector.X);
                    var y3 = (int)Math.Round(v3.Vector.Y);
                    var z3 = v3.Vector.Z;

                    RasterizeTriangle(
                        pBackBuffer, zBuffer, width, height,
                        x1, y1, x2, y2, x3, y3,
                        z1, z2, z3,
                        n1, n2, n3,
                        lightDirection, eye, lightingModel);
                }
            });
        }

        writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        writeableBitmap.Unlock();
    }
    
    private static void ComputeVertexNormals(WavefrontObject wavefrontObject)
    {
        var vertexNormals = new Vector3[wavefrontObject.Vertices.Length];
        var normalCounts = new int[wavefrontObject.Vertices.Length];

        foreach (var face in wavefrontObject.Faces)
        {
            var count = face.Items.Count;
            if (count < 3) continue;

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

                var v1 = wavefrontObject.WorldCords[i1];
                var v2 = wavefrontObject.WorldCords[i2];
                var v3 = wavefrontObject.WorldCords[i3];

                var faceNormal = ComputeNormal(v1, v2, v3);

                vertexNormals[i1] += faceNormal;
                vertexNormals[i2] += faceNormal;
                vertexNormals[i3] += faceNormal;

                normalCounts[i1]++;
                normalCounts[i2]++;
                normalCounts[i3]++;
            }
        }

        for (var i = 0; i < vertexNormals.Length; i++)
        {
            if (normalCounts[i] > 0)
            {
                vertexNormals[i] /= normalCounts[i];
                vertexNormals[i] = Vector3.Normalize(vertexNormals[i]);
            }
        }

        for (var i = 0; i < wavefrontObject.Vertices.Length; i++)
        {
            wavefrontObject.Vertices[i].Normal = vertexNormals[i];
        }
    }

    private new static Vector3 ComputeNormal(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var edge1 = v2 - v1;
        var edge2 = v3 - v1;
        return Vector3.Normalize(Vector3.Cross(edge1, edge2));
    }
    
    private static unsafe void RasterizeTriangle(
        int* buffer, float[] zBuffer, int width, int height,
        int x1, int y1, int x2, int y2, int x3, int y3,
        float z1, float z2, float z3,
        Vector3 n1, Vector3 n2, Vector3 n3,
        Vector3 lightDirection, Vector3 eye,
        PhongLightingModel lightingModel)
    {
        if (y1 > y2)
        {
            Swap(ref x1, ref x2);
            Swap(ref y1, ref y2);
            Swap(ref z1, ref z2);
            Swap(ref n1, ref n2);
        }

        if (y1 > y3)
        {
            Swap(ref x1, ref x3);
            Swap(ref y1, ref y3);
            Swap(ref z1, ref z3);
            Swap(ref n1, ref n3);
        }

        if (y2 > y3)
        {
            Swap(ref x2, ref x3);
            Swap(ref y2, ref y3);
            Swap(ref z2, ref z3);
            Swap(ref n2, ref n3);
        }

        var invSlope1 = (y2 != y1) ? (float)(x2 - x1) / (y2 - y1) : 0;
        var invSlope2 = (y3 != y1) ? (float)(x3 - x1) / (y3 - y1) : 0;

        for (var y = y1; y <= y2; y++)
        {
            var startX = (int)(x1 + (y - y1) * invSlope1);
            var endX = (int)(x1 + (y - y1) * invSlope2);
            var startZ = z1 + (y - y1) * ((z2 - z1) / (y2 - y1));
            var endZ = z1 + (y - y1) * ((z3 - z1) / (y3 - y1));
            var startN = InterpolateNormal(y1, y2, n1, n2, y);
            var endN = InterpolateNormal(y1, y3, n1, n3, y);

            if (startX > endX)
            {
                Swap(ref startX, ref endX);
                Swap(ref startZ, ref endZ);
                Swap(ref startN, ref endN);
            }

            for (var x = startX; x <= endX; x++)
            {
                var t = (endX == startX) ? 0 : (float)(x - startX) / (endX - startX);
                var z = startZ + t * (endZ - startZ);
                var interpolatedNormal = InterpolateNormal(startX, endX, startN, endN, x);

                var index = y * width + x;
                if (x < 0 || x >= width || y < 0 || y >= height || index < 0 || index >= width * height)
                    continue;

                if (!(z < zBuffer[index])) continue;

                zBuffer[index] = z;

                var viewDirection = Vector3.Normalize(eye - new Vector3(x, y, z));
                var shadedColor = lightingModel.CalculateLighting(interpolatedNormal, lightDirection, viewDirection);

                Plot(buffer, width, height, x, y, ColorToIntBgra(shadedColor));
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
            var startN = InterpolateNormal(y2, y3, n2, n3, y);
            var endN = InterpolateNormal(y1, y3, n1, n3, y);

            if (startX > endX)
            {
                Swap(ref startX, ref endX);
                Swap(ref startZ, ref endZ);
                Swap(ref startN, ref endN);
            }

            for (var x = startX; x <= endX; x++)
            {
                var t = (endX == startX) ? 0 : (float)(x - startX) / (endX - startX);
                var z = startZ + t * (endZ - startZ);
                var interpolatedNormal = InterpolateNormal(startX, endX, startN, endN, x);

                var index = y * width + x;
                if (x < 0 || x >= width || y < 0 || y >= height || index < 0 || index >= width * height)
                    continue;

                if (!(z < zBuffer[index])) continue;

                zBuffer[index] = z;
                
                var viewDirection = Vector3.Normalize(eye - new Vector3(x, y, z));
                var shadedColor = lightingModel.CalculateLighting(interpolatedNormal, lightDirection, viewDirection);

                Plot(buffer, width, height, x, y, ColorToIntBgra(shadedColor));
            }
        }
    }

    private static Vector3 InterpolateNormal(int y1, int y2, Vector3 n1, Vector3 n2, int y)
    {
        if (y1 == y2) return n1;
        var t = (float)(y - y1) / (y2 - y1);
        return Vector3.Lerp(n1, n2, t);
    }
}