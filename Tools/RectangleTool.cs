using System;
using System.Numerics;
using System.Windows.Input;
using System.Windows.Media;
using FeatherCAD.Models;
using FeatherCAD.Controls;

namespace FeatherCAD.Tools
{
    public class RectangleTool : CadTool
    {
        private bool _isDrawing = false;

        public RectangleTool(CadCanvas canvas) : base(canvas) { }

        public override void OnMouseDown(Vector2 worldPos, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (!_isDrawing)
            {
                // ÉTAPE 1 : Fixer le premier coin
                _isDrawing = true;

                // On utilise la méthode de base du Canvas pour mémoriser TempStartPoint
                Canvas.StartDrawingAction(worldPos);

                // Initialiser la prévisualisation
                Canvas.PreviewRectangle = new RectangleEntity(worldPos, worldPos, Colors.Gray, Canvas.ActiveLayer.Thickness)
                {
                    Pattern = Canvas.ActiveLayer.LinePattern
                };
            }
            else
            {
                // ÉTAPE 2 : Fixer le coin opposé et terminer
                Canvas.FinishRectangleAction(worldPos);
                _isDrawing = false;
            }
        }

        public override void OnMouseMove(Vector2 worldPos, MouseEventArgs e)
        {
            // Mise à jour de la diagonale en temps réel
            if (_isDrawing && Canvas.PreviewRectangle != null)
            {
                Canvas.PreviewRectangle.P2 = worldPos;
            }
        }

        public override void OnDeactivate()
        {
            _isDrawing = false;
            Canvas.PreviewRectangle = null;
            Canvas.StopDrawing();
        }

        public override string GetInstruction()
        {
            if (!_isDrawing)
                return "RECTANGLE : Cliquez pour placer le PREMIER COIN.";

            return "RECTANGLE : Cliquez pour placer le COIN OPPOSÉ ou entrez les dimensions dX, dY.";
        }
    }
}