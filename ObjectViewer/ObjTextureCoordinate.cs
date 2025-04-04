using System.Numerics;

public class ObjTextureCoordinate
{
    public Vector3 Coordinates { get; }

    public ObjTextureCoordinate(float u, float v = 0.0f, float w = 0.0f)
    {
        Coordinates = new Vector3(u, v, w);
    }
}