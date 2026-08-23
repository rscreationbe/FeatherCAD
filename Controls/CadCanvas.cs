using FeatherCAD.Logic;
using FeatherCAD.Models;
using FeatherCAD.Tools;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FeatherCAD.Controls;

public class CadCanvas : FrameworkElement
{
    #region VARIABLES & ATTRIBUTS
    // --- DONNÉES ---
    public List<Entity> Entities { get; set; } = [];
    public LineEntity? PreviewLine { get; set; }
    public CircleEntity? PreviewCircle { get; set; }
    public ArcEntity? PreviewArc { get; set; }
    public List<Entity> SelectedEntities { get; } = []; 
    public Entity? SelectedEntity => SelectedEntities.LastOrDefault();

    // --- VUE & CAMERA ---
    public float Zoom = 1.0f;
    public Vector2 Offset = new(0, 0);
    private Point _lastMousePosition; // Pour le Pan
    private Point _mouseScreenPos;    // Pour le dessin du curseur
    public Vector2 _currentMouseWorldPos; // Position aimantée (Monde)

    // --- ÉTATS ---
    // Ces drapeaux sont mis à jour par les outils
    public bool IsDrawing { get; set; } = false;
    public bool IsEditing { get; set; } = false;
    public Vector2 TempStartPoint { get; set; }


    // --- LOGIQUE EXTERNE ---
    private DraftingAssistant _assistant = new();
    private SnapResult _currentSnap = new() { Type = SnapType.None };
    private CadTool _activeTool;
    private CadToolType _currentToolType = CadToolType.Select; // Le type (Enum)

    //public CadTool ActiveTool => _activeTool;

    // Dictionnaire pour stocker les outils disponibles
    private Dictionary<CadToolType, CadTool> _tools;

    public Color CurrentDrawingColor { get; set; } = Colors.Black;
    public double CurrentThickness { get; set; } = 1.0;

    public bool IsBoxSelecting { get; set; } = false;
    public Vector2 BoxStartWorld { get; set; }
    public Vector2 BoxEndWorld { get; set; }

    #endregion

    #region DECLARATIONS DES EVENEMENTS 
    // Événement pour notifier que le dessin a commencé ou terminé
    public event EventHandler? DrawingStarted;
    // Événement pour notifier que le dessin a commencé ou terminé
    public event EventHandler? DrawingFinished;
    // Événement pour notifier que l'outil a changé
    public event EventHandler<CadToolType>? ToolChanged;
    // Événement pour notifier que le texte d'aide doit changer
    public event EventHandler<string>? StatusChanged;
    #endregion

    public CadCanvas()
    {
        this.Cursor = Cursors.None;
        this.Focusable = true;
        this.ClipToBounds = true;


        _tools = new Dictionary<CadToolType, CadTool>
        {
            { CadToolType.Select, new SelectTool(this) },
            { CadToolType.Line, new LineTool(this) },
            { CadToolType.Circle_cp, new CircleTool(this) },
            { CadToolType.Circle_2P, new Circle2PTool(this) },
            { CadToolType.Circle_3P, new Circle3PTool(this) },
            { CadToolType.Arc_cp, new ArcTool(this) },
            //{ CadToolType.Arc_3P, new Arc3PTool(this) }
        };

        _activeTool = _tools[CadToolType.Select];
        _currentToolType = CadToolType.Select;
    }

    #region INTERFACE POUR LES OUTILS

