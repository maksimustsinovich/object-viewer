using System.Numerics;

namespace ObjectViewer.Model;

public class VertexTexture(float u = 0, float v = 0, float w = 0)
{
    public Vector3 Vector { get; set; } = new Vector3(u, v, w);
}