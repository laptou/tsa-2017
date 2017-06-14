using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace IvyLock.View.Control
{
    public class RippleEffectDecorator : ContentControl
    {
        static RippleEffectDecorator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RippleEffectDecorator),
                new FrameworkPropertyMetadata(typeof(RippleEffectDecorator)));
        }

        public RippleEffectDecorator() : base()
        {
        }

        public Brush HighlightBackground
        {
            get { return (Brush)GetValue(HighlightBackgroundProperty); }
            set { SetValue(HighlightBackgroundProperty, value); }
        }

        public static readonly DependencyProperty HighlightBackgroundProperty =
            DependencyProperty.Register("HighlightBackground", typeof(Brush), typeof(RippleEffectDecorator),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x7F, 0x00, 0x00, 0x00))));

        private EllipseGeometry ellipse;
        private Grid grid;
        private Storyboard animation;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ellipse = GetTemplateChild("PART_ellipse") as EllipseGeometry;
            grid = GetTemplateChild("PART_grid") as Grid;
            animation = grid.FindResource("PART_animation") as Storyboard;

            //var transform = new TransformGroup();
            //var scale = new ScaleTransform();
            //var translate = new TranslateTransform();
            //transform.Children.Add(scale);
            //transform.Children.Add(translate);
            //ellipse.RenderTransform = transform;

            //animation.Children[1].SetValue(Storyboard.TargetProperty, translate);
            //animation.Children[2].SetValue(Storyboard.TargetProperty, translate);

            AddHandler(MouseDownEvent, new RoutedEventHandler((sender, e) =>
            {
                var targetWidth = Math.Max(ActualWidth, ActualHeight) * 2;
                var mousePosition = (e as MouseButtonEventArgs).GetPosition(this);
                //var startMargin = new Thickness(mousePosition.X, mousePosition.Y, 0, 0);
                //set initial margin to mouse position
                //ellipse.Margin = startMargin;
                //set the to value of the animation that animates the width to the target width
                (animation.Children[0] as DoubleAnimation).To = targetWidth;

                ellipse.Center = mousePosition;

                //(animation.Children[2] as DoubleAnimation).From = mousePosition.X;
                //(animation.Children[2] as DoubleAnimation).To = mousePosition.X; // - targetWidth / 2;
                //(animation.Children[3] as DoubleAnimation).From = mousePosition.Y;
                //(animation.Children[3] as DoubleAnimation).To = mousePosition.Y; // - targetWidth / 2;

                //set the to and from values of the animation that animates the distance relative to the container (grid)
                //(animation.Children[1] as ThicknessAnimation).From = startMargin;
                //(animation.Children[1] as ThicknessAnimation).To = new Thickness(
                //    mousePosition.X - targetWidth / 2,
                //    mousePosition.Y - targetWidth / 2, 0, 0);
                grid.BeginStoryboard(animation);
            }), true);
        }
    }
}