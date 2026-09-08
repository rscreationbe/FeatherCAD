
using FeatherCAD.Controls;
using FeatherCAD.Logic;
using FeatherCAD.Models;
using FeatherCAD.Tools;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;


namespace FeatherCAD;


public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializePatternMenu();


    MyViewport.MouseMove += MyViewport_MouseMove;

        // --- CLIC 1 ---
        MyViewport.DrawingStarted += (s, e) => {
            // Correction : on utilise la propriété publique TempStartPoint
            var start = MyViewport.TempStartPoint;
            TxtX1.Text = start.X.ToString("F2");
            TxtY1.Text = start.Y.ToString("F2");
        };

        // --- CLIC 2 ---
        MyViewport.DrawingFinished += (s, e) => {
            // CAS DU CERCLE
            if (MyViewport.SelectedEntity is CircleEntity circle)
            {
                if (MyViewport.CurrentTool == CadToolType.Circle_cp)
                {
                    lbDist.Text = "R:"; // On change le label pour rayon
                    TxtDist.Text = circle.Radius.ToString("F2"); // On affiche le rayon
                }
                if (MyViewport.CurrentTool == CadToolType.Circle_2P)
                {
                    lbDist.Text = "D:"; // On change le label pour diamètre
                    TxtDist.Text = (Vector2.Distance(circle.Pt1, circle.Pt2)).ToString("F2"); // On affiche le diamètre
                }


                // On vide les autres cases qui ne servent pas au cercle
                //TxtAngle.Text = "";
                //TxtDX.Text = "0.00";
                //TxtDY.Text = "0.00";

                // FOCUS ET SÉLECTION
                TxtDist.Focus();
                TxtDist.SelectAll();
            }
            // CAS DE LA LIGNE
            else if (MyViewport.SelectedEntity is LineEntity line)
            {
                var metrics = MyViewport.GetCurrentMetrics(line.End);
                TxtDX.Text = metrics.dx.ToString("F2");
                TxtDY.Text = metrics.dy.ToString("F2");
                lbDist.Text = "L:";
                TxtDist.Text = metrics.dist.ToString("F2");
                TxtAngle.Text = metrics.angle.ToString("F2");
                TxtDist.Focus();
                TxtDist.SelectAll();
            }
        };

        MyViewport.ToolChanged += (s, toolType) => {
            UpdateUIForTool(toolType);
        };

        MyViewport.StatusChanged += (s, message) => {
            TxtStatus.Text = message;
        };
        MyViewport.UpdateStatus();
    }

    private void InitializePatternMenu()
    {
        PopulatePatternMenu();
        // On écoute le manager : si un motif est ajouté via l'éditeur, le menu se met à jour
        LinePatternManager.Patterns.CollectionChanged += (s, e) => PopulatePatternMenu();
    }
    private void TxtInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (MyViewport.SelectedEntity == null) return;

            if (float.TryParse(((TextBox)sender).Text, out float val))
            {
                string? tag = ((TextBox)sender).Tag?.ToString();

                // --- CAS DE LA LIGNE ---
                if (MyViewport.SelectedEntity is LineEntity line)
                {
                    //float val = float.Parse(((TextBox)sender).Text);
                    //string? tag = ((TextBox)sender).Tag.ToString();

                    if (tag == "L") // DISTANCE
                    {
                        // Recalcul de End basé sur la nouvelle distance
                        double angleRad = Math.Atan2(line.End.Y - line.Start.Y, line.End.X - line.Start.X);
                        line.End = new Vector2(
                            line.Start.X + (float)(val * Math.Cos(angleRad)),
                            line.Start.Y + (float)(val * Math.Sin(angleRad))
                        );
                    }
                    else if (tag == "A") // ANGLE
                    {
                        // On garde la distance actuelle et on change l'angle
                        float currentDist = Vector2.Distance(line.Start, line.End);
                        double angleRad = val * (Math.PI / 180.0); // Conversion Degrés -> Radians

                        line.End = new Vector2(
                            line.Start.X + (float)(currentDist * Math.Cos(angleRad)),
                            line.Start.Y + (float)(currentDist * Math.Sin(angleRad))
                        );
                    }
                    else if (tag == "DX") // DELTA X
                    {
                        // On modifie X par rapport au départ, on ne touche pas à Y
                        line.End = new Vector2(line.Start.X + val, line.End.Y);
                    }
                    else if (tag == "DY") // DELTA Y
                    {
                        // On modifie Y par rapport au départ, on ne touche pas à X
                        line.End = new Vector2(line.End.X, line.Start.Y + val);
                    }
                }

                // --- CAS DU CERCLE ---
                else if (MyViewport.SelectedEntity is CircleEntity circle) { 
                    if (MyViewport.CurrentTool == CadToolType.Circle_cp || MyViewport.CurrentTool == CadToolType.Circle_3P)
                    {
                        if (tag == "L") // On utilise "L" pour le Rayon
                        {
                            circle.Radius = val;
                            circle.Pt2 = new Vector2(circle.Center.X + val, circle.Center.Y); // Mise à jour du point sur le périmètre
                        }
                        else if (tag == "DX") // Décalage X du centre
                        {
                            circle.Center = new Vector2(MyViewport.TempStartPoint.X + val, circle.Center.Y);
                        }
                        else if (tag == "DY") // Décalage Y du centre
                        {
                            circle.Center = new Vector2(circle.Center.X, MyViewport.TempStartPoint.Y + val);
                        }
                    }
                    else if (MyViewport.CurrentTool == CadToolType.Circle_2P)
                    {
                        if (tag == "L") // On utilise "L" pour le Rayon
                        {
                            circle.Radius = val/2;
                            circle.Pt1 = new Vector2(circle.Center.X - val / 2, circle.Center.Y); // Mise à jour du point sur le périmètre
                            circle.Pt2 = new Vector2(circle.Center.X + val/2, circle.Center.Y); // Mise à jour du point sur le périmètre
                        }
                        else if (tag == "DX") // Décalage X du centre
                        {
                            circle.Center = new Vector2(MyViewport.TempStartPoint.X + val, circle.Center.Y);
                        }
                        else if (tag == "DY") // Décalage Y du centre
                        {
                            circle.Center = new Vector2(circle.Center.X, MyViewport.TempStartPoint.Y + val);
                        }
                    }
                }
                // --- CAS DU RECTANGLE ---
                else if (MyViewport.SelectedEntity is RectangleEntity rect)
                {
                    if (tag == "DX")
                    {
                        rect.P2 = new Vector2(rect.P1.X + val, rect.P2.Y);
                    }
                    else if (tag == "DY")
                    {
                        rect.P2 = new Vector2(rect.P2.X, rect.P1.Y + val);
                    }
                }

                MyViewport.InvalidateVisual();
                MyViewport.Focus(); // Rend le focus au canvas pour continuer
            }
        }


    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        // --- TOUCHE CTRL+L : Ouvrir la fenêtre des calques ---
        if (e.Key == Key.L && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            OpenLayer();
            e.Handled = true; // On indique que l'événement est traité
        }
    }

    private void MyViewport_MouseMove(object sender, MouseEventArgs e)
    {
        // On récupère la position AIMANTÉE directement depuis le canvas
        Vector2 mouseWorld = MyViewport._currentMouseWorldPos;

        // Affichage X,Y permanent
        if (!MyViewport.IsDrawing && !MyViewport.IsEditing)
        {
            TxtX1.Text = mouseWorld.X.ToString("F2");
            TxtY1.Text = mouseWorld.Y.ToString("F2");
        }

        // Affichage des mesures si dessin en cours ou ligne sélectionnée
        if (MyViewport.IsDrawing || MyViewport.SelectedEntity is LineEntity)
        {
            Vector2 start = MyViewport.TempStartPoint;

            // Si on dessine, on suit la souris. Si on édite, on prend la fin de la ligne sélectionnée.
            Vector2 currentEnd = MyViewport.IsDrawing ? mouseWorld : ((LineEntity)MyViewport.SelectedEntity).End ;

            var (dx, dy, dist, angle) = MyViewport.GetCurrentMetrics(currentEnd);

            TxtDX.Text = dx.ToString("F2");
            TxtDY.Text = dy.ToString("F2");
            TxtDist.Text = dist.ToString("F2");
            TxtAngle.Text = angle.ToString("F2");
        }

        if (!MyViewport.IsDrawing && MyViewport.SelectedEntity == null)
        {
            ClearAllFields();
        }
    }
    // Effacer tous les champs de texte
    private void ClearAllFields()
    {
        TxtX1.Text = TxtY1.Text = TxtDX.Text = TxtDY.Text = TxtDist.Text = TxtAngle.Text = "";
    }


    // --- MENU FICHIER ---

    private void MenuNew_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Tout le travail non enregistré sera perdu.", "Nouveau", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            MyViewport.DeselectAll();
            MyViewport.StopDrawing();
            MyViewport.ResetLayers();
            MyViewport.Entities.Clear();

            // On recrée au moins un calque par défaut
            MyViewport.AddLayer(new Layer { Name = "Calque 1", Color = Colors.Black, IsActiveLayer = true });
            MyViewport.ActiveLayer = MyViewport.Layers[0];

            MyViewport.InvalidateVisual();
        }

    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    // --- MENU ÉDITION ---
    // On simule l'appui sur la touche Delete
    private void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        // On simule l'appui sur la touche Delete
        // (Ou on appelle directement la logique de suppression du Canvas)
        if (MyViewport.SelectedEntities.Count > 0)
        {
            foreach (var entity in MyViewport.SelectedEntities.ToList())
                MyViewport.Entities.Remove(entity);
            MyViewport.DeselectAll();
        }
    }
    
    private void PopulatePatternMenu()
    {
        if (MenuMotif == null) return;
        MenuMotif.Items.Clear();

        foreach (var lp in LinePatternManager.Patterns)
        {
            var mi = new MenuItem
            {
                Header = $"{lp.Name}  {lp.DisplayPattern}",
                Tag = lp,
                IsCheckable = true,
                IsChecked = (MyViewport.ActiveLayer?.LinePattern?.Name == lp.Name)
            };

            mi.Click += (s, e) => {
                var selectedPattern = (LinePattern)((MenuItem)s).Tag;

                // 1. Appliquer au calque (pour les futurs dessins)
                MyViewport.ActiveLayer.LinePattern = selectedPattern;

                // 2. Appliquer à la sélection actuelle
                foreach (var ent in MyViewport.SelectedEntities) ent.Pattern = selectedPattern;

                MyViewport.InvalidateVisual();
            };
            MenuMotif.Items.Add(mi);
        }

        MenuMotif.Items.Add(new Separator());
        var customMi = new MenuItem { Header = "Personnalisé..." };
        customMi.Click += (s, e) => {
            var editor = new PatternEditorWindow
            {
                Owner = this
            };
            if (editor.ShowDialog() == true && editor.CreatedPattern != null)
            {
                LinePatternManager.AddPattern(editor.CreatedPattern);
            }
        };
        MenuMotif.Items.Add(customMi);
    }

    // --- MENU VUE ---

    private void MenuZoomAll_Click(object sender, RoutedEventArgs e)
    {
        // Logique pour recentrer tout le dessin à l'écran
        if (MyViewport.Entities.Count == 0) return;

        // Calculer les limites (Bounding Box) de toutes les entités
        // (C'est un peu complexe, on pourra le faire en détail si tu veux)
        MyViewport.Zoom = 1.0f;
        MyViewport.Offset = new Vector2(0, 0);
        MyViewport.InvalidateVisual();
    }

    private void MenuGrid_Click(object sender, RoutedEventArgs e)
    {
        // On pourrait ajouter une propriété ShowGrid dans CadCanvas
        // MyViewport.ShowGrid = !MyViewport.ShowGrid;
        MyViewport.InvalidateVisual();
    }

    private void MenuColorPicker_Click(object sender, RoutedEventArgs e)
    {
        ColorPickerWindow picker = new ColorPickerWindow
        {
            Owner = this // Pour que la fenêtre soit centrée sur l'appli
        };

        if (picker.ShowDialog() == true)
        {
            // On met à jour la couleur dans le canvas
            MyViewport.CurrentDrawingColor = picker.SelectedColor;

            // On met à jour l'aperçu visuel du bouton
            //RectCurrentColor.Fill = new SolidColorBrush(picker.SelectedColor);

            if (MyViewport.SelectedEntity != null)
            {
                foreach (var entity in MyViewport.SelectedEntities)
                {
                    entity.Color = picker.SelectedColor;
                }
                MyViewport.InvalidateVisual();
            }
            MyViewport.ActiveLayer.Color = picker.SelectedColor; // On change la couleur du calque actif
        }
    }
    private void MenuWeight_Click(object sender, RoutedEventArgs e)
    {
        MenuItem? selectedItem = sender as MenuItem;
        if (selectedItem == null) return;

        // 1. Récupérer l'épaisseur depuis le Tag
        if (double.TryParse(selectedItem.Tag.ToString(), out double weight))
        {
            MyViewport.CurrentThickness = weight/10;

            // 2. Décocher tous les autres items du menu Poids
            foreach (MenuItem item in MenuPoids.Items)
            {
                item.IsChecked = (item == selectedItem);
            }

            // 3. Appliquer à l'entité sélectionnée
            foreach (var entity in MyViewport.SelectedEntities)
            {
                entity.Thickness = weight/10;
            }
            MyViewport.ActiveLayer.Thickness = weight/10;
            MyViewport.InvalidateVisual();
        }
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e)
    {
        var loadedProject = FileManager.Open();

        // Si l'utilisateur a annulé ou si le fichier est corrompu, on arrête là
        if (loadedProject == null) return;

        // 1. RIGUEUR : On stoppe toute action en cours avant de vider les listes
        MyViewport.DeselectAll();
        MyViewport.StopDrawing();
        MyViewport.ResetLayers();

        // 2. CHARGEMENT DES CALQUES
        MyViewport.Layers.Clear();
        foreach (var layer in loadedProject.Layers)
        {
            // TRÈS IMPORTANT : Utilisez votre méthode AddLayer(layer)
            // C'est elle qui fait le branchement : layer.Renamed += OnLayerRenamed;
            MyViewport.AddLayer(layer);
        }
        foreach (var ent in loadedProject.Entities)
        {
            if (ent.Pattern != null && !LinePatternManager.Patterns.Any(p => p.Name == ent.Pattern.Name))
            {
                LinePatternManager.AddPattern(ent.Pattern);
            }
        }

        // 3. CHARGEMENT DES ENTITÉS
        // On remplace la liste complète
        MyViewport.Entities = loadedProject.Entities ?? new List<Entity>();

        // 4. RESTAURATION DU CALQUE ACTIF
        // On cherche le calque par son nom sauvé, sinon on prend le premier par défaut
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var active = MyViewport.Layers.FirstOrDefault(l => l.Name == loadedProject.ActiveLayerName)
                         ?? MyViewport.Layers.FirstOrDefault();

            if (active != null)
            {
                // On passe par la propriété ActiveLayer du Canvas. 
                // Comme nous l'avons codée, elle va automatiquement :
                // - Mettre à jour le flag IsActiveLayer du calque
                // - Notifier la ComboBox en bas (Binding)
                // - Notifier le gestionnaire de calques
                MyViewport.ActiveLayer = active;
            }

            // 5. FINALISATION
            MyViewport.InvalidateVisual();

            // Optionnel : Mettre à jour le titre de la fenêtre avec le nom du fichier
            // this.Title = $"FeatherCAD - {loadedProject.ProjectName}";
        }), System.Windows.Threading.DispatcherPriority.Loaded);
        UpdateLayerComboBox();
    }
    private void MenuSave_Click(object sender, RoutedEventArgs e)
    {
        // On prépare le conteneur avec les données actuelles du Canvas
        var project = new CadProject
        {
            Entities = MyViewport.Entities,
            Layers = MyViewport.Layers.ToList(), // On convertit l'ObservableCollection en List
            ActiveLayerName = MyViewport.ActiveLayer?.Name ?? "Calque 1"
        };

        FileManager.Save(project);
    }

    private void MenuSelectAll_Click(object sender, RoutedEventArgs e)
    {
        MyViewport.SelectAll();
    }

    private void OnToolSelected(object sender, string toolTag)
    {
        // 1. On change l'outil dans le Canvas
        // On convertit le string en Enum CadToolType
        if (Enum.TryParse(toolTag, out CadToolType selectedType))
        {
            MyViewport.SetTool(selectedType);
            MyViewport.CurrentTool = selectedType;

            // 2. On met à jour l'interface ou les messages
            //TxtStatus.Text = MyViewport.GetInstruction();
        }
        else
        {
            // Cas particuliers (ex: Circle_3P, Circle_Tan si tu ne les as pas encore dans l'Enum)
            MessageBox.Show("Outil non encore implémenté : " + toolTag);
        }
    }

    private void UpdateUIForTool(CadToolType tool)
    {
        // On change les textes selon l'outil
        switch (tool)
        {
            case CadToolType.Line:
                lbDist.Text = "L :";
                lbAngle.Visibility = Visibility.Visible;
                TxtAngle.Visibility = Visibility.Visible;
                break;

            case CadToolType.Circle_cp:
                lbDist.Text = "R :";
                // On cache l'angle car inutile pour un cercle
                lbAngle.Visibility = Visibility.Collapsed;
                TxtAngle.Visibility = Visibility.Collapsed;
                break;
            case CadToolType.Circle_2P:
                lbDist.Text = "D :";
                // On cache l'angle car inutile pour un cercle
                //lbAngle.Visibility = Visibility.Collapsed;
                //TxtAngle.Visibility = Visibility.Collapsed;
                break;
            case CadToolType.Circle_3P:
                lbDist.Text = "R :";
                // On cache l'angle car inutile pour un cercle
                lbAngle.Visibility = Visibility.Collapsed;
                TxtAngle.Visibility = Visibility.Collapsed;
                break;

            case CadToolType.Select:
                lbDist.Text = "Dim :";
                break;
        }
    }
    private void MenuGroup_Click(object sender, RoutedEventArgs e)
    {
        MyViewport.GroupSelectedEntities();
    }

    private void MenuUngroup_Click(object sender, RoutedEventArgs e)
    {
        MyViewport.UngroupSelectedEntities();
    }

    private void MenuLayers_Click(object sender, RoutedEventArgs e)
    {
        OpenLayer();
    }
    public void OpenLayer()
    {
        LayersWindow win = new LayersWindow(MyViewport)
        {
            Owner = this
        };
        win.Show(); // On utilise Show pour pouvoir dessiner tout en gardant la fenêtre ouverte
        ComboLayers.SelectedItem = MyViewport.ActiveLayer;
    }
    public void UpdateLayerComboBox()
    {
        // On met à jour la ComboBox avec les calques actuels
        ComboLayers.ItemsSource = MyViewport.Layers;
        //ComboLayers.DisplayMemberPath = "Name";
        ComboLayers.SelectedItem = MyViewport.ActiveLayer;
    }

    //private void MenuMotif_Click(object sender, RoutedEventArgs e)
    //{

    //}
    private void MenuCustomPattern_Click(object sender, RoutedEventArgs e)
    {
        var editor = new PatternEditorWindow();
        editor.Owner = this;
        if (editor.ShowDialog() == true && editor.CreatedPattern != null)
        {
            LinePatternManager.AddPattern(editor.CreatedPattern);

            // Appliquer immédiatement au calque actif (Inspiration Graphite)
            MyViewport.ActiveLayer.LinePattern = editor.CreatedPattern;

            // Appliquer à la sélection s'il y en a une
            foreach (var entity in MyViewport.SelectedEntities)
            {
                entity.Pattern = editor.CreatedPattern;
            }
            MyViewport.InvalidateVisual();
        }
    }
}