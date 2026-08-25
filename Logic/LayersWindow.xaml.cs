using FeatherCAD.Controls;
using FeatherCAD.Models;
using System.Windows;
using System.Windows.Controls;

namespace FeatherCAD
{
    public partial class LayersWindow : Window
    {
        private CadCanvas _canvas;

        public LayersWindow(CadCanvas canvas)
        {
            InitializeComponent();
            _canvas = canvas;
            LayersGrid.ItemsSource = _canvas.Layers;
        }

        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            // On crée l'objet
            var newLayer = new Layer
            {
                Name = $"Calque {_canvas.Layers.Count + 1}",
                Color = System.Windows.Media.Colors.Black // Couleur par défaut
            };

            // RIGOUREUX : On utilise une méthode du Canvas pour l'ajouter 
            // afin qu'il puisse s'abonner aux événements du calque.
            _canvas.AddLayer(newLayer);
        }

        private void DeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            if (LayersGrid.SelectedItem is Layer layer)
            {
                if (_canvas.Layers.Count > 1 && _canvas.ActiveLayer != layer)
                {
                    _canvas.Layers.Remove(layer);
                    _canvas.InvalidateVisual();
                }
                else
                {
                    MessageBox.Show("Impossible de supprimer le calque actif ou le dernier calque.");
                }
            }
        }

        private void ActivateLayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.DataContext is Layer selectedLayer)
            {
                // 1. Mettre à jour le Canvas
                _canvas.ActiveLayer = selectedLayer;

                // 2. Assurer l'exclusivité dans la collection (Rigueur des données)
                foreach (var layer in _canvas.Layers)
                {
                    layer.IsActiveLayer = (layer == selectedLayer);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}