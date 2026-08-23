using FeatherCAD.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FeatherCAD.Controls
{
    public partial class FlyoutButton : UserControl
    {
        private DispatcherTimer _timer;
        private bool _isPopupOpen = false;

        // --- DÉPENDENCY PROPERTIES (Permettent l'utilisation dans le XAML) ---

        public static readonly DependencyProperty MainIconProperty =
            DependencyProperty.Register("MainIcon", typeof(ImageSource), typeof(FlyoutButton), new PropertyMetadata(null));

        public ImageSource MainIcon
        {
            get { return (ImageSource)GetValue(MainIconProperty); }
            set { SetValue(MainIconProperty, value); }
        }

        public static readonly DependencyProperty MainTagProperty =
            DependencyProperty.Register("MainTag", typeof(string), typeof(FlyoutButton), new PropertyMetadata(""));

        public string MainTag
        {
            get { return (string)GetValue(MainTagProperty); }
            set { SetValue(MainTagProperty, value); }
        }

        public static readonly DependencyProperty SubItemsProperty =
            DependencyProperty.Register("SubItems", typeof(List<SubToolItem>), typeof(FlyoutButton), new PropertyMetadata(new List<SubToolItem>()));

        public List<SubToolItem> SubItems
        {
            get { return (List<SubToolItem>)GetValue(SubItemsProperty); }
            set { SetValue(SubItemsProperty, value); }
        }

        // Événement pour la MainWindow
        public event EventHandler<string> ToolSelected;

        public FlyoutButton()
        {
            InitializeComponent();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            _isPopupOpen = true;
            SubMenuPopup.IsOpen = true;
        }

        private void MainBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isPopupOpen = false;
                _timer.Start();
            }
        }

        private void MainBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _timer.Stop();
            if (!_isPopupOpen)
            {
                ToolSelected?.Invoke(this, MainTag);
            }
        }

        private void SubItem_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                // 1. On récupère l'objet de données lié à ce bouton (le SubToolItem)
                var selectedSubTool = btn.DataContext as SubToolItem;

                if (selectedSubTool != null)
                {
                    // 2. ON MET À JOUR LE BOUTON PRINCIPAL
                    // Grâce aux DependencyProperties, l'image changera toute seule dans l'interface
                    this.MainIcon = selectedSubTool.Icon;
                    this.MainTag = selectedSubTool.Tag;

                    // 3. On prévient la MainWindow que l'outil a changé
                    ToolSelected?.Invoke(this, this.MainTag);
                }

                // 4. On ferme le popup
                SubMenuPopup.IsOpen = false;
            }
        }
    }
}