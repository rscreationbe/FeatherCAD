using System.Numerics;
using System.Windows.Input;
using System.Windows.Media;
using FeatherCAD.Models;

namespace FeatherCAD.Tools
{
    public class Circle2PTool : CadTool
    {
        private bool _isDrawing = false;
        private Vector2 _p1;

        public Circle2PTool(Controls.CadCanvas canvas) : base(canvas) { }

        public override void OnMouseDown(Vector2 worldPos, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (!_isDrawing)
            {
                // PREMIER CLIC : On fixe le premier point du diamètre
                _p1 = worldPos;
                _isDrawing = true;

                Canvas.StartDrawingAction(worldPos);

                // On initialise le cercle de prévisualisation
                Canvas.PreviewCircle = new CircleEntity(worldPos, 0, Colors.Gray, 0.5)
                {
                    DashStyle = DashStyles.Dash
                };
            }
            else
            {
                // DEUXIÈME CLIC : On valide le diamètre
                float distance = Vector2.Distance(_p1, worldPos);

                if (distance > 0.001f)
                {
                    Canvas.FinishCircleAction(worldPos);
                }

                Finish();
            }
        }

        public override void OnMouseMove(Vector2 worldPos, MouseEventArgs e)
        {
            if (_isDrawing && Canvas.PreviewCircle != null)
            {
                // Mise à jour dynamique du centre et du rayon
                // Le centre "glisse" pour rester entre le point fixe et la souris
                Canvas.PreviewCircle.Center = (_p1 + worldPos) / 2;
                Canvas.PreviewCircle.Radius = Vector2.Distance(_p1, worldPos) / 2;
            }
        }

        private void Finish()
        {
            _isDrawing = false;
            Canvas.IsDrawing = false;
            Canvas.PreviewCircle = null;
            Canvas.StopDrawing();
        }

        public override void OnDeactivate()
        {
            Finish();
        }

        public override string GetInstruction()
        {
            if (!_isDrawing)
                return "Cercle Diamètre : Sélectionnez le PREMIER point du diamètre.";

            return "Cercle Diamètre : Sélectionnez le DEUXIÈME point du diamètre.";
        }
    }
}