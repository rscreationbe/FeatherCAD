using FeatherCAD.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace FeatherCAD.Logic
{
    public static class FileManager
    {
        // Configuration pour gérer le polymorphisme (Lignes, Cercles, Arcs dans la même liste)
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Newtonsoft.Json.Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = {
        new ColorJsonConverter() // Utilisez le nouveau nom ici
    }
        };
        // Méthode pour sauvegarder un projet dans un fichier .fcad
        public static void Save(CadProject project)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "FeatherCAD File (*.fcad)|*.fcad",
                Title = "Enregistrer le dessin"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(project, Settings);
                    File.WriteAllText(dlg.FileName, json);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}");
                }
            }
        }
        // Méthode pour ouvrir un projet depuis un fichier .fcad
        public static CadProject? Open()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "FeatherCAD File (*.fcad)|*.fcad",
                Title = "Ouvrir un dessin"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dlg.FileName);
                    return JsonConvert.DeserializeObject<CadProject>(json, Settings);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Erreur lors de l'ouverture : {ex.Message}");
                }
            }
            return null;
        }
    }
}