    // Méthode appelée par MainWindow pour changer d'outil
    public void SetTool(CadToolType type)
    {
        _activeTool?.OnDeactivate();
        if (_tools.ContainsKey(type)) { 
            _activeTool = _tools[type];
            UpdateStatus();
        }
        InvalidateVisual();
    }
    public CadToolType CurrentTool
    {
        get => _currentToolType;
        set
        {
            if (_currentToolType == value) return;

            _activeTool?.OnDeactivate();
            _currentToolType = value;
            if (_tools.ContainsKey(value)) _activeTool = _tools[value];

            // --- ON AJOUTE CETTE LIGNE ---
            ToolChanged?.Invoke(this, value);

            UpdateStatus();
            InvalidateVisual();
        }
    }
    // Méthodes d'aide appelées par LineTool
    public void StartDrawingAction(Vector2 pos)
    {
        DeselectAll();
        IsDrawing = true;
        IsEditing = false;
        TempStartPoint = pos;
        PreviewLine = new LineEntity(pos, pos, Colors.Gray, CurrentThickness) { DashStyle = DashStyles.Dash };
        DrawingStarted?.Invoke(this, EventArgs.Empty);

    }
    // Méthodes d'aide appelées par LineTool
    public void FinishDrawingAction(Vector2 pos)
    {
        IsDrawing = false;
        IsEditing = true;
        var finalLine = new LineEntity(TempStartPoint, pos, CurrentDrawingColor, CurrentThickness);
        Entities.Add(finalLine);
        SelectedEntities.Add(finalLine);
        PreviewLine = null;
        DrawingFinished?.Invoke(this, EventArgs.Empty);
    }
    // Méthodes d'aide appelées par CircleTool
    public void FinishCircleAction(Vector2 pos)
    {
        IsDrawing = false;
        IsEditing = true;
        float radius;
        CircleEntity finalCircle = new();
        // Calcul du rayon final
        if (CurrentTool == CadToolType.Circle_cp) 
        { 
            radius = Vector2.Distance(TempStartPoint, pos);
            // Création de l'entité finale
            finalCircle = new CircleEntity(TempStartPoint, radius, CurrentDrawingColor, CurrentThickness)
            {
                Pt1 = pos
            };
        }
        if (CurrentTool == CadToolType.Circle_2P)
        {
            // Le centre est le milieu entre P1 et P2

            radius = Vector2.Distance(TempStartPoint, pos)/2;
            // Création de l'entité finale
            finalCircle = new CircleEntity(TempStartPoint, radius, CurrentDrawingColor, CurrentThickness)
            {
                Center = (TempStartPoint + pos) / 2,
                Pt1 = TempStartPoint,
                Pt2 = pos
            };
        }
        if (CurrentTool == CadToolType.Circle_3P)
        {
            radius = Vector2.Distance(TempStartPoint, pos);
            // Création de l'entité finale
            finalCircle = new CircleEntity(TempStartPoint, radius, CurrentDrawingColor, CurrentThickness)
            {
                Pt1 = TempStartPoint,
                Pt2 = pos
            };
        }

        Entities.Add(finalCircle);

        // On utilise la méthode de sélection unique que nous avons créée
        SetSingleSelection(finalCircle);

        PreviewCircle = null;

        // On prévient la MainWindow que le dessin est fini
        DrawingFinished?.Invoke(this, EventArgs.Empty);
    }
    // Méthode pour gérer la sélection d'entités
    public void HandleSelectionTool(Vector2 mousePos)
    {
        Entity? found = null;
        float threshold = 5.0f / Zoom;

        // 1. Chercher l'entité sous la souris
        for (int i = Entities.Count - 1; i >= 0; i--)
        {
            var entity = Entities[i];

            // CAS DU GROUPE 
            if (entity is GroupEntity g)
            {
                if (g.IsPointInside(mousePos, threshold))
                {
                    found = g;
                    break;
                }
            }
            // CAS LIGNE
            else if (Entities[i] is LineEntity line && IsPointNearLine(mousePos, line.Start, line.End, threshold))
            { found = line; break; }
            // CAS CERCLE
            else if (Entities[i] is CircleEntity circle && Math.Abs(Vector2.Distance(mousePos, circle.Center) - circle.Radius) < threshold)
            { found = circle; break; }
            // CAS ARC
            else if (Entities[i] is ArcEntity arc && Math.Abs(Vector2.Distance(mousePos, arc.Center) - arc.Radius) < threshold && arc.IsPointOnArc(mousePos, threshold))
            { found = arc; break; }
        }

        // 2. Gérer la logique SHIFT
        bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        if (!isShiftDown)
        {
            // Si Shift n'est pas appuyé, on vide tout avant de sélectionner la nouvelle
            SelectedEntities.Clear();
        }

        if (found != null)
        {
            if (isShiftDown && SelectedEntities.Contains(found))
            {
                SelectedEntities.Remove(found); // Toggle : si déjà là, on l'enlève
            }
            else if (!SelectedEntities.Contains(found))
            {
                SelectedEntities.Add(found); // Sinon on l'ajoute
            }
        }

        IsEditing = SelectedEntities.Count > 0;
    }
    // Méthode utilitaire pour vérifier si un point est proche d'une ligne
    private bool IsPointNearLine(Vector2 p, Vector2 a, Vector2 b, float threshold)
    {
        float l2 = Vector2.DistanceSquared(a, b);
        if (l2 == 0) return Vector2.Distance(p, a) < threshold;

        // Calcul de la projection du point P sur le segment AB
        float t = Math.Max(0, Math.Min(1, Vector2.Dot(p - a, b - a) / l2));
        Vector2 projection = a + t * (b - a);

        return Vector2.Distance(p, projection) < threshold;
    }
    // Méthode pour sélectionner une seule entité et désélectionner les autres
    public void SetSingleSelection(Entity? entity)
    {
        SelectedEntities.Clear();
        if (entity != null)
        {
            SelectedEntities.Add(entity);
        }
        IsEditing = (entity != null);
        InvalidateVisual();
    }
    // Utile pour les calculs d'angles dans CheckSnapping
    public (float dx, float dy, float dist, float angle) GetCurrentMetrics(Vector2 end)
    {
        float dx = end.X - TempStartPoint.X;
        float dy = end.Y - TempStartPoint.Y;
        float dist = Vector2.Distance(TempStartPoint, end);
        float angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
        return (dx, dy, dist, angle);
    }
    // Méthode utilitaire pour vérifier si un point est dans un rectangle défini par min/max X/Y
    private bool IsPointInBox(Vector2 p, float minX, float maxX, float minY, float maxY)
    {
        return p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY;
    }
    // Méthode pour désélectionner toutes les entités
    public void DeselectAll()
    {
        SelectedEntities.Clear(); // On vide la liste
        IsEditing = false;
        IsDrawing = false;
        PreviewLine = null;
        PreviewCircle = null;
        _assistant.ClearMemory();
        InvalidateVisual();
    }
    // Méthode pour arrêter le dessin en cours
    public void StopDrawing()
    {
        // 1. On remet les drapeaux d'état à zéro
        IsDrawing = false;

        // 2. On supprime les prévisualisations (lignes et cercles élastiques)
        PreviewLine = null;
        PreviewCircle = null;

        // 3. On vide la mémoire de l'assistant (les petits points bleus)
        // Pour ne pas que les alignements restent figés après l'annulation
        _assistant.ClearMemory();

        // 4. On demande à WPF de rafraîchir l'affichage
        InvalidateVisual();
    }
    // Méthode pour mettre à jour le statut affiché dans la barre d'état
    public void UpdateStatus()
    {
        string msg = _activeTool?.GetInstruction() ?? "Prêt";
        StatusChanged?.Invoke(this, msg);
    }




