using FeatherCAD.Controls;
using FeatherCAD.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FeatherCAD
{
    public partial class LayersWindow : Window
    {
        private CadCanvas _canvas;
        // Constructeur
        public LayersWindow(CadCanvas canvas)
        {
            InitializeComponent();
            _canvas = canvas;
            LayersGrid.ItemsSource = _canvas.Layers;
        }
        // Méthode bouton ajouter un calque
        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            // On crée l'objet
            var newLayer = new Layer
            {
                Name = $"Calque {_canvas.Layers.Count + 1}",
            };

            _canvas.AddLayer(newLayer);
        }
        // Méthode bouton supprimer un calque
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
        // Méthode pour activer un calque
        private void ActivateLayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.DataContext is Layer selectedLayer)
            {
                // On définit le calque actif dans le canvas
                // C'est ce "SET" qui va déclencher la boucle de synchronisation du point 1
                _canvas.ActiveLayer = selectedLayer;
            }
            _canvas.InvalidateVisual();
        }
        //        
        private void Close_Click(object sender, RoutedEventArgs e) 
        {
            _canvas.InvalidateVisual();
            Close(); 
        }
    }
}