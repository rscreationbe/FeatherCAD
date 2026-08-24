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
        private Color _color = Colors.Black;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
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
        private bool _isActiveLayer;
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}