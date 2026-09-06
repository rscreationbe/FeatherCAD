using FeatherCAD.Logic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace FeatherCAD.Models
{
    public class Layer : INotifyPropertyChanged
    {
        private string _name = "Calque 1";
        private bool _isVisible = true;
        private bool _isLocked = false;
        private bool _isActiveLayer;
        private Color _color = Colors.Black;
        private double  _thickness = 0.5;
        //private DashStyle _dashStyle = DashStyles.Solid;
        private LinePattern _linePattern = LinePatternManager.Patterns.First();
        public LinePattern LinePattern
        {
            get => _linePattern;
            set { _linePattern = value; OnPropertyChanged(); }
        }


        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        public bool IsLocked
        {
            get => _isLocked;
            set { _isLocked = value; OnPropertyChanged(); }
        }

        public Color Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public double Thickness
        {
            get => _thickness;
            set
            {
                if (Math.Abs(_thickness - value) < 0.0001) return; // Évite les boucles infinies
                _thickness = value;
                OnPropertyChanged();
            }
        }

        //public DashStyle DashStyle
        //{
        //    get { return _dashStyle; }
        //    set { _dashStyle = value; OnPropertyChanged(); }
        //}

        public bool IsActiveLayer
        {
            get => _isActiveLayer;
            set
            {
                if (_isActiveLayer != value)
                {
                    _isActiveLayer = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    string oldName = _name;
                    _name = value;
                    OnPropertyChanged();
                    // On déclenche un événement personnalisé pour le Canvas
                    Renamed?.Invoke(this, (oldName, value));
                }
            }
        }

        // Événement spécifique pour notifier le renommage (AncienNom, NouveauNom)
        public event EventHandler<(string OldName, string NewName)>? Renamed;
        // Implémentation de INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        // Méthode pour déclencher l'événement PropertyChanged
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}