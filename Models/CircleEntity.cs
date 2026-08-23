using System;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace FeatherCAD.Models;

public class CircleEntity : Entity
{
    public Vector2 Center { get; set; }
    public float Radius { get; set; }

    private Vector2 pt1, pt2, pt3;


    public Vector2 Pt1
    {
        get { return pt1; }
        set { pt1 = value; }
    }

    public Vector2 Pt2
    {
        get { return pt2; }
        set { pt2 = value; }
    }

    public Vector2 Pt3
    {
        get { return pt3; }
        set { pt3 = value; }
    }

    public CircleEntity(Vector2 center, float radius, Color color, double thickness = 0.5)
    {
        Center = center;
        Radius = radius;
        Color = color;
        Thickness = thickness;
    }
    public CircleEntity() 
    {
        Center = Vector2.Zero;
        Radius = 0;
        Color = Colors.Black;
        Thickness = 0.5;
    }

    public override void Move(Vector2 delta)
    {
        Center += delta;
        if (Pt1 != Vector2.Zero) Pt1 += delta;
        if (Pt2 != Vector2.Zero) Pt2 += delta;
        if (Pt3 != Vector2.Zero) Pt3 += delta;
    }

    public override void Draw(DrawingContext dc, Func<Vector2, Point> worldToScreen, bool isSelected)
    {
        Point screenCenter = worldToScreen(Center);
        Point edgePoint = worldToScreen(Center + new Vector2(Radius, 0));
        double screenRadius = Math.Sqrt(Math.Pow(edgePoint.X - screenCenter.X, 2) + Math.Pow(edgePoint.Y - screenCenter.Y, 2));

        // Logique de couleur identique
        Color drawColor = isSelected ? Colors.Red : this.Color;
        //double thickness = isSelected ? this.Thickness + 1 : this.Thickness;

        Pen pen = new Pen(new SolidColorBrush(drawColor), Thickness);
        pen.Freeze();

        dc.DrawEllipse(null, pen, screenCenter, screenRadius, screenRadius);
    }
}