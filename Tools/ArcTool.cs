using System;
using System.Numerics;
using System.Windows.Input;
using System.Windows.Media;
using FeatherCAD.Models;
using FeatherCAD.Controls;

namespace FeatherCAD.Tools
{
    public class ArcTool : CadTool
    {
        private int _step = 0; // 0: Centre, 1: Départ, 2: Fin
        private Vector2 _center;
        private Vector2 _startPoint;

        public ArcTool(CadCanvas canvas) : base(canvas) { }

        public override void OnMouseDown(Vector2 worldPos, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (_step == 0)
            {
                _center = worldPos;
                _step = 1;
                Canvas.StartDrawingAction(worldPos);
                // Preview du rayon (ligne élastique)
                Canvas.PreviewLine = new LineEntity(_center, worldPos, Colors.Gray, 0.5) { DashStyle = DashStyles.Dash };
            }
            else if (_step == 1)
            {
                _startPoint = worldPos;
                _step = 2;
                Canvas.PreviewLine = null;
                // Preview de l'arc
                Canvas.PreviewArc = new ArcEntity(_center, _startPoint, worldPos, Colors.Gray, 0.5) { DashStyle = DashStyles.Dash };
            }
            else if (_step == 2)
            {
                // On projette le point de fin sur le rayon défini par le point de départ
                float radius = Vector2.Distance(_center, _startPoint);
                Vector2 dir = Vector2.Normalize(worldPos - _center);
                Vector2 finalEnd = _center + dir * radius;

                var arc = new ArcEntity(_center, _startPoint, finalEnd, Canvas.CurrentDrawingColor, Canvas.CurrentThickness)
                {
                    LayerName = Canvas.ActiveLayer.Name
                };
                Canvas.Entities.Add(arc);
                Canvas.SetSingleSelection(arc);
                Finish();
            }
        }

        public override void OnMouseMove(Vector2 worldPos, MouseEventArgs e)
        {
            if (_step == 1 && Canvas.PreviewLine != null)
            {
                Canvas.PreviewLine.End = worldPos;
            }
            else if (_step == 2 && Canvas.PreviewArc != null)
            {
                // On force le point de fin de la preview à rester sur le rayon
                float radius = Vector2.Distance(_center, _startPoint);
                Vector2 dir = Vector2.Normalize(worldPos - _center);
                if (!float.IsNaN(dir.X))
                {
                    Canvas.PreviewArc.EndPoint = _center + dir * radius;
                }
            }
        }

        private void Finish()
        {
            _step = 0;
            Canvas.PreviewLine = null;
            Canvas.PreviewArc = null;
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
                0 => "ARC Centre-Départ-Fin : Cliquez pour placer le CENTRE.",
                1 => "ARC Centre-Départ-Fin : Cliquez pour placer le point de DÉPART (définit le rayon).",
                2 => "ARC Centre-Départ-Fin : Cliquez pour placer le point de FIN.",
                _ => ""
            };
        }
    }
}