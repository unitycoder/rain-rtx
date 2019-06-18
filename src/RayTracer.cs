﻿using System;
using System.Drawing;
using System.Numerics;
using RainRTX;

public enum IntersectionType : byte
{
    None = 0,
    Ground = 1,
    Sphere = 2,
}

public static partial class GraphicCore
{
    private static RayHit TraceRay(Ray ray)
    {
        float closest_t = float.PositiveInfinity;
        Sphere closest_sphere = Sphere.Null;
        var intersection = IntersectionType.None;

        if (Scene.Spheres == null) goto SkipShereIntersection;

        for (var i = 0; i < Scene.Spheres.Length; i++)
        {
            var ts = IntersectRaySphere(ray, i);
            if (ts.X < closest_t && 1 < ts.X && ts.X < float.PositiveInfinity)
            {
                closest_t = ts.X;
                closest_sphere = Scene.Spheres[i];
                intersection = IntersectionType.Sphere;
            }
            if (ts.Y < closest_t && 1 < ts.Y && ts.Y < float.PositiveInfinity)
            {
                closest_t = ts.Y;
                closest_sphere = Scene.Spheres[i];
                intersection = IntersectionType.Sphere;
            }
        }
    SkipShereIntersection:

        if (Scene.Ground == null) goto SkipGroundIntersection;
        float tg = IntersectRayGround(ray);
        if (tg > 0 && tg < closest_t)
        {
            closest_t = tg;
            intersection = IntersectionType.Ground;
        }
    SkipGroundIntersection:

        if (intersection == IntersectionType.Sphere)
        {
            Vector3 point = ray.origin + ray.direction * closest_t;
            Vector3 normal = (point - closest_sphere.center);
            normal /= normal.Length();
            return new RayHit
            {
                intersection = IntersectionType.Sphere,
                color = closest_sphere.color,
                specular = closest_sphere.specular,
                normal = normal,
                position = point
            };
        }

        if (intersection == IntersectionType.Ground)
        {
            Vector3 point = ray.origin + ray.direction * closest_t;
            return new RayHit
            {
                intersection = IntersectionType.Ground,
                color = Scene.Ground.color,
                specular = Scene.Ground.specular,
                normal = new Vector3(0f, 1f, 0f),
                position = point
            };
        }
        return new RayHit { intersection = IntersectionType.None };
    }

    private static Vector2 IntersectRaySphere(Ray ray, int sphereIndex)
    {
        var sphere = Scene.Spheres[sphereIndex];
        Vector3 oc = ray.origin - sphere.center;

        float k1 = Vector3.Dot(ray.direction, ray.direction);
        float k2 = 2 * Vector3.Dot(oc, ray.direction);
        float k3 = Vector3.Dot(oc,oc) - sphere.radius * sphere.radius;

        float discriminant = k2 * k2 - 4 * k1 * k3;
        if (discriminant < 0) return new Vector2(float.PositiveInfinity, float.PositiveInfinity);

        float t1 = (float)(-k2 + Math.Sqrt(discriminant)) / (2 * k1);
        float t2 = (float)(-k2 - Math.Sqrt(discriminant)) / (2 * k1);
        return new Vector2(t1, t2);
    }

    private static float IntersectRayGround(Ray ray)
    {
        return -ray.origin.Y / ray.direction.Y;
    }

    private static Color24 Shade(ref Ray ray, RayHit hit)
    {
        if (hit.intersection != IntersectionType.None)
        {
            Color24 res = hit.color;
            return ComputeLighting(res, hit.position, hit.normal, ray.direction, hit.specular);
        }
        if (Scene.SkyBox != null) return Scene.SkyBox.SampleFromCamera(ray.direction);
        return Scene.BackgroundColor;
    }

    private static Color ComputeLighting(Color24 source, Vector3 point, Vector3 normal, Vector3 view, float s)
    {
        var normal_len = normal.Length();
        var intensivity = 0f;
        for (int a = 0; a < Scene.ambientLights.Length; a++)
            intensivity += Scene.ambientLights[a].intensivity;

        for (int p = 0; p < Scene.pointLights.Length; p++)
        {
            Vector3 light = (Scene.pointLights[p].position - point);
            var i = (float)Vector3.Dot(light, normal);
            if (i > 0)
                intensivity += Scene.pointLights[p].intensivity * (float)(i / (normal_len * light.Length()));
            if (s > -1)
            {
                Vector3 reflection = 2 * normal * Vector3.Dot(normal,light);
                var r_dot_v = Vector3.Dot(view, reflection);
                if (r_dot_v > 0)
                    intensivity += Scene.pointLights[p].intensivity * (float)Math.Pow(r_dot_v / (reflection.Length() * view.Length()), s);
            }
        }

        for (int d = 0; d < Scene.directionalLights.Length; d++)
        {
            Vector3 light = Scene.directionalLights[d].direction;
            var i = (float)Vector3.Dot(normal, light);
            if (i > 0)
                intensivity += Scene.directionalLights[d].intensivity * (float)(i / (normal_len * light.Length()));
            if (s > -1)
            {
                Vector3 reflection = 2 * normal * Vector3.Dot(light, normal);
                var r_dot_v = Vector3.Dot(view, reflection);
                if (r_dot_v > 0)
                    intensivity += Scene.pointLights[d].intensivity * (float)Math.Pow(r_dot_v / (reflection.Length() * view.Length()), s);
            }
        }

        return source * intensivity;
    }
}

