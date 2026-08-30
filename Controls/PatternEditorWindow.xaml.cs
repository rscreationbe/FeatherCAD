using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FeatherCAD.Models;
using FeatherCAD.Logic;

namespace FeatherCAD.Controls;

public partial class PatternEditorWindow : Window
{
    public LinePattern? CreatedPattern { get; private set; }

    public PatternEditorWindow()
    {
        InitializeComponent();
        UpdatePreview();
    }

    private void TxtSequence_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (PreviewLine == null) return;

        try
        {
            var dashes = ParseSequence(TxtSequence.Text);
            if (dashes.Any())
            {
                PreviewLine.StrokeDashArray = new DoubleCollection(dashes);
            }
            else
            {
                PreviewLine.StrokeDashArray = null;
            }
        }
        catch
        {
            // On ne fait rien si l'entrée est en cours de saisie et invalide
        }
    }

    private List<double> ParseSequence(string input)
    {
        return input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => double.TryParse(s.Trim(), out double d) ? d : 0)
                    .Where(d => d > 0)
                    .ToList();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("Veuillez donner un nom au motif.");
            return;
        }

        var dashes = ParseSequence(TxtSequence.Text);

        //CreatedPattern = new LinePattern(TxtName.Text, dashes);
        this.DialogResult = true;
    }
}