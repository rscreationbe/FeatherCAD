using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FeatherCAD.Models;

namespace FeatherCAD.Controls
{
    public partial class FlyoutButton : UserControl
    {
        private DispatcherTimer _timer;
        private bool _isLongPress = false;

        // Gestionnaire d'exclusivité statique
        private static FlyoutButton? _currentOpenButton = null;

        public static readonly DependencyProperty MainIconProperty =
            DependencyProperty.Register("MainIcon", typeof(ImageSource), typeof(FlyoutButton));

        public static readonly DependencyProperty MainTagProperty =
            DependencyProperty.Register("MainTag", typeof(string), typeof(FlyoutButton));

        // Note : On ne met PAS de valeur par défaut ici pour éviter le partage de liste
        public static readonly DependencyProperty SubItemsProperty =
            DependencyProperty.Register("SubItems", typeof(List<SubToolItem>), typeof(FlyoutButton));

        public ImageSource MainIcon { get => (ImageSource)GetValue(MainIconProperty); set => SetValue(MainIconProperty, value); }
        public string MainTag { get => (string)GetValue(MainTagProperty); set => SetValue(MainTagProperty, value); }
        public List<SubToolItem> SubItems { get => (List<SubToolItem>)GetValue(SubItemsProperty); set => SetValue(SubItemsProperty, value); }

        public event EventHandler<string>? ToolSelected;

        public FlyoutButton()
        {
            InitializeComponent();
            // Initialisation d'une liste PROPRE à cette instance
            SubItems = new List<SubToolItem>();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            if (SubItems.Count == 0) return;

            _isLongPress = true;

            // Fermer l'éventuel autre bouton ouvert avant d'ouvrir celui-ci
            if (_currentOpenButton != null && _currentOpenButton != this)
            {
                _currentOpenButton.SubMenuPopup.IsOpen = false;
            }

            SubMenuPopup.IsOpen = true;
            _currentOpenButton = this;
        }

        private void MainBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Si on clique sur un bouton, on ferme d'abord tout ce qui est ouvert ailleurs
                if (_currentOpenButton != null && _currentOpenButton != this)
                {
                    _currentOpenButton.SubMenuPopup.IsOpen = false;
                }

                _isLongPress = false;
                _timer.Start();
            }
        }

        private void MainBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _timer.Stop();
            if (!_isLongPress)
            {
                // Clic court : On sélectionne l'outil et on ferme le popup local
                ToolSelected?.Invoke(this, MainTag);
                SubMenuPopup.IsOpen = false;
            }
        }

        private void SubItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SubToolItem item)
            {
                this.MainIcon = item.Icon;
                this.MainTag = item.Tag;
                ToolSelected?.Invoke(this, item.Tag);
                SubMenuPopup.IsOpen = false;
                _currentOpenButton = null;
            }
        }
    }
}