using System.Numerics;

namespace ObjectViewer.Model;

public class WavefrontObject
{
    public Vertex[] Vertices { get; set; } = [];
    
    public VertexTexture[] VertexTextures { get; set; } = [];
    
    public VertexNormal[] VertexNormals { get; set; } = [];
    
    public Face[] Faces { get; set; } = [];

    public Vector3[] WorldCords { get; set; } = [];
}