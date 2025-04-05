using System.Numerics;

namespace ObjectViewer.Model;

public class Vertex
{
    public Vertex(Vector4 vector)
    {
        Vector = vector;
    }

    public Vertex(float x = 0, float y = 0, float z = 0, float w = 1.0f)
    {
        Vector = new Vector4(x, y, z, w);
    }

    public Vector4 Vector { get; set; }
    
    public float X => Vector.X;
    
    public float Y => Vector.Y;
    
    public float Z => Vector.Z;
    
}