public class ObjModel
{
    public List<ObjVertex> Vertices { get; } = new List<ObjVertex>();
    public List<ObjTextureCoordinate> TextureCoordinates { get; } = new List<ObjTextureCoordinate>();
    public List<ObjNormal> Normals { get; } = new List<ObjNormal>();
    public List<ObjFace> Faces { get; } = new List<ObjFace>();
}