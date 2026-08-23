using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Windows.Media;


namespace FeatherCAD.Models;

// Types d'outils disponibles dans l'application CAD
public enum CadToolType
{
    Select,
    Line,
    MultiLine,
    Circle_cp,
    Circle_2P,
    Circle_3P,
    Arc_cp,
    Arc_3P
}
// Un élément de sous-outil, utilisé pour représenter un outil dans l'interface utilisateur
public class SubToolItem
{
    public ImageSource Icon { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
    public string ToolTip { get; set; } = string.Empty;
}
// Types de snap disponibles pour l'accrochage
public enum SnapType { None, End, Mid, Alignment, Perpendiculaire, Intersection }
// Une ligne d'alignement, définie par un point d'origine et une direction
public struct AlignmentLine
{
    public Vector2 Origin;    // Le point d'où part la ligne (ex: le coin d'un mur)
    public Vector2 Direction; // La direction (ex: Vector2.UnitX pour horizontal)
    public string Label;      // Le nom (ex: "45°")
}
// Résultat d'un snap, incluant le point dans le monde, le type de snap et un label descriptif
public struct SnapResult
{
    public Vector2 WorldPoint;
    public SnapType Type;
    public string Label;

    public List<AlignmentLine> ActiveAlignments;
}
