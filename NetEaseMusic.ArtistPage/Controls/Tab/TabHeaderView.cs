using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace NetEaseMusic.ArtistPage.Controls.Tab
{
    internal sealed class TabHeaderView : ListBox, ITabHeader
    {
        public TabHeaderView()
        {
            this.DefaultStyleKey = typeof(TabHeaderView);
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            this.SelectionChanged += OnSelectionChanged;
        }

        #region Field

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
        ExpressionAnimation ProgressExpression;
        ExpressionAnimation IndicatorOffsetExpression;
        ExpressionAnimation IndicatorSizeExpression;
        ExpressionAnimation BaseOffsetXExpression;
        ExpressionAnimation PropertySetWidthExpression;
        ExpressionAnimation PropertySetOffsetXExpression;

        #endregion Field

        #region Overrides
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ScrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            ScrollViewerVisual = ElementCompositionPreview.GetElementVisual(ScrollViewer);
            HeaderScrollPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(ScrollViewer);
            TrySetupComposition();
            this.SizeChanged += OnSizeChanged;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TabHeaderItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TabHeaderItem();
        }

        #endregion Overrides

        #region Private Methods

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

        private void TrySetupComposition()
        {
            if (this.ScrollPropertySet == null) return;
            if (Indicator == null)
            {
                PropSet = Compositor.CreatePropertySet();
                PropSet.InsertScalar("Progress", 0f);
                PropSet.InsertScalar("BaseOffsetX", 0f);
                PropSet.InsertScalar("OffsetX", 0f);
                PropSet.InsertScalar("Width", 0f);
                PropSet.InsertScalar("LeftWidth", 0f);
                PropSet.InsertScalar("RightWidth", 0f);
                PropSet.InsertScalar("SelectedIndex", 0f);
                PropSet.InsertScalar("NowOffsetX", 0f);
                PropSet.InsertScalar("NowWidth", 0f);
                PropSet.InsertScalar("TabsWidth", Convert.ToSingle(_TabsWidth));

                Indicator = Compositor.CreateSpriteVisual();
                Indicator.Brush = Window.Current.Compositor.CreateColorBrush(IndicatorColor);
            }

            TrySetupExpression();
            TrySetIndicator();
        }

        private void TrySetupExpression()
        {
            if (Indicator != null && ScrollViewerVisual != null && ScrollPropertySet != null)
            {
                if (ProgressExpression == null)
                {
                    ProgressExpression = Compositor.CreateExpressionAnimation("-((scroll.Translation.X + (propset.SelectedIndex * propset.TabsWidth)) / propset.TabsWidth)");
                    ProgressExpression.SetReferenceParameter("scroll", ScrollPropertySet);
                    ProgressExpression.SetReferenceParameter("propset", PropSet);
                    PropSet.StartAnimation("Progress", ProgressExpression);
                }

                if (IndicatorOffsetExpression == null)
                {
                    IndicatorOffsetExpression = Compositor.CreateExpressionAnimation("Vector3(propset.BaseOffsetX + propset.OffsetX + 5, host.Size.Y - target.Size.Y, 0)");
                    IndicatorOffsetExpression.SetReferenceParameter("propset", PropSet);
                    IndicatorOffsetExpression.SetReferenceParameter("host", ScrollViewerVisual);
                    IndicatorOffsetExpression.SetReferenceParameter("target", Indicator);
                    Indicator.StartAnimation("Offset", IndicatorOffsetExpression);
                }

                if (IndicatorSizeExpression == null)
                {
                    IndicatorSizeExpression = Compositor.CreateExpressionAnimation("Vector2(propset.Width - 10,4)");
                    IndicatorSizeExpression.SetReferenceParameter("propset", PropSet);
                    Indicator.StartAnimation("Size", IndicatorSizeExpression);
                }

                if (BaseOffsetXExpression == null)
                {
                    BaseOffsetXExpression = Compositor.CreateExpressionAnimation("headerScroll.Translation.X");
                    BaseOffsetXExpression.SetReferenceParameter("headerScroll", HeaderScrollPropertySet);
                    PropSet.StartAnimation("BaseOffsetX", BaseOffsetXExpression);
                }

                if (PropertySetWidthExpression == null)
                {
                    PropertySetWidthExpression = Compositor.CreateExpressionAnimation("propset.Progress > 0 ? (propset.NowWidth + (propset.RightWidth - propset.NowWidth) * propset.Progress) : (propset.NowWidth - (propset.LeftWidth - propset.NowWidth) * propset.Progress)");
                    PropertySetWidthExpression.SetReferenceParameter("propset", PropSet);
                    PropSet.StartAnimation("Width", PropertySetWidthExpression);
                }

                if (PropertySetOffsetXExpression == null)
                {
                    PropertySetOffsetXExpression = Compositor.CreateExpressionAnimation("propset.Progress > 0 ? (propset.NowOffsetX + propset.Progress * propset.NowWidth) : (propset.NowOffsetX + propset.Progress * propset.LeftWidth)");
                    PropertySetOffsetXExpression.SetReferenceParameter("propset", PropSet);
                    PropSet.StartAnimation("OffsetX", PropertySetOffsetXExpression);
                }
            }
        }

        private void TrySetIndicator()
        {
            if (Indicator != null && ScrollViewer != null)
            {
                ElementCompositionPreview.SetElementChildVisual(ScrollViewer, null);
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

        private void SetLeftRightWidth(int Index)
        {
            if (Index > 0)
            {
                PropSet.InsertScalar("LeftWidth", Convert.ToSingle(ContainerWidths[Index - 1]));
                if (Index < ContainerWidths.Count - 1)
                {
                    PropSet.InsertScalar("RightWidth", Convert.ToSingle(ContainerWidths[Index + 1]));
                }
                else
                {
                    PropSet.InsertScalar("RightWidth", 0f);
                }
            }
            else
            {
                PropSet.InsertScalar("LeftWidth", 0f);
                if (Index < ContainerWidths.Count - 1)
                {
                    PropSet.InsertScalar("RightWidth", Convert.ToSingle(ContainerWidths[Index + 1]));
                }
                else
                {
                    PropSet.InsertScalar("RightWidth", 0f);
                }
            }
        }

        #endregion Private Methods

        #region Interface Methods

        void ITabHeader.SetTabsWidth(double Width)
        {
            _TabsWidth = Width;
            if (PropSet != null)
            {
                PropSet.InsertScalar("TabsWidth", Convert.ToSingle(Width));
            }
        }

        void ITabHeader.SetTabsRootScrollPropertySet(CompositionPropertySet ScrollPropertySet)
        {
            this.ScrollPropertySet = ScrollPropertySet;
            TrySetupComposition();
        }

        async void ITabHeader.OnTabsLoaded()
        {
            if (SelectedIndex > -1)
            {
                PropSet.InsertScalar("SelectedIndex", SelectedIndex);
            }
            UpdateContainerWidths();
            await Task.Delay(100);
        }

        void ITabHeader.SyncSelection(int Index)
        {
            SetLeftRightWidth(Index);

            PropSet.InsertScalar("NowOffsetX", Convert.ToSingle(ContainerWidths.Take(Index).Sum()));
            PropSet.InsertScalar("SelectedIndex", Math.Max(Index, 0));
            if (Index > -1)
            {
                PropSet.InsertScalar("NowWidth", Convert.ToSingle(ContainerWidths[Index]));
            }
        }

        #endregion Interface Methods

        #region Event Methods

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _IsLoaded = true;
            TrySetupComposition();
            UpdateContainerWidths();
            if (SelectedIndex > -1)
            {
                PropSet.InsertScalar("NowWidth", Convert.ToSingle(ContainerWidths[SelectedIndex]));
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _IsLoaded = false;

            if (ScrollViewer != null)
            {
                ElementCompositionPreview.SetElementChildVisual(ScrollViewer, null);
            }

            ScrollPropertySet?.Dispose();
            ScrollPropertySet = null;

            HeaderScrollPropertySet?.Dispose();
            HeaderScrollPropertySet = null;

            PropSet?.Dispose();
            PropSet = null;

            Indicator?.Dispose();
            Indicator = null;

            ScrollViewerVisual?.Dispose();
            ScrollViewerVisual = null;

            ProgressExpression?.Dispose();
            ProgressExpression = null;

            IndicatorOffsetExpression?.Dispose();
            IndicatorOffsetExpression = null;

            IndicatorSizeExpression?.Dispose();
            IndicatorSizeExpression = null;

            BaseOffsetXExpression?.Dispose();
            BaseOffsetXExpression = null;

            PropertySetWidthExpression?.Dispose();
            PropertySetWidthExpression = null;

            PropertySetOffsetXExpression?.Dispose();
            PropertySetOffsetXExpression = null;

        }


        private void OnSelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            var oldIndex = -1;
            var newIndex = -1;
            if (e.RemovedItems.FirstOrDefault() is object oldItem)
            {
                if (ContainerFromItem(oldItem) is DependencyObject oldContainer)
                {
                    oldIndex = IndexFromContainer(oldContainer);
                }
            }
            if (e.AddedItems.FirstOrDefault() is object newItem)
            {
                if (ContainerFromItem(newItem) is DependencyObject newContainer)
                {
                    newIndex = IndexFromContainer(newContainer);
                }
            }

            InnerSelectionChanged?.Invoke(this, new SelectionChangedEventArgs() { NewIndex = newIndex, OldIndex = oldIndex });
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateContainerWidths();
            if (SelectedIndex > -1)
            {
                PropSet.InsertScalar("NowWidth", Convert.ToSingle(ContainerWidths[SelectedIndex]));
            }
        }

        #endregion Event Methods

        #region Dependency Property

        public Color IndicatorColor
        {
            get { return (Color)GetValue(IndicatorColorProperty); }
            set { SetValue(IndicatorColorProperty, value); }
        }

        public static readonly DependencyProperty IndicatorColorProperty =
            DependencyProperty.Register("IndicatorColor", typeof(Color), typeof(TabHeaderView), new PropertyMetadata(null, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is TabHeaderView sender)
                    {
                        if (sender._IsLoaded)
                        {
                            sender.UpdateIndicatorColor();
                        }
                    }
                }
            }));
        #endregion Dependency Property

        #region Custom Events

        private event SelectionChangedEventHandler InnerSelectionChanged;

        event SelectionChangedEventHandler ITabHeader.SelectionChanged
        {
            add
            {
                InnerSelectionChanged += value;
            }
            remove
            {
                InnerSelectionChanged -= value;
            }
        }

        #endregion Events
    }
}
