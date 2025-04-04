using System.Globalization;
using System.IO;

public class ObjParser
{
    public ObjModel Parse(string filePath)
    {
        var model = new ObjModel();
        
        using var reader = new StreamReader(filePath);
        string line;
        
        while ((line = reader.ReadLine()) != null)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            var parts = trimmedLine.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;

            var command = parts[0];
            var data = parts.Length > 1 ? parts[1] : "";

            switch (command)
            {
                case "v":
                    ParseVertex(data, model.Vertices);
                    break;
                case "vt":
                    ParseTextureCoordinate(data, model.TextureCoordinates);
                    break;
                case "vn":
                    ParseNormal(data, model.Normals);
                    break;
                case "f":
                    ParseFace(data, model);
                    break;
            }
        }

        return model;
    }

    private void ParseVertex(string data, List<ObjVertex> vertices)
    {
        var components = data.Split(' ');
        if (components.Length < 3)
            throw new FormatException("Vertex must have at least 3 components");

        var x = float.Parse(components[0], CultureInfo.InvariantCulture);
        var y = float.Parse(components[1], CultureInfo.InvariantCulture);
        var z = float.Parse(components[2], CultureInfo.InvariantCulture);
        var w = components.Length > 3 
            ? float.Parse(components[3], CultureInfo.InvariantCulture) 
            : 1.0f;

        vertices.Add(new ObjVertex(x, y, z, w));
    }

    private void ParseTextureCoordinate(string data, List<ObjTextureCoordinate> textureCoordinates)
    {
        var components = data.Split(' ');
        if (components.Length < 1)
            throw new FormatException("Texture coordinate must have at least 1 component");

        var u = float.Parse(components[0], CultureInfo.InvariantCulture);
        var v = components.Length > 1 
            ? float.Parse(components[1], CultureInfo.InvariantCulture) 
            : 0.0f;
        var w = components.Length > 2 
            ? float.Parse(components[2], CultureInfo.InvariantCulture) 
            : 0.0f;

        textureCoordinates.Add(new ObjTextureCoordinate(u, v, w));
    }

    private void ParseNormal(string data, List<ObjNormal> normals)
    {
        var components = data.Split(' ');
        if (components.Length != 3)
            throw new FormatException("Normal must have exactly 3 components");

        var x = float.Parse(components[0], CultureInfo.InvariantCulture);
        var y = float.Parse(components[1], CultureInfo.InvariantCulture);
        var z = float.Parse(components[2], CultureInfo.InvariantCulture);

        normals.Add(new ObjNormal(x, y, z));
    }

    private int ParseIndex(string indexStr, int listCount)
    {
        if (!int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            throw new FormatException($"Invalid index: {indexStr}");

        if (index == 0)
            throw new FormatException("Index cannot be zero");

        return index > 0 ? index - 1 : listCount + index;
    }

    private void ParseFace(string data, ObjModel model)
    {
        var face = new ObjFace();
        var vertexStrings = data.Split(' ');

        foreach (var vertexStr in vertexStrings)
        {
            var parts = vertexStr.Split('/');
            if (parts.Length == 0)
                continue;

            var vertexIndex = ParseIndex(parts[0], model.Vertices.Count);
            var textureIndex = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) 
                ? ParseIndex(parts[1], model.TextureCoordinates.Count) 
                : (int?)null;
            var normalIndex = parts.Length > 2 && !string.IsNullOrEmpty(parts[2]) 
                ? ParseIndex(parts[2], model.Normals.Count) 
                : (int?)null;

            face.Vertices.Add(new ObjFaceVertex
            {
                VertexIndex = vertexIndex,
                TextureCoordinateIndex = textureIndex,
                NormalIndex = normalIndex
            });
        }

        model.Faces.Add(face);
    }
}