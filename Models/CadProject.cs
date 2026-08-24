using System.Collections.Generic;
using FeatherCAD.Models;

namespace FeatherCAD.Models
{
    public class CadProject
    {
        // Les calques du projet
        public List<Layer> Layers { get; set; } = new();

        // Les entités (lignes, cercles, etc.)
        public List<Entity> Entities { get; set; } = new();

        // Le nom du calque qui était actif lors de la sauvegarde
        public string ActiveLayerName { get; set; } = "Calque 1";
    }
}