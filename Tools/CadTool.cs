using System.Windows.Input;
using System.Numerics;
using FeatherCAD.Models;

namespace FeatherCAD.Tools;

public abstract class CadTool
{
    protected Controls.CadCanvas Canvas;
    private CadToolType _type;


    public CadTool(Controls.CadCanvas canvas)
    {
        Canvas = canvas;
    }

    // Les événements que l'outil peut intercepter
    public abstract void OnMouseDown(Vector2 worldPos, MouseButtonEventArgs e);
    public abstract void OnMouseMove(Vector2 worldPos, MouseEventArgs e);

    public virtual void OnMouseUp(MouseButtonEventArgs e) { }
    public virtual void OnKeyDown(KeyEventArgs e) { }
    public virtual void OnDeactivate() { } // Appelé quand on change d'outil
    public abstract string GetInstruction();
}