using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Coursee.UI.Views;

/// <summary>
/// An entry's icon (<see cref="IconVisual"/>), <see cref="Size"/> pixels square: an image as it is,
/// a path in the foreground of where it is (a selected row's, say), as a glyph of text would be.
/// </summary>
public sealed class EntryIconView : FrameworkElement
{
    public static readonly DependencyProperty VisualProperty = DependencyProperty.Register(
        nameof(Visual), typeof(IconVisual), typeof(EntryIconView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>One of coursee's icons by name (<see cref="LibraryIcons"/>): sets <see cref="Visual"/>.</summary>
    public static readonly DependencyProperty IconNameProperty = DependencyProperty.Register(
        nameof(IconName), typeof(string), typeof(EntryIconView), new FrameworkPropertyMetadata(null, (view, change) => ((EntryIconView)view).Visual = LibraryIcons.Of(change.NewValue as string)));

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(EntryIconView), new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(EntryIconView), new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    public EntryIconView() => RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);

    /// <summary>The icon; nothing drawn when null, the room kept.</summary>
    public IconVisual? Visual
    {
        get => (IconVisual?)GetValue(VisualProperty);
        set => SetValue(VisualProperty, value);
    }

    public string? IconName
    {
        get => (string?)GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (Visual?.Image is { } image)
        {
            drawingContext.DrawImage(image, new Rect(0, 0, Size, Size));
        }
        else if (Visual is { Geometry: { } geometry, ViewBox: var box })
        {
            // The view box fills the square, centred, its proportions kept.
            var scale = Size / Math.Max(box.Width, box.Height);
            var left = (Size - box.Width * scale) / 2 - box.X * scale;
            var top = (Size - box.Height * scale) / 2 - box.Y * scale;
            drawingContext.PushTransform(new MatrixTransform(scale, 0, 0, scale, left, top));
            drawingContext.DrawGeometry(Foreground, null, geometry);
            drawingContext.Pop();
        }
    }
}
