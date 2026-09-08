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

            Color drawColor = isSelected ? Colors.Red : Color;

            Pen pen = new Pen(new SolidColorBrush(drawColor), Thickness);

            // --- LE CŒUR DE LA CORRECTION D'ÉCHELLE ---
            if (Pattern != null && Pattern.Dashes != null && Pattern.Dashes.Count > 0)
            {
                // RIGOUREUX : On divise chaque valeur du motif par l'épaisseur du trait.
                // Cela annule le multiplicateur automatique de WPF.
                var correctedDashes = Pattern.Dashes.Select(d => d*2 / Thickness).ToList(); 

                // On crée un nouveau DashStyle à la volée pour ce rendu spécifique
                pen.DashStyle = new DashStyle(correctedDashes, 0);
            }
            else
            {
                pen.DashStyle = DashStyles.Solid;
            }

            pen.Freeze(); // Toujours figer pour la performance
            dc.DrawLine(pen, p1, p2);
        }
    }
}