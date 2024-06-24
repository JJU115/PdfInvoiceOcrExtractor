using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows;

namespace WpfOcrInvoiceExtractor
{
    public class MatrixAnimator : AnimationTimeline
    {
        public Matrix? From
        {
            set { SetValue(FromProperty, value); }
            get { return (Matrix)GetValue(FromProperty); }
        }
        public static DependencyProperty FromProperty = DependencyProperty.Register("From", typeof(Matrix?), typeof(MatrixAnimator), new PropertyMetadata(null));

        public Matrix? To
        {
            set { SetValue(ToProperty, value); }
            get { return (Matrix)GetValue(ToProperty); }
        }
        public static DependencyProperty ToProperty = DependencyProperty.Register("To", typeof(Matrix?), typeof(MatrixAnimator), new PropertyMetadata(null));

        public MatrixAnimator()
        {
        }

        public MatrixAnimator(Matrix from, Matrix to, Duration duration)
        {
            Duration = duration;
            From = from;
            To = to;
        }

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            if (animationClock.CurrentProgress == null)
            {
                return null;
            }

            double progress = animationClock.CurrentProgress.Value;
            Matrix from = From ?? (Matrix)defaultOriginValue;

            if (To.HasValue)
            {
                Matrix to = To.Value;
                Matrix newMatrix = new Matrix(((to.M11 - from.M11) * progress) + from.M11, 0, 0, ((to.M22 - from.M22) * progress) + from.M22,
                                              ((to.OffsetX - from.OffsetX) * progress) + from.OffsetX, ((to.OffsetY - from.OffsetY) * progress) + from.OffsetY);
                return newMatrix;
            }

            return Matrix.Identity;
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new MatrixAnimator();
        }

        public override System.Type TargetPropertyType
        {
            get { return typeof(Matrix); }
        }

    }
}
