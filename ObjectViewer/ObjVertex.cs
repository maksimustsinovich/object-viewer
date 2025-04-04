using System.Numerics;

public class ObjVertex
{
    public Vector4 Coordinates { get; }

    public ObjVertex(float x, float y, float z, float w = 1.0f)
    {
        Coordinates = new Vector4(x, y, z, w);
    }
}