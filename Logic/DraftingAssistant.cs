using FeatherCAD.Models;
using FeatherCAD.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace FeatherCAD.Logic
{
    //public enum SnapType { None, End, Mid, Alignment }

    //public struct AlignmentLine
    //{
    //    public Vector2 Origin;
    //    public Vector2 Direction;
    //    public string Label;
    //}

    //public struct SnapResult
    //{
    //    public Vector2 WorldPoint;
    //    public SnapType Type;
    //    public string Label;
    //    public List<AlignmentLine> ActiveAlignments;
    //}

    public class DraftingAssistant
    {
        private List<Vector2> _inferenceMemory = new List<Vector2>();
        private const int MaxMemory = 5;

        public float SnapThreshold { get; set; } = 12f;
        public double AngleThreshold { get; set; } = 2.0;

        public void ClearMemory() => _inferenceMemory.Clear();
        public List<Vector2> GetMemory() => _inferenceMemory;

        /// <summary>
        /// Calcule le point magnétique le plus pertinent selon la position de la souris.
        /// </summary>
        public SnapResult CalculateSnap(Vector2 mouseWorld, List<Entity> entities, float zoom, Vector2? tempStartPoint)
        {
            float threshold = SnapThreshold / zoom;
            var atomicEntities = GetAllAtomicEntities(entities).ToList();

            // --- 1. PRIORITÉ : POINTS REMARQUABLES (Fin, Milieu, Centre) ---
            var pointSnap = GetPointSnap(mouseWorld, atomicEntities, threshold);
            if (pointSnap != null) return pointSnap.Value;

            // --- 2. PRIORITÉ : INTERSECTIONS RÉELLES ---
            var interSnap = GetIntersectionSnap(mouseWorld, atomicEntities, threshold);
            if (interSnap != null) return interSnap.Value;

            // --- 3. PRIORITÉ : PERPENDICULAIRE (si on dessine) ---
            if (tempStartPoint.HasValue)
            {
                var perpSnap = GetPerpendicularSnap(mouseWorld, tempStartPoint.Value, atomicEntities, threshold);
                if (perpSnap != null) return perpSnap.Value;
            }

            // --- 4. COLLECTE DES ALIGNEMENTS (MÉMOIRE + ANGULAIRE) ---
            List<AlignmentLine> activeLines = new List<AlignmentLine>();

            // A. Alignements H/V sur mémoire d'inférence
            var refPoints = new List<Vector2>(_inferenceMemory);
            if (tempStartPoint.HasValue) refPoints.Add(tempStartPoint.Value);

            foreach (var pt in refPoints)
            {
                if (Math.Abs(mouseWorld.X - pt.X) < threshold)
                    activeLines.Add(new AlignmentLine { Origin = pt, Direction = Vector2.UnitY, Label = "Vertical" });
                if (Math.Abs(mouseWorld.Y - pt.Y) < threshold)
                    activeLines.Add(new AlignmentLine { Origin = pt, Direction = Vector2.UnitX, Label = "Horizontal" });
            }

            // B. Alignements Angulaires (30, 45, 60...)
            if (tempStartPoint.HasValue)
            {
                var angular = GeometryUtils.GetAngularAlignmentLine(mouseWorld, tempStartPoint.Value, threshold);
                if (angular != null) activeLines.Add(angular.Value);
            }

            // --- 5. INTERSECTION D'ALIGNEMENTS (Croisement de pointillés) ---
            if (activeLines.Count >= 2)
            {
                if (GeometryUtils.IntersectLines(activeLines[0], activeLines[1], out Vector2 inter))
                {
                    if (Vector2.Distance(mouseWorld, inter) < threshold * 2)
                        return new SnapResult { WorldPoint = inter, Type = SnapType.Alignment, Label = "Intersection", ActiveAlignments = activeLines };
                }
            }

            // --- 6. ALIGNEMENT SIMPLE ---
            if (activeLines.Count > 0)
            {
                Vector2 snapped = GeometryUtils.ProjectPointOnInfiniteLine(mouseWorld, activeLines[0].Origin, activeLines[0].Direction);
                return new SnapResult { WorldPoint = snapped, Type = SnapType.Alignment, Label = activeLines[0].Label, ActiveAlignments = new List<AlignmentLine> { activeLines[0] } };
            }

            // --- 7. DERNIER RECOURS : AIMANTATION SUR L'OBJET (Glissement) ---
            var onEntitySnap = GetOnEntitySnap(mouseWorld, atomicEntities, threshold);
            if (onEntitySnap != null) return onEntitySnap.Value;

            return new SnapResult { Type = SnapType.None, ActiveAlignments = new List<AlignmentLine>() };
        }

        private SnapResult? GetPointSnap(Vector2 mouse, List<Entity> entities, float threshold)
        {
            foreach (var entity in entities)
            {
                var points = new List<(Vector2 pt, string label)>();

                if (entity is LineEntity l)
                {
                    points.Add((l.Start, "Fin"));
                    points.Add((l.End, "Fin"));
                    points.Add(((l.Start + l.End) / 2, "Milieu"));
                }
                else if (entity is CircleEntity c)
                {
                    points.Add((c.Center, "Centre"));
                    points.Add((c.Center + new Vector2(c.Radius, 0), "Quadrant"));
                    points.Add((c.Center + new Vector2(-c.Radius, 0), "Quadrant"));
                    points.Add((c.Center + new Vector2(0, c.Radius), "Quadrant"));
                    points.Add((c.Center + new Vector2(0, -c.Radius), "Quadrant"));
                }
                else if (entity is ArcEntity a)
                {
                    points.Add((a.StartPoint, "Fin"));
                    points.Add((a.EndPoint, "Fin"));
                    points.Add((a.Center, "Centre"));
                    points.Add((CalculateArcMidpoint(a), "Milieu"));
                }

                foreach (var p in points)
                {
                    if (Vector2.Distance(mouse, p.pt) < threshold)
                    {
                        UpdateMemory(p.pt);
                        return new SnapResult { WorldPoint = p.pt, Type = SnapType.End, Label = p.label };
                    }
                }
            }
            return null;
        }

        private SnapResult? GetIntersectionSnap(Vector2 mouse, List<Entity> entities, float threshold)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                for (int j = i + 1; j < entities.Count; j++)
                {
                    var pts = GeometryUtils.GetEntitiesIntersections(entities[i], entities[j]);
                    foreach (var pt in pts)
                    {
                        if (Vector2.Distance(mouse, pt) < threshold)
                            return new SnapResult { WorldPoint = pt, Type = SnapType.End, Label = "Intersection" };
                    }
                }
            }
            return null;
        }

        private SnapResult? GetPerpendicularSnap(Vector2 mouse, Vector2 start, List<Entity> entities, float threshold)
        {
            foreach (var entity in entities)
            {
                Vector2? perp = null;
                if (entity is LineEntity l) perp = GeometryUtils.ProjectPointOnLine(start, l.Start, l.End);
                else if (entity is CircleEntity c) perp = c.Center + Vector2.Normalize(start - c.Center) * c.Radius;
                else if (entity is ArcEntity a)
                {
                    var p = a.Center + Vector2.Normalize(start - a.Center) * a.Radius;
                    if (a.IsPointOnArc(p, threshold)) perp = p;
                }

                if (perp.HasValue && Vector2.Distance(mouse, perp.Value) < threshold)
                    return new SnapResult { WorldPoint = perp.Value, Type = SnapType.End, Label = "Perpendiculaire" };
            }
            return null;
        }

        private SnapResult? GetOnEntitySnap(Vector2 mouse, List<Entity> entities, float threshold)
        {
            foreach (var entity in entities)
            {
                Vector2? nearest = null;
                if (entity is LineEntity l) nearest = GeometryUtils.ProjectPointOnLine(mouse, l.Start, l.End);
                else if (entity is CircleEntity c) nearest = c.Center + Vector2.Normalize(mouse - c.Center) * c.Radius;
                else if (entity is ArcEntity a)
                {
                    var p = a.Center + Vector2.Normalize(mouse - a.Center) * a.Radius;
                    if (a.IsPointOnArc(p, threshold)) nearest = p;
                }

                if (nearest.HasValue && Vector2.Distance(mouse, nearest.Value) < threshold)
                    return new SnapResult { WorldPoint = nearest.Value, Type = SnapType.Alignment, Label = "Sur l'objet" };
            }
            return null;
        }

        private Vector2 CalculateArcMidpoint(ArcEntity arc)
        {
            double sAngle = Math.Atan2(arc.StartPoint.Y - arc.Center.Y, arc.StartPoint.X - arc.Center.X);
            double eAngle = Math.Atan2(arc.EndPoint.Y - arc.Center.Y, arc.EndPoint.X - arc.Center.X);
            double sweep = eAngle - sAngle;
            while (sweep < 0) sweep += 2 * Math.PI;
            double mid = sAngle + (sweep / 2.0);
            return arc.Center + new Vector2((float)(arc.Radius * Math.Cos(mid)), (float)(arc.Radius * Math.Sin(mid)));
        }

        private IEnumerable<Entity> GetAllAtomicEntities(List<Entity> entities)
        {
            foreach (var e in entities)
            {
                if (e is GroupEntity g) foreach (var child in GetAllAtomicEntities(g.Children)) yield return child;
                else yield return e;
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
}