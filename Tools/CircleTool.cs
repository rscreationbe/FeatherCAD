using System.Numerics;
using System.Windows.Input;
using System.Windows.Media;
using FeatherCAD.Models;

namespace FeatherCAD.Tools
{
    public class CircleTool : CadTool
    {
        private bool _isDrawing = false;
        private Vector2 _center;

        public CircleTool(Controls.CadCanvas canvas) : base(canvas) {  }

        public override void OnMouseDown(Vector2 worldPos, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (!_isDrawing)
            {
                _center = worldPos;
                _isDrawing = true;
                // On utilise la méthode de base du Canvas pour fixer le centre
                Canvas.StartDrawingAction(worldPos);
                // On s'assure que c'est un cercle qu'on prévisualise
                Canvas.PreviewLine = null;
                Canvas.PreviewCircle = new CircleEntity(worldPos, 0, Colors.Gray, 0.5) { DashStyle = DashStyles.Dash };
            }
            else
            {
                // DEUXIÈME CLIC : On appelle la nouvelle méthode
                Canvas.FinishCircleAction(worldPos);
                _isDrawing = false;
            }
        }

        public override void OnMouseMove(Vector2 worldPos, MouseEventArgs e)
        {
            if (_isDrawing && Canvas.PreviewCircle != null)
            {
                // On met à jour le rayon en temps réel (distance centre-souris)
                Canvas.PreviewCircle.Radius = Vector2.Distance(_center, worldPos);
            }
        }

        private void Finish()
        {
            _isDrawing = false;
            Canvas.IsDrawing = false;
            Canvas.PreviewCircle = null;
            Canvas.IsEditing = true;
            // On déclenche l'événement pour mettre à jour les TextBox (dX, dY, L, A)
            // Pour un cercle, "L" peut représenter le rayon.
        }

        public override void OnDeactivate()
        {
            Finish();
            Canvas.IsEditing = false;
            Canvas.StopDrawing();
        }
        public override string GetInstruction()
        {
            if (!_isDrawing)
                return "Cercle 'centre-point : Sélectionnez le centre.";

            return "Cercle 'centre-point : Sélectionnez un point sur le périmètre.";
        }
    }
}