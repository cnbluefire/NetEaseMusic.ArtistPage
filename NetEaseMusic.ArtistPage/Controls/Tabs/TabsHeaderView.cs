using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace NetEaseMusic.ArtistPage.Controls.Tabs
{
    internal sealed class TabsHeaderView : ListBox
    {
        public TabsHeaderView()
        {
            this.DefaultStyleKey = typeof(TabsHeaderView);
            this.SelectionChanged += TabsHeaderView_SelectionChanged;
            this.Loaded += OnLoaded;
        }

        private const double AnimationDuration = 0.5d;
        private const float LeftMiddle = 0.35f;
        private const float Middle = 0.5f;
        private const float RightMiddle = 0.65f;

        private bool _IsLoaded;
        private double _TabsWidth;

        List<double> ContainerWidths = new List<double>();

        ScrollViewer ScrollViewer;

        Compositor Compositor => Window.Current.Compositor;
        CompositionPropertySet ScrollPropertySet;
        CompositionPropertySet HeaderScrollPropertySet;
        CompositionPropertySet PropSet;
        SpriteVisual Indicator;
        Visual ScrollViewerVisual;
        ExpressionAnimation IndicatorOffsetExpression;
        ExpressionAnimation IndicatorSizeExpression;
        ExpressionAnimation BaseOffsetXExpression;
        ScalarKeyFrameAnimation OffsetAnimation;
        ScalarKeyFrameAnimation WidthAnimation;
        ScalarKeyFrameAnimation ScaleAnimation;
        ScalarKeyFrameAnimation CenterPointAnimation;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ScrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;

            ScrollViewerVisual = ElementCompositionPreview.GetElementVisual(ScrollViewer);
            HeaderScrollPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(ScrollViewer);
            SetupExpression();
            SetIndicator();
            this.SizeChanged += OnSizeChanged;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TabsHeaderItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TabsHeaderItem();
        }

        private void UpdateContainerWidths()
        {
            int count = 0;
            if (ItemsSource is ICollection collection)
            {
                count = collection.Count;
            }
            else count = Items.Count;

            ContainerWidths.Clear();

            for (int i = 0; i < count; i++)
            {
                if (ContainerFromIndex(i) is FrameworkElement ele)
                {
                    ContainerWidths.Add(ele.ActualWidth);
                }
                else
                {
                    ContainerWidths.Add(0);
                }
            }
        }

        private float CalcContainerWidth(int Index)
        {
            if (Index > -1)
            {
                if (ContainerFromIndex(Index) is FrameworkElement container)
                {
                    Convert.ToSingle(container.ActualWidth);
                }
            }
            return 0f;
        }

        private void SetupComposition()
        {
            if (this.ScrollPropertySet == null) return;
            if (Indicator != null) return;

            PropSet = Compositor.CreatePropertySet();
            PropSet.InsertScalar("BaseOffsetX", 0f);
            PropSet.InsertScalar("OffsetX", 0f);
            PropSet.InsertScalar("Width", 0f);
            PropSet.InsertScalar("TabsWidth", Convert.ToSingle(_TabsWidth));

            Indicator = Compositor.CreateSpriteVisual();
            Indicator.Brush = Window.Current.Compositor.CreateColorBrush(IndicatorColor);

            SetupExpression();
            SetIndicator();
        }

        private void SetupExpression()
        {
            if (Indicator != null && ScrollViewerVisual != null)
            {
                IndicatorOffsetExpression = Compositor.CreateExpressionAnimation("Vector3(propset.BaseOffsetX + propset.OffsetX + 5, host.Size.Y - target.Size.Y, 0)");
                IndicatorOffsetExpression.SetReferenceParameter("propset", PropSet);
                IndicatorOffsetExpression.SetReferenceParameter("host", ScrollViewerVisual);
                IndicatorOffsetExpression.SetReferenceParameter("target", Indicator);
                Indicator.StartAnimation("Offset", IndicatorOffsetExpression);

                IndicatorSizeExpression = Compositor.CreateExpressionAnimation("Vector2(propset.Width - 10,4)");
                IndicatorSizeExpression.SetReferenceParameter("propset", PropSet);
                Indicator.StartAnimation("Size", IndicatorSizeExpression);

                BaseOffsetXExpression = Compositor.CreateExpressionAnimation("headerScroll.Translation.X");
                BaseOffsetXExpression.SetReferenceParameter("headerScroll", HeaderScrollPropertySet);
                PropSet.StartAnimation("BaseOffsetX", BaseOffsetXExpression);


                OffsetAnimation = Compositor.CreateScalarKeyFrameAnimation();
                OffsetAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
                OffsetAnimation.InsertExpressionKeyFrame(LeftMiddle, "this.StartingValue");
                OffsetAnimation.Duration = TimeSpan.FromSeconds(AnimationDuration);

                WidthAnimation = Compositor.CreateScalarKeyFrameAnimation();
                WidthAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
                WidthAnimation.InsertExpressionKeyFrame(LeftMiddle, "this.StartingValue");
                WidthAnimation.Duration = TimeSpan.FromSeconds(AnimationDuration);

                ScaleAnimation = Compositor.CreateScalarKeyFrameAnimation();
                ScaleAnimation.InsertKeyFrame(0f, 1f);
                ScaleAnimation.InsertKeyFrame(1f, 1f);
                ScaleAnimation.SetReferenceParameter("target", Indicator);
                ScaleAnimation.Duration = TimeSpan.FromSeconds(AnimationDuration);

                CenterPointAnimation = Compositor.CreateScalarKeyFrameAnimation();
                CenterPointAnimation.SetReferenceParameter("target", Indicator);
                CenterPointAnimation.Duration = TimeSpan.FromSeconds(AnimationDuration);
            }
        }

        private void SetOffsetWithAnimation(double offset)
        {
            OffsetAnimation.InsertKeyFrame(RightMiddle, Convert.ToSingle(offset));
            OffsetAnimation.InsertKeyFrame(1f, Convert.ToSingle(offset));
            PropSet.StartAnimation("OffsetX", OffsetAnimation);
        }

        private void SetWidthWithAnimation(double width)
        {
            WidthAnimation.InsertKeyFrame(RightMiddle, Convert.ToSingle(width));
            WidthAnimation.InsertKeyFrame(1f, Convert.ToSingle(width));
            PropSet.StartAnimation("Width", WidthAnimation);
        }

        private void StartScaleAnimation(bool IsLeftToRight, double length)
        {
            ScaleAnimation.SetScalarParameter("length", Convert.ToSingle(length));
            ScaleAnimation.InsertExpressionKeyFrame(Middle, "1 + length / target.Size.X");
            if (IsLeftToRight)
            {
                CenterPointAnimation.InsertKeyFrame(0f, 0f);
                CenterPointAnimation.InsertKeyFrame(LeftMiddle, 0f);
                CenterPointAnimation.InsertExpressionKeyFrame(RightMiddle, "target.Size.X");
                CenterPointAnimation.InsertExpressionKeyFrame(1f, "target.Size.X");
            }
            else
            {
                CenterPointAnimation.InsertExpressionKeyFrame(0f, "target.Size.X");
                CenterPointAnimation.InsertExpressionKeyFrame(LeftMiddle, "target.Size.X");
                CenterPointAnimation.InsertKeyFrame(RightMiddle, 0f);
                CenterPointAnimation.InsertKeyFrame(1f, 0f);
            }
            Indicator.StartAnimation("Scale.X", ScaleAnimation);
            Indicator.StartAnimation("CenterPoint.X", CenterPointAnimation);
        }

        private void SetIndicator()
        {
            if (Indicator != null && ScrollViewer != null)
            {
                ElementCompositionPreview.SetElementChildVisual(ScrollViewer, Indicator);
            }
        }

        private void UpdateIndicatorColor()
        {
            if (Indicator != null)
            {
                Indicator.Brush = Window.Current.Compositor.CreateColorBrush(IndicatorColor);
            }
        }

        public void SetTabsWidth(double Width)
        {
            _TabsWidth = Width;
            if (PropSet != null)
            {
                PropSet.InsertScalar("TabsWidth", Convert.ToSingle(Width));
            }
        }

        public void SetTabsRootScrollPropertySet(CompositionPropertySet ScrollPropertySet)
        {
            this.ScrollPropertySet = ScrollPropertySet;
            SetupComposition();
        }

        public async void OnTabsLoaded()
        {
            await Task.Delay(100);
            UpdateContainerWidths();
            if (SelectedIndex > -1)
            {
                PropSet.InsertScalar("Width", Convert.ToSingle(ContainerWidths[SelectedIndex]));
                PropSet.InsertScalar("OffsetX", Convert.ToSingle(ContainerWidths.Take(SelectedIndex).Sum()));
            }
        }

        private void TabsHeaderView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_IsLoaded) return;
            if (SelectedIndex < 0)
            {
                PropSet.InsertScalar("Width", 0f);
                PropSet.InsertScalar("OffsetX", 0f);
                return;
            }

            var oldIndex = -1;
            var oldItem = e.RemovedItems.FirstOrDefault();
            if (oldItem != null)
            {
                var oldContainer = ContainerFromItem(oldItem);
                if (oldContainer != null)
                {
                    oldIndex = IndexFromContainer(oldContainer);
                }
            }

            if (oldIndex != -1)
            {
                bool IsLeftToRight = oldIndex < SelectedIndex;
                var length = 0d;
                if (IsLeftToRight)
                {
                    length = ContainerWidths.Skip(oldIndex).Take(SelectedIndex - oldIndex).Sum();
                }
                else
                {
                    length = ContainerWidths.Skip(SelectedIndex).Take(oldIndex - SelectedIndex).Sum();
                }
                StartScaleAnimation(IsLeftToRight, length);
            }

            var offset = ContainerWidths.Take(SelectedIndex).Sum();
            var width = ContainerWidths[SelectedIndex];
            SetOffsetWithAnimation(offset);
            SetWidthWithAnimation(width);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _IsLoaded = true;
            SetupComposition();
            UpdateContainerWidths();
            if (SelectedIndex > -1)
            {
                PropSet.InsertScalar("Width", Convert.ToSingle(ContainerWidths[SelectedIndex]));
                PropSet.InsertScalar("OffsetX", Convert.ToSingle(ContainerWidths.Take(SelectedIndex).Sum()));
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateContainerWidths();
        }

        public Color IndicatorColor
        {
            get { return (Color)GetValue(IndicatorColorProperty); }
            set { SetValue(IndicatorColorProperty, value); }
        }

        public static readonly DependencyProperty IndicatorColorProperty =
            DependencyProperty.Register("IndicatorColor", typeof(Color), typeof(TabsHeaderView), new PropertyMetadata(null, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is TabsHeaderView sender)
                    {
                        if (sender._IsLoaded)
                        {
                            sender.UpdateIndicatorColor();
                        }
                    }
                }
            }));
    }
}
