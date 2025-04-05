using System.Globalization;
using System.IO;
using ObjectViewer.Model;

namespace ObjectViewer.Parser;

public static class WavefrontObjectParser
{
    public static WavefrontObject Parse(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        var vertices = new List<Vertex>();
        var vertexTextures = new List<VertexTexture>();
        var vertexNormals = new List<VertexNormal>();
        var faces = new List<Face>();

        using (var reader = new StreamReader(filePath))
        {
            while (reader.ReadLine() is { } line)
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                {
                    continue;
                }

                var tokens = trimmedLine.Split([' '], StringSplitOptions.RemoveEmptyEntries);

                switch (tokens[0])
                {
                    case "v":
                        ParseVertex(tokens, vertices);
                        break;

                    case "vt":
                        ParseVertexTexture(tokens, vertexTextures);
                        break;

                    case "vn":
                        ParseVertexNormal(tokens, vertexNormals);
                        break;

                    case "f":
                        ParseFace(tokens, faces);
                        break;
                }
            }
        }

        return new WavefrontObject
        {
            Vertices = vertices.ToArray(),
            VertexTextures = vertexTextures.ToArray(),
            VertexNormals = vertexNormals.ToArray(),
            Faces = faces.ToArray()
        };
    }

    private static void ParseVertex(string[] tokens, List<Vertex> vertices)
    {
        if (tokens.Length < 4)
        {
            throw new FormatException($"Invalid vertex line format: {string.Join(" ", tokens)}");
        }

        var x = float.Parse(tokens[1], CultureInfo.InvariantCulture);
        var y = float.Parse(tokens[2], CultureInfo.InvariantCulture);
        var z = float.Parse(tokens[3], CultureInfo.InvariantCulture);
        var w = tokens.Length > 4 ? float.Parse(tokens[4], CultureInfo.InvariantCulture) : 1.0f;

        vertices.Add(new Vertex(x, y, z, w));
    }

    private static void ParseVertexTexture(string[] tokens, List<VertexTexture> vertexTextures)
    {
        if (tokens.Length < 2)
        {
            throw new FormatException($"Invalid texture coordinate line format: {string.Join(" ", tokens)}");
        }

        var u = float.Parse(tokens[1], CultureInfo.InvariantCulture);
        var v = tokens.Length > 2 ? float.Parse(tokens[2], CultureInfo.InvariantCulture) : 0.0f;
        var w = tokens.Length > 3 ? float.Parse(tokens[3], CultureInfo.InvariantCulture) : 0.0f;

        vertexTextures.Add(new VertexTexture(u, v, w));
    }

    private static void ParseVertexNormal(string[] tokens, List<VertexNormal> vertexNormals)
    {
        if (tokens.Length < 4)
        {
            throw new FormatException($"Invalid normal line format: {string.Join(" ", tokens)}");
        }

        var i = float.Parse(tokens[1], CultureInfo.InvariantCulture);
        var j = float.Parse(tokens[2], CultureInfo.InvariantCulture);
        var k = float.Parse(tokens[3], CultureInfo.InvariantCulture);

        vertexNormals.Add(new VertexNormal(i, j, k));
    }

    private static void ParseFace(string[] tokens, List<Face> faces)
    {
        if (tokens.Length < 4)
        {
            throw new FormatException($"Invalid face line format: {string.Join(" ", tokens)}");
        }

        var faceItems = new List<FaceItem>();

        for (var i = 1; i < tokens.Length; i++)
        {
            var indices = tokens[i].Split('/');
            var vertexIndex = ParseIndex(indices[0]);
            var textureIndex = indices.Length > 1 && !string.IsNullOrEmpty(indices[1]) ? ParseIndex(indices[1]) : 0;
            var normalIndex = indices.Length > 2 && !string.IsNullOrEmpty(indices[2]) ? ParseIndex(indices[2]) : 0;

            faceItems.Add(new FaceItem(vertexIndex, textureIndex, normalIndex));
        }

        faces.Add(new Face { Items = faceItems });
    }

    private static int ParseIndex(string index)
    {
        var parsedIndex = int.Parse(index, CultureInfo.InvariantCulture);
        return parsedIndex > 0 ? parsedIndex - 1 : parsedIndex;
    }
}