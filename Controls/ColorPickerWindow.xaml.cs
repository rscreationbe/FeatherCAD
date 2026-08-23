using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FeatherCAD.Controls;

public partial class ColorPickerWindow : Window
{
    // Cette propriété stockera la couleur choisie
    public Color SelectedColor { get; private set; } = Colors.Black;

    public ColorPickerWindow()
    {
        InitializeComponent();
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as System.Windows.Controls.Button;
        if (btn != null && btn.Background is SolidColorBrush brush)
        {
            SelectedColor = brush.Color;
            this.DialogResult = true; // Ferme la fenêtre et renvoie "OK"
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
    }
}
