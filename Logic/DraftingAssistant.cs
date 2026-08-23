using FeatherCAD.Models;
using FeatherCAD.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace FeatherCAD.Logic;


public class DraftingAssistant
{
    private List<Vector2> _inferenceMemory = new List<Vector2>();
    private const int MaxMemory = 5;

    public float SnapThreshold { get; set; } = 12f;
    public double AngleThreshold { get; set; } = 2.0;

    public void ClearMemory() => _inferenceMemory.Clear();
    public List<Vector2> GetMemory() => _inferenceMemory;
    // Retoune un SnapResult basé sur la position de la souris, les entités existantes, le zoom et un point de départ temporaire (si applicable).
    public SnapResult CalculateSnap(Vector2 mouseWorld, List<Entity> entities, float zoom, Vector2? tempStartPoint)
    {
        float threshold = SnapThreshold / zoom;

        // --- 1. PRIORITÉ ABSOLUE : POINTS REMARQUABLES (Fin, Milieu) ---
        var pointSnap = GetPointSnap(mouseWorld, entities, threshold);
        if (pointSnap != null) return pointSnap.Value;

        // --- 2. INTERSECTIONS RÉELLES (Entre deux lignes dessinées) ---
        var interSnap = GetIntersectionSnap(mouseWorld, entities, threshold);
        if (interSnap != null) return interSnap.Value;

        // --- 3. PERPENDICULAIRE (Seulement si on est en train de dessiner) ---
        if (tempStartPoint.HasValue)
        {
            var perpSnap = GetPerpendicularSnap(mouseWorld, tempStartPoint.Value, entities, threshold);
            if (perpSnap != null) return perpSnap.Value;
        }

        // --- 4. COLLECTE DES ALIGNEMENTS (LIGNES DE RAPPEL) ---
        List<AlignmentLine> activeLines = new List<AlignmentLine>();

        // A. Alignements sur la mémoire d'inférence (H et V)
        var pts = new List<Vector2>(_inferenceMemory);
        if (tempStartPoint.HasValue) pts.Add(tempStartPoint.Value);

        foreach (var pt in pts)
        {
            if (Math.Abs(mouseWorld.X - pt.X) < threshold)
                activeLines.Add(new AlignmentLine { Origin = pt, Direction = Vector2.UnitY, Label = "Vertical" });

            if (Math.Abs(mouseWorld.Y - pt.Y) < threshold)
                activeLines.Add(new AlignmentLine { Origin = pt, Direction = Vector2.UnitX, Label = "Horizontal" });
        }

        // B. Alignements Angulaires (30°, 45°, etc.)
        if (tempStartPoint.HasValue)
        {
            var angular = GeometryUtils.GetAngularAlignmentLine(mouseWorld, tempStartPoint.Value, threshold);
            if (angular != null) activeLines.Add(angular.Value);
        }

        // --- 5. INTERSECTION DE LIGNES D'ALIGNEMENT (Le "Graal" de Graphite) ---
        if (activeLines.Count >= 2)
        {
            if (GeometryUtils.IntersectLines(activeLines[0], activeLines[1], out Vector2 inter))
            {
                if (Vector2.Distance(mouseWorld, inter) < threshold * 2)
                {
                    return new SnapResult
                    {
                        WorldPoint = inter,
                        Type = SnapType.Alignment,
                        Label = "Intersection d'alignement",
                        ActiveAlignments = activeLines
                    };
                }
            }
        }

        // --- 6. ALIGNEMENT SIMPLE ---
        if (activeLines.Count > 0)
        {
            Vector2 snapped = GeometryUtils.ProjectPointOnInfiniteLine(mouseWorld, activeLines[0].Origin, activeLines[0].Direction);
            return new SnapResult
            {
                WorldPoint = snapped,
                Type = SnapType.Alignment,
                Label = activeLines[0].Label,
                ActiveAlignments = new List<AlignmentLine> { activeLines[0] }
            };
        }
        // --- 7 AIMANTATION SUR L'OBJET ---
        var onEntitySnap = GetOnEntitySnap(mouseWorld, entities, threshold);
        if (onEntitySnap != null) return onEntitySnap.Value;

        return new SnapResult { Type = SnapType.None, ActiveAlignments = new List<AlignmentLine>() };
    }

