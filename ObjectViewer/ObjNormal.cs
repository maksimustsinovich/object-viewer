using System.Numerics;

public class ObjNormal
{
    public Vector3 Direction { get; }

    public ObjNormal(float x, float y, float z)
    {
        Direction = new Vector3(x, y, z);
    }
}