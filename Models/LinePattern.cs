using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Newtonsoft.Json;

namespace FeatherCAD.Models
{
    public class LinePattern
    {
        public string Name { get; set; } = "Plein";

        // Liste brute des longueurs [trait, espace, trait, espace...]
        public List<double> Dashes { get; set; } = new();

        public LinePattern() { }

        // Constructeur polyvalent (accepte tableaux ou listes)
        public LinePattern(string name, IEnumerable<double> dashes)
        {
            Name = name;
            Dashes = dashes?.ToList() ?? new List<double>();
        }

        // Pour l'affichage dans les menus (ex: "Axe — . —")
        [JsonIgnore]
        public string DisplayPattern => Dashes.Count == 0 ? "────────" :
            string.Join("", Dashes.Select((d, i) => i % 2 == 0 ? "—" : "·"));

        // Conversion sécurisée pour WPF
        [JsonIgnore]
        public DashStyle WpfDashStyle
        {
            get
            {
                if (Dashes == null || Dashes.Count == 0) return DashStyles.Solid;
                // DoubleCollection est nécessaire pour DashStyle
                var ds = new DashStyle(new DoubleCollection(Dashes), 0);
                ds.Freeze();
                return ds;
            }
        }
    }
}