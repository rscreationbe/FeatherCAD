using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Newtonsoft.Json;

namespace FeatherCAD.Models
{
    public class LinePattern
    {
        public string Name { get; set; } = "Plein";

        // La séquence : [longueur trait, longueur espace, longueur trait, longueur espace...]
        public List<double> Dashes { get; set; } = new List<double>();

        public LinePattern() { }

        public LinePattern(string name, params double[] dashes)
        {
            Name = name;
            Dashes = dashes.ToList();
        }

        // --- LA PROPRIÉTÉ MANQUANTE POUR VOTRE ERREUR ---
        [JsonIgnore]
        public string DisplayPattern
        {
            get
            {
                if (Dashes == null || Dashes.Count == 0) return "────────";

                // On transforme la liste [10, 2, 2, 2] en une chaîne de symboles visuels
                // On limite à quelques répétitions pour ne pas surcharger le menu
                return string.Join(" ", Dashes.Select((d, i) => i % 2 == 0 ? "—" : "·"));
            }
        }

        [JsonIgnore]
        public DashStyle WpfDashStyle
        {
            get
            {
                if (Dashes == null || Dashes.Count == 0) return DashStyles.Solid;
                // DoubleCollection convertit notre List<double> pour WPF
                var ds = new DashStyle(new DoubleCollection(Dashes), 0);
                ds.Freeze();
                return ds;
            }
        }

        // --- MOTIFS STANDARDS (Statiques pour un accès facile) ---
        public static LinePattern Solid => new LinePattern("Plein");
        public static LinePattern Dashed => new LinePattern("Pointillé", 4, 2);
        public static LinePattern Dotted => new LinePattern("Points", 1, 2);
        public static LinePattern Axis => new LinePattern("Axe", 10, 2, 2, 2);
        public static LinePattern Phantom => new LinePattern("Fantôme", 10, 2, 2, 2, 2, 2);
    }
}