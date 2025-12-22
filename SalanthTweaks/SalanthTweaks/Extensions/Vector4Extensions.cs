using System.Numerics;
using Dalamud.Interface;

namespace SalanthTweaks.Extensions;

public static class Vector4Extensions
{

    public static Vector4 Add(this Vector4 v1, Vector4 v2)
    {
        return new Vector4(v1.X+v2.X, v1.Y+v2.Y, v1.Z+v2.Z, v1.W+v2.W);
    }

    public static Vector4 Multiply(this Vector4 v1, Vector4 v2)
    {
        return new Vector4(v1.X*v2.X, v1.Y*v2.Y, v1.Z*v2.Z, v1.W*v2.W);
    }

    public static Vector4 Subtract(this Vector4 v1, Vector4 v2)
    {
        return new Vector4(v1.X-v2.X, v1.Y-v2.Y, v1.Z-v2.Z, v1.W-v2.W);
    }

    public static Vector4 Clamp(this Vector4 v)
    {
        return new Vector4(
            float.Clamp(v.X, 0, 1),
            float.Clamp(v.Y, 0, 1),
            float.Clamp(v.Z, 0, 1),
            float.Clamp(v.W, 0, 1)
        );
    }
}
