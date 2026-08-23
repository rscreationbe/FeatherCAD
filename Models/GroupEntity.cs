using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace FeatherCAD.Models
{
    public class GroupEntity : Entity
    {
        // La liste des entités à l'intérieur du groupe
        public List<Entity> Children { get; set; } = new List<Entity>();
        // Méthode pour déplacer le groupe et tous ses enfants
        public override void Move(Vector2 delta)
        {
            foreach (var child in Children) child.Move(delta);
        }
        // Méthode pour dessiner le groupe et ses enfants
        public override void Draw(DrawingContext dc, Func<Vector2, Point> worldToScreen, bool isSelected)
        {
            // Un groupe se dessine en demandant à tous ses enfants de se dessiner
            // Si le groupe est sélectionné, on passe 'true' à tous ses enfants
            foreach (var child in Children)
            {
                child.Draw(dc, worldToScreen, isSelected);
            }
        }
        // Méthode pour vérifier si un point est à l'intérieur du groupe (ou proche de ses enfants) 
        public bool IsPointInside(Vector2 mousePos, float threshold)
        {
            foreach (var child in Children)
            {
                // 1. CAS D'UNE LIGNE : On vérifie si le point est proche de la ligne
                if (child is LineEntity l && GeometryUtils.IsPointNearLine(mousePos, l.Start, l.End, threshold))
                    return true;
                // 2. CAS D'UN CERCLE : On vérifie si le point est proche du cercle
                if (child is CircleEntity c && Math.Abs(Vector2.Distance(mousePos, c.Center) - c.Radius) < threshold)
                    return true;
                // 3. CAS D'UN ARC : On vérifie si le point est proche de l'arc
                if (child is ArcEntity a)
                {
                    float distToCenter = Vector2.Distance(mousePos, a.Center);
                    if (Math.Abs(distToCenter - a.Radius) < threshold && a.IsPointOnArc(mousePos, threshold))
                        return true;
                }

                // Récursivité : si l'enfant est lui-même un groupe
                if (child is GroupEntity g && g.IsPointInside(mousePos, threshold))
                    return true;
            }
            return false;
        }
    }
}