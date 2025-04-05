namespace ObjectViewer.Model;

public class FaceItem(int vertex = 0, int vertexTexture = 0, int vertexNormal = 0)
{
    public int Vertex { get; set; } = vertex;
    
    public int VertexTexture { get; set; } = vertexTexture;
    
    public int VertexNormal { get; set; } = vertexNormal;
}