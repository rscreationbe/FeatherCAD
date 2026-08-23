using FeatherCAD.Logic;
using FeatherCAD.Models;
using System.Linq;
using System.Numerics;
using System.Windows.Input;

namespace FeatherCAD.Tools
{
    public class SelectTool : CadTool
    {
        private bool _isDragging = false;
        private Vector2 _lastMouseWorldPos;

        public SelectTool(Controls.CadCanvas canvas) : base(canvas) { }

        public override void OnMouseDown(Vector2 worldPos, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            _lastMouseWorldPos = worldPos;

            // 1. On vérifie si on clique sur un objet déjà sélectionné (pour le déplacer)
            bool clickedOnSelected = false;
            float threshold = 5.0f / Canvas.Zoom;

            foreach (var entity in Canvas.SelectedEntities)
            {
                if (IsEntityHit(entity, worldPos, threshold))
                {
                    clickedOnSelected = true;
                    break;
                }
            }

            if (clickedOnSelected)
            {
                // On commence le déplacement (Drag)
                _isDragging = true;
            }
            else
            {
                // 2. Sinon, on essaie d'attraper un nouvel objet
                Canvas.HandleSelectionTool(worldPos);

                if (Canvas.SelectedEntities.Count > 0)
                {
                    // Si on vient d'en attraper un, on commence aussi le drag
                    _isDragging = true;
                }
                else
                {
                    // 3. Sinon, on commence un rectangle de sélection
                    Canvas.IsBoxSelecting = true;
                    Canvas.BoxStartWorld = worldPos;
                    Canvas.BoxEndWorld = worldPos;
                }
            }
        }

        public override void OnMouseMove(Vector2 worldPos, MouseEventArgs e)
        {
            if (_isDragging)
            {
                // Calcul du déplacement relatif
                Vector2 delta = worldPos - _lastMouseWorldPos;

                // On déplace toutes les entités sélectionnées
                foreach (var entity in Canvas.SelectedEntities)
                {
                    entity.Move(delta);
                }

                _lastMouseWorldPos = worldPos; // On mémorise pour le prochain pixel
            }
            else if (Canvas.IsBoxSelecting)
            {
                Canvas.BoxEndWorld = worldPos;
            }
        }

        public override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = false;

                if (Canvas.IsBoxSelecting)
                {
                    Canvas.IsBoxSelecting = false;
                    Canvas.CommitBoxSelection(Canvas.BoxStartWorld, Canvas.BoxEndWorld);
                }
            }
        }

        // Méthode d'aide pour savoir si la souris touche une entité spécifique
        private bool IsEntityHit(Entity entity, Vector2 pos, float threshold)
        {
            if (entity is LineEntity l) return GeometryUtils.IsPointNearLine(pos, l.Start, l.End, threshold);
            if (entity is CircleEntity c) return Math.Abs(Vector2.Distance(pos, c.Center) - c.Radius) < threshold;
            if (entity is ArcEntity a) return Math.Abs(Vector2.Distance(pos, a.Center) - a.Radius) < threshold && a.IsPointOnArc(pos, threshold);
            if (entity is GroupEntity g) return g.IsPointInside(pos, threshold);
            return false;
        }

        public override string GetInstruction()
        {
            if (_isDragging) return "DÉPLACEMENT : Relâchez pour valider la position.";

            bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            return isShiftDown ? "Extension de la sélection." : "Sélectionnez : Sélectionnez [Maj = Etendre la sélection].";
        }
    }
}
