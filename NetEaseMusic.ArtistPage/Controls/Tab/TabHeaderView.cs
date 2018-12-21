﻿using System;
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
        ExpressionAnimation Indicator_OffsetExpression;
        ExpressionAnimation Indicator_SizeExpression;
        ExpressionAnimation PropertySet_ProgressExpression;
        ExpressionAnimation PropertySet_ActualWidthExpression;
        ExpressionAnimation PropertySet_ActualHeightExpression;
        ExpressionAnimation PropertySet_BaseOffsetXExpression;
        ExpressionAnimation PropertySet_WidthExpression;
        ExpressionAnimation PropertySet_OffsetXExpression;

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
                PropSet.InsertScalar("ProgressWithDelay", 0f);
                PropSet.InsertScalar("ProgressWithoutDelay", 0f);
                PropSet.InsertScalar("BaseOffsetX", 0f);
                PropSet.InsertScalar("OffsetX", 0f);
                PropSet.InsertScalar("Width", 0f);
                PropSet.InsertScalar("ActualWidth", 0f);
                PropSet.InsertScalar("ActualHeight", 0f);
                PropSet.InsertScalar("LeftWidth", 0f);
                PropSet.InsertScalar("RightWidth", 0f);
                PropSet.InsertScalar("SelectedIndex", 0f);
                PropSet.InsertScalar("NowOffsetX", 0f);
                PropSet.InsertScalar("NowWidth", 0f);
                PropSet.InsertScalar("SuggestionWidth", Convert.ToSingle(IndicatorWidth));
                PropSet.InsertScalar("SuggestionHeight", Convert.ToSingle(IndicatorHeight));
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
                if (Indicator_OffsetExpression == null)
                {
                    Indicator_OffsetExpression = Compositor.CreateExpressionAnimation("Vector3(propset.BaseOffsetX + propset.OffsetX + (propset.Width - propset.ActualWidth) / 2, host.Size.Y - target.Size.Y, 0)");
                    Indicator_OffsetExpression.SetReferenceParameter("propset", PropSet);
                    Indicator_OffsetExpression.SetReferenceParameter("host", ScrollViewerVisual);
                    Indicator_OffsetExpression.SetReferenceParameter("target", Indicator);
                    Indicator.StartAnimation("Offset", Indicator_OffsetExpression);
                }

                if (Indicator_SizeExpression == null)
                {
                    Indicator_SizeExpression = Compositor.CreateExpressionAnimation("Vector2(propset.ActualWidth, propset.ActualHeight)");
                    Indicator_SizeExpression.SetReferenceParameter("propset", PropSet);
                    Indicator.StartAnimation("Size", Indicator_SizeExpression);
                }

                if (PropertySet_ProgressExpression == null)
                {
                    PropertySet_ProgressExpression = Compositor.CreateExpressionAnimation("-((scroll.Translation.X + (propset.SelectedIndex * propset.TabsWidth)) / propset.TabsWidth)");
                    PropertySet_ProgressExpression.SetReferenceParameter("scroll", ScrollPropertySet);
                    PropertySet_ProgressExpression.SetReferenceParameter("propset", PropSet);
                    PropSet.StartAnimation("Progress", PropertySet_ProgressExpression);
                }

                if (PropertySet_ActualWidthExpression == null)
                {
                    PropertySet_ActualWidthExpression = Compositor.CreateExpressionAnimation("propset.SuggestionWidth == 0 ? (propset.Width - 10) : (min(propset.SuggestionWidth, propset.Width - 10))");
                    PropertySet_ActualWidthExpression.SetReferenceParameter("propset", PropSet);
                    PropSet.StartAnimation("ActualWidth", PropertySet_ActualWidthExpression);
                }

                if (PropertySet_ActualHeightExpression == null)
                {
                    PropertySet_ActualHeightExpression = Compositor.CreateExpressionAnimation("propset.SuggestionHeight == 0 ? (4) : (max(propset.SuggestionHeight, 4))");
                    PropertySet_ActualHeightExpression.SetReferenceParameter("propset", PropSet);
                    PropSet.StartAnimation("ActualHeight", PropertySet_ActualHeightExpression);
                }

                if (PropertySet_BaseOffsetXExpression == null)
                {
                    PropertySet_BaseOffsetXExpression = Compositor.CreateExpressionAnimation("headerScroll.Translation.X");
                    PropertySet_BaseOffsetXExpression.SetReferenceParameter("headerScroll", HeaderScrollPropertySet);
                    PropSet.StartAnimation("BaseOffsetX", PropertySet_BaseOffsetXExpression);
                }

                if (PropertySet_WidthExpression == null)
                {
                    PropertySet_WidthExpression = Compositor.CreateExpressionAnimation("propset.Progress > 0 ? (propset.NowWidth + (propset.RightWidth - propset.NowWidth) * propset.Progress) : (propset.NowWidth - (propset.LeftWidth - propset.NowWidth) * propset.Progress)");
                    PropertySet_WidthExpression.SetReferenceParameter("propset", PropSet);
                    PropSet.StartAnimation("Width", PropertySet_WidthExpression);
                }

                if (PropertySet_OffsetXExpression == null)
                {
                    PropertySet_OffsetXExpression = Compositor.CreateExpressionAnimation("propset.Progress > 0 ? (propset.NowOffsetX + propset.Progress * propset.NowWidth) : (propset.NowOffsetX + propset.Progress * propset.LeftWidth)");
                    PropertySet_OffsetXExpression.SetReferenceParameter("propset", PropSet);
                    PropSet.StartAnimation("OffsetX", PropertySet_OffsetXExpression);
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

        private void UpdateIndicatorSize()
        {
            if (PropSet != null)
            {
                PropSet.InsertScalar("SuggestionWidth", Convert.ToSingle(IndicatorWidth));
                PropSet.InsertScalar("SuggestionHeight", Convert.ToSingle(IndicatorHeight));
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
            await Task.Delay(100);
            UpdateContainerWidths();
            if (SelectedIndex > -1)
            {
                PropSet.InsertScalar("SelectedIndex", SelectedIndex);
            }
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

        public double IndicatorWidth
        {
            get { return (double)GetValue(IndicatorWidthProperty); }
            set { SetValue(IndicatorWidthProperty, value); }
        }

        public double IndicatorHeight
        {
            get { return (double)GetValue(IndicatorHeightProperty); }
            set { SetValue(IndicatorHeightProperty, value); }
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

        public static readonly DependencyProperty IndicatorWidthProperty =
            DependencyProperty.Register("IndicatorWidth", typeof(double), typeof(TabHeaderView), new PropertyMetadata(0d, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is TabHeaderView sender)
                    {
                        if (sender._IsLoaded)
                        {
                            sender.UpdateIndicatorSize();
                        }
                    }
                }
            }));

        public static readonly DependencyProperty IndicatorHeightProperty =
            DependencyProperty.Register("IndicatorHeight", typeof(double), typeof(TabHeaderView), new PropertyMetadata(0d, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is TabHeaderView sender)
                    {
                        if (sender._IsLoaded)
                        {
                            sender.UpdateIndicatorSize();
                        }
                    }
                }
            }));

        #endregion Dependency Property
    }
}
