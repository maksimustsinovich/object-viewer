using System.Numerics;

namespace ObjectViewer.Transformation;

public static class MatrixTransformation
{
    public static Matrix4x4 CreateWorldMatrix(Vector3 translation, Vector3 rotation, float scale)
    {
        var rotationMatrix = Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        var scaleMatrix = Matrix4x4.CreateScale(scale);
        var translationMatrix = Matrix4x4.CreateTranslation(translation);

        var worldMatrix = translationMatrix * rotationMatrix * scaleMatrix;

        return worldMatrix;
    }

    public static Matrix4x4 CreateViewMatrix(Vector3 eye, Vector3 target, Vector3 up)
    {
        var zAxis = Vector3.Normalize(eye - target);
        var xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));

        var tx = -Vector3.Dot(xAxis, eye);
        var ty = -Vector3.Dot(up, eye);
        var tz = -Vector3.Dot(zAxis, eye);

        var view = new Matrix4x4(
            xAxis.X, xAxis.Y, xAxis.Z, tx,
            up.X, up.Y, up.Z, ty,
            zAxis.X, zAxis.Y, zAxis.Z, tz,
            0.0f, 0.0f, 0.0f, 1.0f
        );

        view = Matrix4x4.Transpose(view);

        return view;
    }

    public static Matrix4x4 CreateProjectionMatrix(float width, float height, float zNear, float zFar)
    {
        //var orthographic = new Matrix4x4(
        //    2 * zNear / width, 0, 0, 0,
        //    0, 2 * zNear / height, 0, 0,
        //    0, 0, zFar / (zNear - zFar), zNear * zFar / (zNear - zFar),
        //    0, 0, -1, 0
        //);

        var orthographic = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2, width / height, zNear, zFar);
            //Matrix4x4.Transpose(orthographic);
        
        return orthographic;
    }

    public static Matrix4x4 CreateViewportMatrix(float width, float height, float xMin = 0.0f, float yMin = 0.0f)
    {
        var viewportMatrix = new Matrix4x4(
            width / 2, 0, 0, xMin + width / 2,
            0, -height / 2, 0, yMin + height / 2,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        viewportMatrix = Matrix4x4.Transpose(viewportMatrix);

        return viewportMatrix;
    }
}