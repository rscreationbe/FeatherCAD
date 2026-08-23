using System.Windows.Media;
using System.Numerics;
using System.Windows;

namespace FeatherCAD.Models
{
    public class LineEntity : Entity
    {
        // Une ligne est définie par deux points dans le "Monde"
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }

        
        // Constructeur pour faciliter la création
        public LineEntity(Vector2 start, Vector2 end, Color color, double thickness = 0.5)
        {
            Start = start;
            End = end;
            Color = color;
            Thickness = thickness;
        }
        // Déplace la ligne en décalant ses deux points
        public override void Move(Vector2 delta)
        {
            Start += delta;
            End += delta;
        }

        // Implémentation réelle du dessin
        public override void Draw(DrawingContext dc, Func<Vector2, Point> worldToScreen, bool isSelected)
        {
            Point p1 = worldToScreen(Start);
            Point p2 = worldToScreen(End);

            // Si sélectionné, on force le Bleu, sinon on prend la couleur de l'entité
            Color drawColor = isSelected ? Colors.Red : this.Color;

            // Si sélectionné, on peut aussi épaissir un peu le trait
            //double thickness = isSelected ? this.Thickness + 1 : this.Thickness;

            Pen pen = new Pen(new SolidColorBrush(drawColor), Thickness);
            if (DashStyle != null) pen.DashStyle = DashStyle;

            pen.Freeze();
            dc.DrawLine(pen, p1, p2);
        }
    }
}