using System.Windows.Media; // Pour la couleur
using System.Numerics;      // Pour Vector2 (nécessite .NET 6+)
using System.Windows;       // Pour le type Point de WPF
using FeatherCAD.Logic;

namespace FeatherCAD.Models;

// "abstract" signifie qu'on ne peut pas créer un objet "Entity" tout seul.
// On doit créer des sous-classes (Line, Circle, etc.)
public abstract class Entity
{
    public Color Color { get; set; } = Colors.Black;
    public double Thickness { get; set; } = 0.5;
    //public DashStyle? DashStyle { get; set; } = null;
    public string LayerName { get; set; } = "Calque 1";
    // Pattern est un objet de type LinePattern qui représente le motif de ligne de l'entité.
    public LinePattern Pattern { get; set; } = LinePatternManager.Patterns.First();




    // méthode abstraite pour déplacer l'entité, à implémenter dans les sous-classes
    public abstract void Move(Vector2 delta);
    // méthode abstraite pour dessiner l'entité, à implémenter dans les sous-classes
    public abstract void Draw(DrawingContext dc, Func<Vector2, Point> worldToScreen, bool isSelected);
}
