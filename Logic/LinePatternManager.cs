using System.Collections.ObjectModel;
using FeatherCAD.Models;

namespace FeatherCAD.Logic
{
    public static class LinePatternManager
    {
        public static ObservableCollection<LinePattern> Patterns { get; } = new()
        {
            new LinePattern("Plein", new double[] { }),
            new LinePattern("Pointillé", new double[] { 4, 2 }),
            new LinePattern("Point", new double[] { 1, 2 }),
            new LinePattern("Axe", new double[] { 10, 2, 2, 2 }),
            new LinePattern("Fantôme", new double[] { 10, 2, 2, 2, 2, 2 })
        };

        public static void AddPattern(LinePattern pattern)
        {
            if (pattern != null && !Patterns.Any(p => p.Name == pattern.Name))
                Patterns.Add(pattern);
        }
    }
}