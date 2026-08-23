using System.Numerics;
using FeatherCAD.Models;

public static class GeometryUtils
{
    public static List<Vector2> GetEntitiesIntersections(Entity e1, Entity e2)
    {
        List<Vector2> points = new List<Vector2>();

        // --- CAS 1 : LIGNE / LIGNE ---
        if (e1 is LineEntity l1 && e2 is LineEntity l2)
        {
            if (FindIntersection(l1.Start, l1.End, l2.Start, l2.End, out Vector2 inter))
                points.Add(inter);
        }
        // --- CAS 2 : LIGNE / CERCLE ---
        else if (e1 is LineEntity line && e2 is CircleEntity circ)
        {
            points.AddRange(FindLineCircleIntersections(line.Start, line.End, circ.Center, circ.Radius));
        }
        else if (e1 is CircleEntity circ2 && e2 is LineEntity line2)
        {
            points.AddRange(FindLineCircleIntersections(line2.Start, line2.End, circ2.Center, circ2.Radius));
        }
        // --- CAS 3 : CERCLE / CERCLE ---
        else if (e1 is CircleEntity c1 && e2 is CircleEntity c2)
        {
            points.AddRange(FindCircleCircleIntersections(c1.Center, c1.Radius, c2.Center, c2.Radius));
        }
        // --- CAS 4 : ARC / (LIGNE, CERCLE ou ARC) ---
        // On calcule l'intersection comme s'il s'agissait de cercles/lignes, 
        // puis on filtre si le point est réellement sur l'arc.
        else if (e1 is ArcEntity || e2 is ArcEntity)
        {
            // On transforme temporairement l'arc en cercle pour le calcul
            Entity shadow1 = e1 is ArcEntity a1 ? new CircleEntity(a1.Center, a1.Radius, System.Windows.Media.Colors.Black, 1) : e1;
            Entity shadow2 = e2 is ArcEntity a2 ? new CircleEntity(a2.Center, a2.Radius, System.Windows.Media.Colors.Black, 1) : e2;

            var rawPoints = GetEntitiesIntersections(shadow1, shadow2);
            foreach (var p in rawPoints)
            {
                bool valid = true;
                if (e1 is ArcEntity arc1 && !arc1.IsPointOnArc(p, 0.1f)) valid = false;
                if (e2 is ArcEntity arc2 && !arc2.IsPointOnArc(p, 0.1f)) valid = false;

                if (valid) points.Add(p);
            }
        }

        return points;
    }
    // Calcule l'intersection de deux segments de ligne AB et CD
    public static bool FindIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 result)
    {
        result = Vector2.Zero;
        float denom = (D.Y - C.Y) * (B.X - A.X) - (D.X - C.X) * (B.Y - A.Y);
        if (Math.Abs(denom) < 0.0001f) return false;

        float ua = ((D.X - C.X) * (A.Y - C.Y) - (D.Y - C.Y) * (A.X - C.X)) / denom;
        float ub = ((B.X - A.X) * (A.Y - C.Y) - (B.Y - A.Y) * (A.X - C.X)) / denom;

        if (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1)
        {
            result = new Vector2(A.X + ua * (B.X - A.X), A.Y + ua * (B.Y - A.Y));
            return true;
        }
        return false;
    }
    // Projette un point P sur le segment de ligne défini par les points A et B
    public static Vector2 ProjectPointOnLine(Vector2 P, Vector2 A, Vector2 B)
    {
        Vector2 ap = P - A;
        Vector2 ab = B - A;
        float ab2 = Vector2.Dot(ab, ab);
        if (ab2 == 0) return A;
        float t = Math.Max(0, Math.Min(1, Vector2.Dot(ap, ab) / ab2));
        return A + t * ab;
    }
    // Calcule l'intersection de deux lignes infinies définies par Point + Direction
    public static bool IntersectLines(AlignmentLine L1, AlignmentLine L2, out Vector2 intersection)
    {
        intersection = Vector2.Zero;
        float det = L2.Direction.X * L1.Direction.Y - L2.Direction.Y * L1.Direction.X;
        if (Math.Abs(det) < 0.0001f) return false; // Parallèles

        float t = (L2.Direction.X * (L2.Origin.Y - L1.Origin.Y) - L2.Direction.Y * (L2.Origin.X - L1.Origin.X)) / det;
        intersection = L1.Origin + L1.Direction * t;
        return true;
    }
    // Projette un point sur une ligne infinie
    public static Vector2 ProjectPointOnInfiniteLine(Vector2 P, Vector2 Origin, Vector2 Direction)
    {
        Vector2 v = P - Origin;
        float d = Vector2.Dot(v, Direction);
        return Origin + Direction * d;
    }
    // Retourne une ligne d'alignement angulaire si la souris est proche d'un angle magnétique
    public static AlignmentLine? GetAngularAlignmentLine(Vector2 mouse, Vector2 start, float threshold)
    {
        float dist = Vector2.Distance(mouse, start);
        // On ne calcule pas d'angle si la souris est trop proche du point de départ
        if (dist < 0.01f) return null;

        // 1. Calcul de l'angle actuel de la souris
        double angleRad = Math.Atan2(mouse.Y - start.Y, mouse.X - start.X);
        double angleDeg = angleRad * (180.0 / Math.PI);

        // 2. Liste des angles magnétiques
        double[] targets = { 0, 30, 45, 60, 90, 120, 135, 150, 180, -30, -45, -60, -90, -120, -135, -150 };
        double angleThreshold = 2.5; // Tolérance en degrés

        foreach (double target in targets)
        {
            if (Math.Abs(angleDeg - target) < angleThreshold)
            {
                // 3. On a trouvé un angle, on crée la direction correspondante
                double snappedRad = target * (Math.PI / 180.0);
                return new AlignmentLine
                {
                    Origin = start,
                    Direction = new Vector2((float)Math.Cos(snappedRad), (float)Math.Sin(snappedRad)),
                    Label = $"{target}°"
                };
            }
        }

        return null;
    }
    // Calcule le cercle circonscrit à trois points
    public static bool CalculateCircle3P(Vector2 p1, Vector2 p2, Vector2 p3, out Vector2 center, out float radius)
    {
        center = Vector2.Zero;
        radius = 0;

        // Calcul du dénominateur pour vérifier si les points ne sont pas alignés
        float x1 = p1.X, y1 = p1.Y;
        float x2 = p2.X, y2 = p2.Y;
        float x3 = p3.X, y3 = p3.Y;

        float D = 2 * (x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2));

        if (Math.Abs(D) < 0.001f) return false; // Points alignés, pas de cercle possible

        // Formule pour le centre du cercle circonscrit
        float centerX = ((x1 * x1 + y1 * y1) * (y2 - y3) + (x2 * x2 + y2 * y2) * (y3 - y1) + (x3 * x3 + y3 * y3) * (y1 - y2)) / D;
        float centerY = ((x1 * x1 + y1 * y1) * (x3 - x2) + (x2 * x2 + y2 * y2) * (x1 - x3) + (x3 * x3 + y3 * y3) * (x2 - x1)) / D;

        center = new Vector2(centerX, centerY);
        radius = Vector2.Distance(center, p1);
        return true;
    }
    // Vérifie si un point est proche d'un segment de ligne
    public static bool IsPointNearLine(Vector2 p, Vector2 a, Vector2 b, float threshold)
    {
        float l2 = Vector2.DistanceSquared(a, b);
        if (l2 == 0) return Vector2.Distance(p, a) < threshold;

        // Calcul de la projection du point P sur le segment AB
        float t = Math.Max(0, Math.Min(1, Vector2.Dot(p - a, b - a) / l2));
        Vector2 projection = a + t * (b - a);

        return Vector2.Distance(p, projection) < threshold;
    }
    // Calcule les intersections entre une ligne et un cercle
    public static List<Vector2> FindLineCircleIntersections(Vector2 p1, Vector2 p2, Vector2 center, float radius)
    {
        List<Vector2> result = new List<Vector2>();
        Vector2 dir = p2 - p1;
        Vector2 diff = p1 - center;

        float a = Vector2.Dot(dir, dir);
        float b = 2 * Vector2.Dot(diff, dir);
        float c = Vector2.Dot(diff, diff) - radius * radius;

        float discriminant = b * b - 4 * a * c;

        if (discriminant >= 0)
        {
            discriminant = (float)Math.Sqrt(discriminant);
            float t1 = (-b - discriminant) / (2 * a);
            float t2 = (-b + discriminant) / (2 * a);

            // On vérifie si les points sont bien sur le segment (t entre 0 et 1)
            if (t1 >= 0 && t1 <= 1) result.Add(p1 + t1 * dir);
            if (t2 >= 0 && t2 <= 1 && Math.Abs(t1 - t2) > 0.001) result.Add(p1 + t2 * dir);
        }
        return result;
    }
    // Calcule les intersections entre deux cercles
    public static List<Vector2> FindCircleCircleIntersections(Vector2 c1, float r1, Vector2 c2, float r2)
    {
        List<Vector2> result = new List<Vector2>();
        float d = Vector2.Distance(c1, c2);

        // 1. Vérifier si des intersections sont possibles
        if (d > r1 + r2) return result;           // Trop loin l'un de l'autre
        if (d < Math.Abs(r1 - r2)) return result; // L'un est dans l'autre
        if (d < 0.001f && Math.Abs(r1 - r2) < 0.001f) return result; // Cercles identiques

        // 2. Calcul mathématique des points
        float a = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
        float h = (float)Math.Sqrt(Math.Max(0, r1 * r1 - a * a));

        // Point P2 est le point où la ligne joignant les intersections coupe la ligne des centres
        Vector2 p2 = c1 + a * (c2 - c1) / d;

        // Calcul des deux points d'intersection
        float x3_1 = p2.X + h * (c2.Y - c1.Y) / d;
        float y3_1 = p2.Y - h * (c2.X - c1.X) / d;
        result.Add(new Vector2(x3_1, y3_1));

        if (h > 0.001f) // S'il y a deux points distincts
        {
            float x3_2 = p2.X - h * (c2.Y - c1.Y) / d;
            float y3_2 = p2.Y + h * (c2.X - c1.X) / d;
            result.Add(new Vector2(x3_2, y3_2));
        }

        return result;
    }
}