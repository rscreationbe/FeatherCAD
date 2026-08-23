using System.Numerics;
using System.Windows.Input;
using FeatherCAD.Controls;
using FeatherCAD.Models;

namespace FeatherCAD.Tools;

public class LineTool : CadTool
{
    // État interne pour savoir si on attend le deuxième clic
    private bool _isDrawing = false;

    /// <summary>
    /// Initialise un nouvel outil de dessin de ligne.
    /// </summary>
    /// <param name="canvas">Le canevas sur lequel l'outil sera utilisé.</param>
    public LineTool(CadCanvas canvas) : base(canvas) { }

    // Méthode appelée lors d'un clic de souris sur le canevas
    public override void OnMouseDown(Vector2 worldPos, MouseButtonEventArgs e)
    {
        // On ne réagit qu'au clic gauche
        if (e.ChangedButton != MouseButton.Left) return;

        if (!_isDrawing)
        {
            // --- ÉTAPE 1 : Premier clic (Point de départ) ---
            _isDrawing = true;

            // On délègue au Canvas la création de la PreviewLine et l'allumage des flags
            Canvas.StartDrawingAction(worldPos);
        }
        else
        {
            // --- ÉTAPE 2 : Deuxième clic (Point d'arrivée) ---
            // On délègue au Canvas la création de l'entité finale
            Canvas.FinishDrawingAction(worldPos);

            // On réinitialise l'état pour la prochaine ligne
            _isDrawing = false;

            // ASTUCE GRAPHITE : 
            // Si tu veux que les lignes s'enchaînent (le point de fin devient le nouveau départ),
            // remplace les deux lignes ci-dessus par :
            // _isDrawing = true;
            // Canvas.StartDrawingAction(worldPos);
        }
    }
    // Méthode appelée lors du mouvement de la souris sur le canevas
    public override void OnMouseMove(Vector2 worldPos, MouseEventArgs e)
    {
        // Si on est entre deux clics, on met à jour la ligne élastique
        if (_isDrawing && Canvas.PreviewLine != null)
        {
            Canvas.PreviewLine.End = worldPos;
        }
    }
    // Méthode appelée quand l'outil est désactivé (changement d'outil ou ESC)
    public override void OnDeactivate()
    {
        // Appelé quand on change d'outil ou qu'on appuie sur ESC
        _isDrawing = false;
        Canvas.StopDrawing(); // Nettoie PreviewLine et IsDrawing dans le Canvas
    }
    // Méthode pour obtenir l'instruction à afficher dans la barre d'état
    public override string GetInstruction()
    {
        if (!_isDrawing)
            return "Ligne simple : Sélectionnez le point de départ.";

        return "Ligne simple : Cliquez pour placer le point d'arrivée.";
    }
}