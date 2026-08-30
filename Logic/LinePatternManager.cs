using System.Collections.ObjectModel;
using FeatherCAD.Models;

namespace FeatherCAD.Logic
{
    public static class LinePatternManager
    {
        // On utilise "Patterns" comme nom unique et clair
        public static ObservableCollection<LinePattern> Patterns { get; } = new ObservableCollection<LinePattern>
        {
            LinePattern.Solid,
            new LinePattern("Pointillé", new double[] { 4, 2 }),
            new LinePattern("Point", new double[] { 1, 2 }),
            LinePattern.Axis,
            new LinePattern("Fantôme", new double[] { 10, 2, 2, 2, 2, 2 })
        };

        public static void AddPattern(LinePattern pattern)
        {
            if (pattern != null)
            {
                // Ici, "Patterns" correspond bien au nom de la propriété au-dessus
                Patterns.Add(pattern);
            }
        }
    }
}