    // --- MÉTHODES DE DÉTECTION ---
    // Retourne un SnapResult si la souris est proche d'un point remarquable (début, fin, milieu) d'une entité.
    private SnapResult? GetPointSnap(Vector2 mouse, List<Entity> entities, float threshold)
    {

        var atomicEntities = GetAllAtomicEntities(entities);

        foreach (var entity in atomicEntities)
        {
            // --- SNAP SUR LIGNES ---
            if (entity is LineEntity line)
            {
                Vector2[] pts = { line.Start, line.End, (line.Start + line.End) / 2 };
                foreach (var pt in pts)
                {
                    if (Vector2.Distance(mouse, pt) < threshold)
                    {
                        UpdateMemory(pt);
                        string label = pt == line.Start ? "Extrémité" : pt == line.End ? "Extrémité" : "Milieu";
                        return new SnapResult { WorldPoint = pt, Type = SnapType.End, Label = label };
                    }
                }
            }
            // --- SNAP SUR CERCLES ---
            else if (entity is CircleEntity circle)
            {
                // Liste des points remarquables du cercle
                var circlePoints = new List<(Vector2 pt, string label)>
                {
                    (circle.Center, "Centre"),
                    (circle.Pt1 != Vector2.Zero ? circle.Pt1 : Vector2.Zero, "Point 1"),
                    (circle.Pt2 != Vector2.Zero ? circle.Pt2 : Vector2.Zero, "Point 2"),
                    (circle.Pt3 != Vector2.Zero ? circle.Pt3 : Vector2.Zero, "Point 3"),
                    (circle.Center + new Vector2(circle.Radius, 0), "Quadrant"),
                    (circle.Center + new Vector2(-circle.Radius, 0), "Quadrant"),
                    (circle.Center + new Vector2(0, circle.Radius), "Quadrant"),
                    (circle.Center + new Vector2(0, -circle.Radius), "Quadrant")
                };

                foreach (var cp in circlePoints)
                {
                    if (Vector2.Distance(mouse, cp.pt) < threshold)
                    {
                        UpdateMemory(cp.pt);
                        return new SnapResult { WorldPoint = cp.pt, Type = SnapType.End, Label = cp.label };
                    }
                }
            }
            else if (entity is ArcEntity arc)
            {
                // 1. Début et Fin
                if (Vector2.Distance(mouse, arc.StartPoint) < threshold)
                    return new SnapResult { WorldPoint = arc.StartPoint, Type = SnapType.End, Label = "Extrémité" };

                if (Vector2.Distance(mouse, arc.EndPoint) < threshold)
                    return new SnapResult { WorldPoint = arc.EndPoint, Type = SnapType.End, Label = "Extrémité" };

                // 2. Centre
                if (Vector2.Distance(mouse, arc.Center) < threshold)
                    return new SnapResult { WorldPoint = arc.Center, Type = SnapType.End, Label = "Centre" };

                // 3. Milieu de l'arc (Géométrique sur la courbe)
                Vector2 midPoint = CalculateArcMidpoint(arc);
                if (Vector2.Distance(mouse, midPoint) < threshold)
                    return new SnapResult { WorldPoint = midPoint, Type = SnapType.Mid, Label = "Milieu" };
            }
        }
        return null;
    }
    // Helper pour calculer le point milieu réel sur la courbe de l'arc
    private Vector2 CalculateArcMidpoint(ArcEntity arc)
    {
        double startAngle = Math.Atan2(arc.StartPoint.Y - arc.Center.Y, arc.StartPoint.X - arc.Center.X);
        double endAngle = Math.Atan2(arc.EndPoint.Y - arc.Center.Y, arc.EndPoint.X - arc.Center.X);

        double sweep = endAngle - startAngle;
        while (sweep < 0) sweep += 2 * Math.PI;

        double midAngle = startAngle + (sweep / 2.0);
        return new Vector2(
            arc.Center.X + (float)(arc.Radius * Math.Cos(midAngle)),
            arc.Center.Y + (float)(arc.Radius * Math.Sin(midAngle))
        );
    }
    // Cette méthode vérifie si la souris est proche d'une intersection entre deux entités (lignes ou cercles) et retourne le point d'intersection le plus proche.
    private SnapResult? GetIntersectionSnap(Vector2 mouse, List<Entity> entities, float threshold)
    {
        var atomicList = GetAllAtomicEntities(entities).ToList();

        for (int i = 0; i < atomicList.Count; i++)
        {
            for (int j = i + 1; j < atomicList.Count; j++)
            {
                Entity e1 = atomicList[i];
                Entity e2 = atomicList[j];

                // --- CAS A : LIGNE / LIGNE --- (Déjà fait)
                if (e1 is LineEntity l1 && e2 is LineEntity l2)
                {
                    if (GeometryUtils.FindIntersection(l1.Start, l1.End, l2.Start, l2.End, out Vector2 inter))
                    {
                        if (Vector2.Distance(mouse, inter) < threshold)
                            return new SnapResult { WorldPoint = inter, Type = SnapType.End, Label = "Intersection" };
                    }
                }
                // --- CAS B : LIGNE / CERCLE --- (Déjà fait)
                else if ((e1 is LineEntity || e2 is LineEntity) && (e1 is CircleEntity || e2 is CircleEntity))
                {
                    LineEntity line = (e1 is LineEntity) ? (LineEntity)e1 : (LineEntity)e2;
                    CircleEntity circ = (e1 is CircleEntity) ? (CircleEntity)e1 : (CircleEntity)e2;

                    var points = GeometryUtils.FindLineCircleIntersections(line.Start, line.End, circ.Center, circ.Radius);
                    foreach (var pt in points)
                    {
                        if (Vector2.Distance(mouse, pt) < threshold)
                            return new SnapResult { WorldPoint = pt, Type = SnapType.End, Label = "Intersection" };
                    }
                }
                // --- CAS C : CERCLE / CERCLE --- (LE NOUVEAU !)
                else if (e1 is CircleEntity c1 && e2 is CircleEntity c2)
                {
                    var points = GeometryUtils.FindCircleCircleIntersections(c1.Center, c1.Radius, c2.Center, c2.Radius);
                    foreach (var pt in points)
                    {
                        if (Vector2.Distance(mouse, pt) < threshold)
                            return new SnapResult { WorldPoint = pt, Type = SnapType.End, Label = "Intersection" };
                    }
                }
            }
        }
        return null;
    }
    // Cette méthode calcule le point de snap perpendiculaire à une entité (ligne ou cercle) depuis un point de départ donné.
    private SnapResult? GetPerpendicularSnap(Vector2 mouse, Vector2 start, List<Entity> entities, float threshold)
    {
        // 1. On récupère toutes les entités de base (aplatissement des groupes)
        var atomicEntities = GetAllAtomicEntities(entities);

        foreach (var entity in atomicEntities)
        {
            // --- CAS DE LA LIGNE ---
            if (entity is LineEntity line)
            {
                // La perpendiculaire est la projection orthogonale du point 'start' sur la ligne
                Vector2 perp = GeometryUtils.ProjectPointOnLine(start, line.Start, line.End);

                if (Vector2.Distance(mouse, perp) < threshold)
                    return new SnapResult { WorldPoint = perp, Type = SnapType.End, Label = "Perpendiculaire" };
            }

            // --- CAS DU CERCLE ---
            else if (entity is CircleEntity circle)
            {
                // La perpendiculaire à un cercle depuis un point 'start' est le point 
                // sur la circonférence situé sur la ligne passant par 'start' et le Centre.
                // (C'est là que le rayon est normal à la tangente)

                Vector2 dir = Vector2.Normalize(start - circle.Center);

                // Sécurité : si le point de départ est pile sur le centre, la direction est indéfinie
                if (!float.IsNaN(dir.X))
                {
                    // Il y a deux points perpendiculaires possibles sur un cercle
                    Vector2 p1 = circle.Center + dir * circle.Radius;
                    Vector2 p2 = circle.Center - dir * circle.Radius;

                    if (Vector2.Distance(mouse, p1) < threshold)
                        return new SnapResult { WorldPoint = p1, Type = SnapType.End, Label = "Perpendiculaire" };

                    if (Vector2.Distance(mouse, p2) < threshold)
                        return new SnapResult { WorldPoint = p2, Type = SnapType.End, Label = "Perpendiculaire" };
                }
            }
        }
        return null;
    }
    // Cette méthode vérifie si la souris est proche d'une entité (ligne ou cercle) et retourne le point le plus proche sur cette entité.
    private SnapResult? GetOnEntitySnap(Vector2 mouse, List<Entity> entities, float threshold)
    {
        // 1. On "aplatit" la structure pour voir les lignes et cercles cachés dans les groupes
        var atomicEntities = GetAllAtomicEntities(entities);

        foreach (var entity in atomicEntities)
        {
            // --- CAS DE LA LIGNE ---
            if (entity is LineEntity line)
            {
                // On projette la souris sur le segment pour trouver le point le plus proche
                Vector2 nearest = GeometryUtils.ProjectPointOnLine(mouse, line.Start, line.End);

                if (Vector2.Distance(mouse, nearest) < threshold)
                {
                    return new SnapResult
                    {
                        WorldPoint = nearest,
                        Type = SnapType.Alignment,
                        Label = "Sur la ligne"
                    };
                }
            }

            // --- CAS DU CERCLE ---
            else if (entity is CircleEntity circle)
            {
                float distToCenter = Vector2.Distance(mouse, circle.Center);

                // Si la souris est proche de la circonférence (rayon)
                if (Math.Abs(distToCenter - circle.Radius) < threshold)
                {
                    // On calcule le vecteur direction entre le centre et la souris
                    Vector2 dir = Vector2.Normalize(mouse - circle.Center);

                    // Sécurité si la souris est exactement sur le centre (évite une division par zéro)
                    if (float.IsNaN(dir.X)) dir = Vector2.UnitX;

                    // Le point exact sur le cercle est : Centre + (Direction normalisée * Rayon)
                    Vector2 nearest = circle.Center + dir * circle.Radius;

                    return new SnapResult
                    {
                        WorldPoint = nearest,
                        Type = SnapType.Alignment,
                        Label = "Sur le cercle"
                    };
                }
            }
            // --- CAS DE L'ARC ---
            if (entity is ArcEntity arc)
            {
                float distToCenter = Vector2.Distance(mouse, arc.Center);
                if (Math.Abs(distToCenter - arc.Radius) < threshold)
                {
                    // On projette la souris sur le cercle imaginaire
                    Vector2 dir = Vector2.Normalize(mouse - arc.Center);
                    Vector2 pointOnCircle = arc.Center + dir * arc.Radius;

                    // On vérifie si ce point appartient à la portion d'arc
                    if (arc.IsPointOnArc(pointOnCircle, threshold))
                    {
                        return new SnapResult { WorldPoint = pointOnCircle, Type = SnapType.Alignment, Label = "Sur arc" };
                    }
                }
            }
        }
        return null;
    }
    // Cette méthode extrait toutes les lignes et cercles, même s'ils sont dans des groupes
    private IEnumerable<Entity> GetAllAtomicEntities(List<Entity> entities)
    {
        foreach (var e in entities)
        {
            if (e is GroupEntity g)
            {
                // Récursivité : on va chercher les petits enfants
                foreach (var child in GetAllAtomicEntities(g.Children))
                    yield return child;
            }
            else
            {
                yield return e;
            }
        }
    }

    private void UpdateMemory(Vector2 pt)
    {
        if (!_inferenceMemory.Any(p => Vector2.Distance(p, pt) < 0.001f))
        {
            _inferenceMemory.Add(pt);
            if (_inferenceMemory.Count > MaxMemory) _inferenceMemory.RemoveAt(0);
        }
    }
}