    #endregion

    #region RENDU
    // Méthode de rendu principale
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

        foreach (var entity in Entities)
        {
            // On vérifie si cette entité est dans la liste des sélectionnés
            bool isSelected = SelectedEntities.Contains(entity);

            // On passe l'info à la méthode Draw
            entity.Draw(dc, WorldToScreen, isSelected);
        }

        PreviewLine?.Draw(dc, WorldToScreen, false);
        PreviewCircle?.Draw(dc, WorldToScreen, false);
        PreviewArc?.Draw(dc, WorldToScreen, false);

        // Dessin des poignées si sélection
        foreach (var sel in SelectedEntities)
        {
            if (sel is LineEntity l)
            {
                //sel.Color = Colors.Red; // Change color to indicate selection
                DrawHandle(dc, l.Start);
                DrawHandle(dc, l.End);
            }
            else if (sel is CircleEntity c)
            {
                DrawHandle(dc, c.Center);
                if (c.Pt1 != Vector2.Zero) DrawHandle(dc, c.Pt1);
                if (c.Pt2 != Vector2.Zero) DrawHandle(dc, c.Pt2);
                if (c.Pt3 != Vector2.Zero) DrawHandle(dc, c.Pt3);
            }
            else if (sel is ArcEntity a)
            {
                DrawHandle(dc, a.Center);
                if (a.StartPoint != Vector2.Zero) DrawHandle(dc, a.StartPoint);
                if (a.EndPoint != Vector2.Zero) DrawHandle(dc, a.EndPoint);
            }

        }

