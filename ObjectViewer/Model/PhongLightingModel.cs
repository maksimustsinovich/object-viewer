using System.Drawing;
using System.Numerics;

namespace ObjectViewer.Model;

public class PhongLightingModel(
    float ambientCoefficient,
    float diffuseCoefficient,
    float specularCoefficient,
    float shininess,
    Color ambientColor,
    Color diffuseColor,
    Color specularColor)
{
    public Color CalculateLighting(Vector3 normal, Vector3 lightDirection, Vector3 viewDirection)
    {
        normal = Vector3.Normalize(normal);
        lightDirection = Vector3.Normalize(lightDirection);
        viewDirection = Vector3.Normalize(viewDirection);

        var ambient = CalculateAmbient();
        var diffuse = CalculateDiffuse(normal, lightDirection);
        var specular = CalculateSpecular(normal, lightDirection, viewDirection);

        return CombineColors(ambient, diffuse, specular);
    }

    private Color CalculateAmbient()
    {
        return ApplyIntensity(ambientColor, ambientCoefficient);
    }

    private Color CalculateDiffuse(Vector3 normal, Vector3 lightDirection)
    {
        var diffuseIntensity = Math.Max(0, Vector3.Dot(normal, -lightDirection));
        return ApplyIntensity(diffuseColor, diffuseCoefficient * (float)diffuseIntensity);
    }

    private Color CalculateSpecular(Vector3 normal, Vector3 lightDirection, Vector3 viewDirection)
    {
        var reflection = Vector3.Reflect(lightDirection, normal);
        var specularIntensity = Math.Pow(Math.Max(0, Vector3.Dot(reflection, viewDirection)), shininess);
        return ApplyIntensity(specularColor, specularCoefficient * (float)specularIntensity);
    }

    private static Color ApplyIntensity(Color color, float intensity)
    {
        return Color.FromArgb(
            color.A,
            (byte)Math.Min(255, color.R * intensity),
            (byte)Math.Min(255, color.G * intensity),
            (byte)Math.Min(255, color.B * intensity));
    }

    private static Color CombineColors(Color c1, Color c2, Color c3)
    {
        return Color.FromArgb(
            255,
            (byte)Math.Min(255, c1.R + c2.R + c3.R),
            (byte)Math.Min(255, c1.G + c2.G + c3.G),
            (byte)Math.Min(255, c1.B + c2.B + c3.B));
    }
}