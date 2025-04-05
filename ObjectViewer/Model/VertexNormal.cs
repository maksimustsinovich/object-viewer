using System.Numerics;

namespace ObjectViewer.Model;

public class VertexNormal(float i = 0, float j = 0, float k = 0)
{
    public Vector3 Vector { get; set; } = new(i, j, k);

    public VertexNormal(Vector3 vector) : this() => Vector = vector;
}