        if (IsBoxSelecting)
        {
            DrawSelectionRectangle(dc);
        }

        // Curseur Croix
        DrawCursor(dc);

        // Snapping Assistant
        DrawDraftingAssistant(dc);
    }
    // Méthode pour dessiner le rectangle de sélection
    private void DrawSelectionRectangle(DrawingContext dc)
    {
        Point p1 = WorldToScreen(BoxStartWorld);
        Point p2 = WorldToScreen(BoxEndWorld);
        Rect selectionRect = new Rect(p1, p2);
        // Couleur de remplissage bleu transparent
        Brush fillBrush = new SolidColorBrush(Color.FromArgb(30, 0, 120, 215));
        fillBrush.Freeze();
        // Bordure bleue
        Pen strokePen = new Pen(Brushes.DodgerBlue, 1);
        strokePen.Freeze();
        dc.DrawRectangle(fillBrush, strokePen, selectionRect);
    }
    // Méthode pour dessiner les poignées de sélection
    private void DrawHandle(DrawingContext dc, Vector2 pos)
    {
        Point p = WorldToScreen(pos);
        dc.DrawRectangle(Brushes.White, new Pen(Brushes.Red, 0.5), new Rect(p.X - 2, p.Y - 2, 4, 4));
    }
    // Méthode pour dessiner le curseur en croix
    private void DrawCursor(DrawingContext dc)
    {
        Pen p = new Pen(Brushes.Red, 0.5);
        dc.DrawLine(p, new Point(_mouseScreenPos.X - 5, _mouseScreenPos.Y), new Point(_mouseScreenPos.X + 5, _mouseScreenPos.Y));
        dc.DrawLine(p, new Point(_mouseScreenPos.X, _mouseScreenPos.Y - 5), new Point(_mouseScreenPos.X, _mouseScreenPos.Y + 5));
    }
    // Méthode pour dessiner l'assistant de dessin (Drafting Assistant)
    private void DrawDraftingAssistant(DrawingContext dc)
    {
        // Si pas de snap, on ne dessine rien
        if (_currentSnap.Type == SnapType.None) return;

        // 1. Préparation des styles
        Point snapPointScreen = WorldToScreen(_currentSnap.WorldPoint);

        Pen alignPen = new Pen(Brushes.LightGray, 0.8) { DashStyle = DashStyles.Dash };
        alignPen.Freeze(); // Performance

        Pen markerPen = new Pen(Brushes.Orange, 1);
        markerPen.Freeze();

        // 2. DESSIN DES LIGNES D'ALIGNEMENT (Les pointillés "infinis")
        // On utilise la liste ActiveAlignments que l'assistant nous a fournie
        if (_currentSnap.ActiveAlignments != null)
        {
            foreach (var align in _currentSnap.ActiveAlignments)
            {
                // On calcule deux points très loin pour simuler l'infini
                // On utilise 10000 / Zoom pour que la ligne couvre tout le monde
                Vector2 p1World = align.Origin + (align.Direction * (10000 / Zoom));
                Vector2 p2World = align.Origin - (align.Direction * (10000 / Zoom));

                dc.DrawLine(alignPen, WorldToScreen(p1World), WorldToScreen(p2World));
            }
        }

        // 3. DESSIN DU MARQUEUR (Le petit cercle orange sur le point)
        dc.DrawEllipse(null, markerPen, snapPointScreen, 4, 4);

        // 4. DESSIN DU TEXTE (Le label : "Fin", "Milieu", "45°"...)
        FormattedText labelText = new(
            _currentSnap.Label,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), // Police standard Windows
            10,                         // Taille
            Brushes.Orange,             // Couleur
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        // On décale un peu le texte pour qu'il ne soit pas sur le curseur
        dc.DrawText(labelText, new Point(snapPointScreen.X + 10, snapPointScreen.Y - 15));
    }

    #endregion

    #region SOURIS

    // Méthode pour gérer le clic de la souris
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        this.Focus();
        UpdateStatus();

        if (e.ChangedButton == MouseButton.Left) { 
            _activeTool.OnMouseDown(_currentMouseWorldPos, e);
            UpdateStatus();
        }
        if (e.ChangedButton == MouseButton.Middle) { _lastMousePosition = e.GetPosition(this); this.CaptureMouse(); }
        
        InvalidateVisual();
    }
    // Méthode pour gérer le mouvement de la souris
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Point rawPos = e.GetPosition(this);
        Vector2 rawWorld = ScreenToWorld(rawPos);

        // 1. Snapping
        _currentSnap = _assistant.CalculateSnap(rawWorld, Entities, Zoom, IsDrawing ? TempStartPoint : null);
        _currentMouseWorldPos = (_currentSnap.Type != SnapType.None) ? _currentSnap.WorldPoint : rawWorld;
        _mouseScreenPos = WorldToScreen(_currentMouseWorldPos);

        // 2. Tool
        _activeTool.OnMouseMove(_currentMouseWorldPos, e);

        // 3. Pan
        if (this.IsMouseCaptured && e.MiddleButton == MouseButtonState.Pressed)
        {
            Offset += new Vector2((float)(rawPos.X - _lastMousePosition.X), (float)(rawPos.Y - _lastMousePosition.Y));
            _lastMousePosition = rawPos;
        }
        InvalidateVisual();
    }
    // Méthode pour gérer le relâchement du bouton de la souris
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.ChangedButton == MouseButton.Left)
        {
            _activeTool.OnMouseUp(e); // On prévient l'outil que le bouton est relâché
            UpdateStatus();
        }
        if (e.ChangedButton == MouseButton.Middle) this.ReleaseMouseCapture();
        InvalidateVisual();
    }
    // Méthode pour gérer le zoom avec la molette de la souris
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        // 1. Déterminer le facteur de zoom (10% par cran de molette)
        float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;

        // 2. Récupérer la position de la souris au moment du zoom
        Point mousePos = e.GetPosition(this);

        // 3. Astuce mathématique pour zoomer vers la souris :
        // On calcule la position du monde sous la souris AVANT le zoom
        Vector2 worldBefore = ScreenToWorld(mousePos);

        // 4. On applique le zoom
        Zoom *= zoomFactor;

        // On limite le zoom pour ne pas disparaître à l'infini
        if (Zoom < 0.001f) Zoom = 0.001f;
        if (Zoom > 1000.0f) Zoom = 1000.0f;

        // 5. On recalcule l'Offset pour que le point sous la souris reste au même endroit
        Vector2 worldAfter = ScreenToWorld(mousePos);

        // La différence entre avant et après doit être compensée dans l'Offset
        Offset += (worldAfter - worldBefore) * Zoom;

        // Redessiner
        InvalidateVisual();
    }

    #endregion

    #region CONVERTION COORDONNEES

    // Méthodes de conversion entre coordonnées monde et écran
    public Point WorldToScreen(Vector2 wp) => new((wp.X * Zoom) + Offset.X, (wp.Y * Zoom) + Offset.Y);
    // Méthode pour convertir les coordonnées écran en coordonnées monde
    public Vector2 ScreenToWorld(Point sp) => new((float)((sp.X - Offset.X) / Zoom), (float)((sp.Y - Offset.Y) / Zoom));

    #endregion

    #region GESTION CLAVIER
    // Méthode pour gérer l'appui d'une touche
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // --- TOUCHE SUPPR : Supprimer tout ce qui est sélectionné ---
        if (e.Key == Key.Delete)
        {
            if (SelectedEntities.Count > 0)
            {
                // On utilise ToList() pour pouvoir supprimer pendant qu'on parcourt la liste
                foreach (var entity in SelectedEntities.ToList())
                {
                    Entities.Remove(entity);
                }
                DeselectAll(); // Nettoie l'affichage et les variables
            }
        }

        // --- TOUCHE ESC : Annuler l'action en cours ---
        if (e.Key == Key.Escape)
        {
            if (IsDrawing)
            {
                // On demande à l'outil de s'arrêter proprement
                _activeTool?.OnDeactivate();
                IsDrawing = false;
                PreviewLine = null;
                PreviewCircle = null;
            }
            else
            {
                // Si on ne dessinait rien, on désélectionne tout
                DeselectAll();
            }

            // On vide la mémoire de l'assistant (les petits points bleus)
            _assistant.ClearMemory();

            InvalidateVisual();
        }
        // --- TOUCHE CTRL+A : Sélectionner tout ---
        if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            SelectAll();
            e.Handled = true; // On indique que l'événement est traité
        }
        // --- TOUCHE SHIFT : Mettre à jour le statut ---
        if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
        {
            UpdateStatus();
        }
    }
    // Méthode pour gérer le relâchement d'une touche
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        // Quand on relâche Shift, on revient à l'instruction normale
        if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
        {
            UpdateStatus();
        }
    }
    #endregion

    #region METHODES DE SELECTION

    // Méthode pour sélectionner toutes les entités du dessin
    public void SelectAll()
    {
        // 1. On vide d'abord la sélection actuelle pour éviter les doublons
        SelectedEntities.Clear();

        // 2. On ajoute toutes les entités du dessin à la liste de sélection
        foreach (var entity in Entities)
        {
            SelectedEntities.Add(entity);
        }

        // 3. On met à jour l'état d'édition et on redessine
        IsEditing = SelectedEntities.Count > 0;
        InvalidateVisual();
    }
    // Méthode pour démarrer la sélection par boîte
    public void StartBoxSelection(Vector2 pos)
    {
        IsBoxSelecting = true;
        BoxStartWorld = pos;
        BoxEndWorld = pos;
    }
    // Méthode pour mettre à jour la sélection par boîte
    public void UpdateBoxSelection(Vector2 pos)
    {
        BoxEndWorld = pos;
    }
    // Méthode pour finaliser la sélection par boîte
    public void CommitBoxSelection(Vector2 start, Vector2 end)
    {
        IsBoxSelecting = false;

        // Calculer les bornes du rectangle (Min et Max pour gérer tous les sens de tracé)
        float minX = Math.Min(start.X, end.X);
        float maxX = Math.Max(start.X, end.X);
        float minY = Math.Min(start.Y, end.Y);
        float maxY = Math.Max(start.Y, end.Y);

        bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        if (!isShiftDown) SelectedEntities.Clear();

        foreach (var entity in Entities)
        {
            bool isInside = false;
            if (entity is LineEntity line)
            {
                // Une ligne est dedans si ses deux extrémités le sont
                isInside = IsPointInBox(line.Start, minX, maxX, minY, maxY) &&
                           IsPointInBox(line.End, minX, maxX, minY, maxY);
            }
            else if (entity is CircleEntity circle)
            {
                // Un cercle est dedans si son carré englobant le est
                isInside = IsPointInBox(new Vector2(circle.Center.X - circle.Radius, circle.Center.Y - circle.Radius), minX, maxX, minY, maxY) &&
                           IsPointInBox(new Vector2(circle.Center.X + circle.Radius, circle.Center.Y + circle.Radius), minX, maxX, minY, maxY);
            }

            if (isInside && !SelectedEntities.Contains(entity))
                SelectedEntities.Add(entity);
        }
        IsEditing = SelectedEntities.Count > 0;
        InvalidateVisual();
    }
    #endregion

    #region GESTION DE GROUPE ENTITIES
    // Méthode pour grouper les entités sélectionnées
    public void GroupSelectedEntities()
    {
        if (SelectedEntities.Count < 2) return; // Rien à grouper

        // 1. Créer le groupe
        GroupEntity newGroup = new GroupEntity();

        // 2. Transférer les entités
        foreach (var entity in SelectedEntities.ToList())
        {
            newGroup.Children.Add(entity);
            Entities.Remove(entity); // On les enlève de la liste principale
        }

        // 3. Ajouter le groupe à la liste principale
        Entities.Add(newGroup);

        // 4. Sélectionner le nouveau groupe
        SetSingleSelection(newGroup);
        InvalidateVisual();
    }
    // Méthode pour dégrouper les entités sélectionnées
    public void UngroupSelectedEntities()
    {
        var groups = SelectedEntities.OfType<GroupEntity>().ToList();

        foreach (var group in groups)
        {
            // On remet les enfants dans la liste principale
            foreach (var child in group.Children)
            {
                Entities.Add(child);
            }

            // On supprime le groupe
            Entities.Remove(group);
        }

        DeselectAll();
        InvalidateVisual();
    }
    #endregion

}