using System;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using FeatherCAD.Logic;

namespace FeatherCAD.Models
{
    public class RectangleEntity : Entity
    {
        // Les deux points définissant la diagonale
        public Vector2 P1 { get; set; }
        public Vector2 P2 { get; set; }

        // Propriétés calculées pour faciliter le rendu et les calculs
        [JsonIgnore] public float Left => Math.Min(P1.X, P2.X);
        [JsonIgnore] public float Right => Math.Max(P1.X, P2.X);
        [JsonIgnore] public float Top => Math.Min(P1.Y, P2.Y);
        [JsonIgnore] public float Bottom => Math.Max(P1.Y, P2.Y);
        [JsonIgnore] public float Width => Math.Abs(P2.X - P1.X);
        [JsonIgnore] public float Height => Math.Abs(P2.Y - P1.Y);

        public RectangleEntity(Vector2 p1, Vector2 p2, Color color, double thickness)
        {
            P1 = p1;
            P2 = p2;
            Color = color;
            Thickness = thickness;
        }

        public override void Draw(DrawingContext dc, Func<Vector2, Point> worldToScreen, bool isSelected)
        {
            // Conversion des coins du monde vers l'écran
            Point pTopLeft = worldToScreen(new Vector2(Left, Top));
            Point pBottomRight = worldToScreen(new Vector2(Right, Bottom));

            // Création du rectangle WPF
            Rect rect = new Rect(pTopLeft, pBottomRight);

            // Définition de l'apparence (Sélection = Red)
            Color drawColor = isSelected ? Colors.Red : Color;
            Pen pen = new Pen(new SolidColorBrush(drawColor), Thickness);
            if (Pattern != null) pen.DashStyle = Pattern.WpfDashStyle;
            pen.Freeze();

            // Dessin (fond null pour n'avoir que le contour)
            dc.DrawRectangle(null, pen, rect);
        }

        public override void Move(Vector2 delta)
        {
            P1 += delta;
            P2 += delta;
        }

        /// <summary>
        /// Vérifie si la souris est proche des bords du rectangle (pour la sélection)
        /// </summary>
        public bool IsPointOnEdges(Vector2 mousePos, float threshold)
        {
            // Les 4 coins
            Vector2 tl = new Vector2(Left, Top);
            Vector2 tr = new Vector2(Right, Top);
            Vector2 bl = new Vector2(Left, Bottom);
            Vector2 br = new Vector2(Right, Bottom);

            // On vérifie la proximité avec chacun des 4 segments
            return GeometryUtils.IsPointNearLine(mousePos, tl, tr, threshold) || // Haut
                   GeometryUtils.IsPointNearLine(mousePos, tr, br, threshold) || // Droite
                   GeometryUtils.IsPointNearLine(mousePos, br, bl, threshold) || // Bas
                   GeometryUtils.IsPointNearLine(mousePos, bl, tl, threshold);   // Gauche
        }
    }
}
