using System;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace FeatherCAD.Models
{
    public class ArcEntity : Entity
    {
        public Vector2 Center { get; set; }
        public Vector2 StartPoint { get; set; }
        public Vector2 EndPoint { get; set; }
        public float Radius => Vector2.Distance(Center, StartPoint);

        public ArcEntity(Vector2 center, Vector2 start, Vector2 end, Color color, double thickness)
        {
            Center = center;
            StartPoint = start;
            EndPoint = end;
            Color = color;
            Thickness = thickness;
        }

        public override void Draw(DrawingContext dc, Func<Vector2, Point> worldToScreen, bool isSelected)
        {
            float radius = Vector2.Distance(Center, StartPoint);
            if (radius < 0.01f) return;

            // Calcul des angles en radians
            double startAngle = Math.Atan2(StartPoint.Y - Center.Y, StartPoint.X - Center.X);
            double endAngle = Math.Atan2(EndPoint.Y - Center.Y, EndPoint.X - Center.X);

            // Calcul du balayage (sens trigonométrique inverse pour WPF / CCW pour CAD)
            double sweep = endAngle - startAngle;
            while (sweep < 0) sweep += 2 * Math.PI;

            // Conversion en points écran
            Point pStart = worldToScreen(StartPoint);
            Point pEnd = worldToScreen(EndPoint);
            Point pCenter = worldToScreen(Center);
            double screenRadius = Vector2.Distance(new Vector2((float)pCenter.X, (float)pCenter.Y),
                                                 new Vector2((float)pStart.X, (float)pStart.Y));

            // Construction de la géométrie de l'arc
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(pStart, false, false);
                ctx.ArcTo(pEnd,
                          new Size(screenRadius, screenRadius),
                          0,
                          sweep > Math.PI, // Large Arc Flag
                          SweepDirection.Clockwise,
                          true,
                          false);
            }

            Color drawColor = isSelected ? Colors.Red : Color;
            Pen pen = new Pen(new SolidColorBrush(drawColor), Thickness);
            if (DashStyle != null) pen.DashStyle = DashStyle;
            pen.Freeze();

            dc.DrawGeometry(null, pen, geometry);
        }
        public bool IsPointOnArc(Vector2 pos, float threshold)
        {
            // 1. Calcul des angles de base (en radians)
            double startAngle = Math.Atan2(StartPoint.Y - Center.Y, StartPoint.X - Center.X);
            double endAngle = Math.Atan2(EndPoint.Y - Center.Y, EndPoint.X - Center.X);
            double currentAngle = Math.Atan2(pos.Y - Center.Y, pos.X - Center.X);

            // 2. Normalisation des angles entre 0 et 2π
            startAngle = NormalizeAngle(startAngle);
            endAngle = NormalizeAngle(endAngle);
            currentAngle = NormalizeAngle(currentAngle);

            // 3. Calcul de l'angle de balayage (Sweep)
            // Dans FeatherCAD, nous avons défini que l'arc tourne dans le sens horaire (WPF)
            double sweepAngle = endAngle - startAngle;
            if (sweepAngle < 0) sweepAngle += 2 * Math.PI;

            // 4. Calcul de l'angle entre le début et la souris
            double angleToMouse = currentAngle - startAngle;
            if (angleToMouse < 0) angleToMouse += 2 * Math.PI;

            // 5. Le point est sur l'arc si son angle relatif est inférieur au balayage total
            // On ajoute une petite tolérance angulaire basée sur le threshold
            double angularThreshold = threshold / Radius;
            return angleToMouse <= (sweepAngle + angularThreshold);
        }

        // Helper pour garder les angles entre 0 et 2π
        private double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }
        public override void Move(Vector2 delta)
        {
            Center += delta;
            StartPoint += delta;
            EndPoint += delta;
        }
    }
}