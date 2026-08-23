using System.Numerics;
using System.Windows.Input;
using System.Windows.Media;
using FeatherCAD.Models;
using FeatherCAD.Logic;

namespace FeatherCAD.Tools
{
    public class Circle3PTool : CadTool
    {
        private int _step = 0; // 0: Attente P1, 1: Attente P2, 2: Attente P3
        private Vector2 _p1, _p2, _p3;

        public Circle3PTool(Controls.CadCanvas canvas) : base(canvas) { }

        public override void OnMouseDown(Vector2 worldPos, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (_step == 0) // Premier clic
            {
                _p1 = worldPos;
                _step = 1;
                Canvas.StartDrawingAction(worldPos);
                // On utilise PreviewLine pour montrer le segment P1 -> Souris
                Canvas.PreviewLine = new LineEntity(_p1, worldPos, Colors.Gray, 0.5) { DashStyle = DashStyles.Dash };
            }
            else if (_step == 1) // Deuxième clic
            {
                _p2 = worldPos;
                _step = 2;
                Canvas.PreviewLine = null; // On enlève la ligne
                // On initialise le cercle de prévisualisation
                Canvas.PreviewCircle = new CircleEntity(_p1, 0, Colors.Gray, 0.5) { DashStyle = DashStyles.Dash };
            }
            else if (_step == 2) // Troisième clic
            {
                if (GeometryUtils.CalculateCircle3P(_p1, _p2, worldPos, out Vector2 center, out float radius))
                {
                    _p3 = worldPos;
                    var finalCircle = new CircleEntity(center, radius, Canvas.CurrentDrawingColor, Canvas.CurrentThickness);
                    
                    finalCircle.Pt1 = _p1;
                    finalCircle.Pt2 = _p2;
                    finalCircle.Pt3 = _p3; // On stocke le troisième point pour référence
                    Canvas.Entities.Add(finalCircle);
                    Canvas.SetSingleSelection(finalCircle);
                }
                Finish();
            }
        }

        public override void OnMouseMove(Vector2 worldPos, MouseEventArgs e)
        {
            if (_step == 1 && Canvas.PreviewLine != null)
            {
                Canvas.PreviewLine.End = worldPos;
            }
            else if (_step == 2 && Canvas.PreviewCircle != null)
            {
                // Calculer le cercle passant par P1, P2 et la souris en temps réel
                if (GeometryUtils.CalculateCircle3P(_p1, _p2, worldPos, out Vector2 center, out float radius))
                {
                    Canvas.PreviewCircle.Center = center;
                    Canvas.PreviewCircle.Radius = radius;
                }
            }
        }

        private void Finish()
        {
            _step = 0;
            Canvas.IsDrawing = false;
            Canvas.PreviewLine = null;
            Canvas.PreviewCircle = null;
            Canvas.StopDrawing();
        }

        public override void OnDeactivate()
        {
            Finish();
        }

        public override string GetInstruction()
        {
            return _step switch
            {
                0 => "Cercle 3 Points : Selectionnez le PREMIER point.",
                1 => "Cercle 3 Points : Selectionnez le DEUXIÈME point.",
                2 => "Cercle 3 Points : Selectionnez le TROISIÈME point (termine le cercle).",
                _ => ""
            };
        }
    }
}