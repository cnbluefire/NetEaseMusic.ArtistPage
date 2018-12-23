using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace NetEaseMusic.ArtistPage.Controls.Tab
{
    [ContentProperty(Name = "Items")]
    public sealed class Tab : ItemsControl
    {
        public Tab()
        {
            this.DefaultStyleKey = typeof(Tab);
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        #region Field

        private bool _IsLoaded;

        private CancellationTokenSource SizeChangedToken;
        private PointerEventHandler OnPointerWheel;
        private ScrollViewer ScrollViewer;
        private ITabHeader TabHeader;

        private CompositionPropertySet ScrollPropertySet;

        private int NowScrollIndex = -1;
        private int LastActiveIndex = -1;

        #endregion Field

        #region Overrides

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ScrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            TabHeader = GetTemplateChild("TabsHeaderView") as ITabHeader;

            if (TabHeader != null)
            {
                TabHeader.SelectionChanged += OnHeaderSelectionChanged;
            }
            if (ScrollViewer != null)
            {
                ScrollViewer.DirectManipulationStarted += OnDirectManipulationStarted;
                ScrollViewer.DirectManipulationCompleted += OnDirectManipulationCompleted;
                ScrollViewer.ViewChanging += OnViewChanging;

                OnPointerWheel = (s, a) =>
                {
                    if (a.OriginalSource is Grid)
                    {
                        ItemsPanelRoot?.CancelDirectManipulations();
                    }
                };

                ScrollViewer.AddHandler(PointerWheelChangedEvent, OnPointerWheel, true);
            }

            this.SizeChanged += OnSizeChanged;

            TrySetupComposition();
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TabItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TabItem;
        }

        #endregion Overrides

        #region Private Methods


        private int CalcIndex()
        {
            return (int)(ScrollViewer.HorizontalOffset + 2 / ScrollViewer.ActualWidth);
        }

        private void TrySetupComposition()
        {
            if (TabHeader != null)
            {
                ScrollPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(ScrollViewer);
                TabHeader.SetTabsRootScrollPropertySet(ScrollPropertySet);
            }
        }

        /// <summary>
        /// 同步Tab滚动条状态
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="disableAnimation"></param>
        private void SyncSelectedIndex(int Index, bool disableAnimation = false)
        {
            if (SelectedIndex > -1)
            {
                ScrollViewer.ChangeView(Index * ScrollViewer.ActualWidth, null, null, disableAnimation);
                //(ContainerFromIndex(Index) as ContentControl)?.StartBringIntoView(new BringIntoViewOptions() { AnimationDesired = !(disableAnimation) });
            }
            else
            {
                ScrollViewer.ChangeView(0, null, null, true);
            }
        }

        /// <summary>
        /// 更新SelectedIndex和相关属性
        /// </summary>
        /// <param name="NewIndex"></param>
        /// <param name="OldIndex"></param>
        private void UpdateSelectedIndex(int NewIndex, int OldIndex)
        {
            if (NewIndex > -1 && NewIndex != SelectedIndex)
            {
                var newContainer = ContainerFromIndex(NewIndex);
                if (newContainer is ITabItem newTabsItem)
                {
                    SelectedIndex = NewIndex;
                    SelectedItem = Items[NewIndex];
                    TabHeader.SelectedIndex = NewIndex;
                    OnSelectionChanged(NewIndex, OldIndex);
                }
                else
                {
                    OnSelectionChanged(-1, OldIndex);
                }
            }
            else
            {
                OnSelectionChanged(-1, OldIndex);
            }
        }

        private void UpdateSelectedIndex(object NewItem, object OldItem)
        {
            UpdateSelectedIndex(Items.IndexOf(NewItem), Items.IndexOf(OldItem));
        }

        /// <summary>
        /// 同步TabHeader
        /// </summary>
        /// <param name="Index"></param>
        private void SyncTabHeader(int Index)
        {
            if (_IsLoaded && Index > -1 && Index < Items.Count)
            {
                if (Index == LastActiveIndex) return;

                var newContainer = ContainerFromIndex(Index);
                TabHeader.SyncSelection(Index);
                LastActiveIndex = Index;
            }
        }

        /// <summary>
        /// 加载或卸载Container
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="Load"></param>
        private void SetContainerLoadState(int Index, bool Load)
        {
            if (Index > -1 && Index < Items.Count)
            {
                var Container = ContainerFromIndex(Index);
                if (Container is ITabItem TabsItem)
                {
                    if (Load || !Load && TabsItem.UnloadItemOutsideViewport)
                    {
                        TabsItem.UpdateLoadState(Load);
                    }
                }
            }
        }

        #endregion Private Methods

        #region Events Methods

        private void OnHeaderSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SyncSelectedIndex(e.NewIndex);
            UpdateSelectedIndex(e.NewIndex, e.OldIndex);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var header in Items.Select(c => (ContainerFromItem(c) as TabItem)?.Header))
            {
                TabHeader.Items.Add(header);
            }
            if (Items.Count > 0)
            {
                if (SelectedIndex == -1)
                {
                    UpdateSelectedIndex(0, -1);
                }
                else
                {
                    UpdateSelectedIndex(SelectedIndex, -1);
                }
            }

            UpdateSelectedIndex(SelectedIndex, -1);
            SyncSelectedIndex(SelectedIndex, true);
            _IsLoaded = true;
            await TabHeader.OnTabsLoadedAsync();
            SyncTabHeader(SelectedIndex);
            SetContainerLoadState(SelectedIndex, true);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _IsLoaded = false;
            ScrollPropertySet.Dispose();
            ScrollPropertySet = null;
            ScrollViewer.RemoveHandler(PointerWheelChangedEvent, OnPointerWheel);
        }

        private void OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            if (_IsLoaded)
            {
                var tmp = (int)(e.NextView.HorizontalOffset / ScrollViewer.ActualWidth);

                if (tmp != NowScrollIndex)//显示左侧
                {
                    if (tmp != LastActiveIndex)
                    {
                        SyncTabHeader(tmp);
                        SetContainerLoadState(NowScrollIndex - 1, true);
                        SetContainerLoadState(NowScrollIndex + 1, false);
                    }
                }
                else if (tmp + 1 != NowScrollIndex) //显示右侧
                {
                    if (tmp + 1 != LastActiveIndex)
                    {
                        SyncTabHeader(tmp + 1);
                        SetContainerLoadState(tmp + 1, true);
                        SetContainerLoadState(tmp - 1, false);
                    }
                }
                NowScrollIndex = tmp;
            }
        }

        private void OnDirectManipulationStarted(object sender, object e)
        {
            ScrollViewer.HorizontalSnapPointsType = SnapPointsType.MandatorySingle;
        }

        private void OnDirectManipulationCompleted(object sender, object e)
        {
            UpdateSelectedIndex(CalcIndex(), SelectedIndex);
            ScrollViewer.HorizontalSnapPointsType = SnapPointsType.Mandatory;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ScrollViewer == null) return;
            foreach (var item in Items)
            {
                if (ContainerFromItem(item) is TabItem tabsItem)
                {
                    tabsItem.Width = ScrollViewer.ActualWidth;
                    tabsItem.Height = ScrollViewer.ActualHeight;
                }
            }
            TabHeader.SetTabsWidth(e.NewSize.Width);

            SizeChangedToken?.Cancel();
            SizeChangedToken = new CancellationTokenSource();
            Task.Run(() => Task.Delay(50), SizeChangedToken.Token)
                .ContinueWith((t) => SyncSelectedIndex(SelectedIndex, true), TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion Events Methods

        #region Dependency Properties

        public Color IndicatorColor
        {
            get { return (Color)GetValue(IndicatorColorProperty); }
            set { SetValue(IndicatorColorProperty, value); }
        }

        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public object LeftHeader
        {
            get { return (object)GetValue(LeftHeaderProperty); }
            set { SetValue(LeftHeaderProperty, value); }
        }

        public object RightHeader
        {
            get { return (object)GetValue(RightHeaderProperty); }
            set { SetValue(RightHeaderProperty, value); }
        }

        public DataTemplate LeftHeaderTemplate
        {
            get { return (DataTemplate)GetValue(LeftHeaderTemplateProperty); }
            set { SetValue(LeftHeaderTemplateProperty, value); }
        }

        public DataTemplate RightHeaderTemplate
        {
            get { return (DataTemplate)GetValue(RightHeaderTemplateProperty); }
            set { SetValue(RightHeaderTemplateProperty, value); }
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


        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(Tab), new PropertyMetadata(null, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is Tab sender)
                    {
                        if (sender._IsLoaded)
                        {
                            sender.UpdateSelectedIndex(a.NewValue, a.OldValue);
                        }
                    }
                }
            }));

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(Tab), new PropertyMetadata(-1, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is Tab sender)
                    {
                        if (sender._IsLoaded)
                        {
                            sender.UpdateSelectedIndex((int)a.NewValue, (int)a.OldValue);
                        }
                    }
                }
            }));

        public bool SingleSelectionFollowsFocus
        {
            get { return (bool)GetValue(SingleSelectionFollowsFocusProperty); }
            set { SetValue(SingleSelectionFollowsFocusProperty, value); }
        }


        public static readonly DependencyProperty LeftHeaderProperty =
            DependencyProperty.Register("LeftHeader", typeof(object), typeof(Tab), new PropertyMetadata(null));

        public static readonly DependencyProperty RightHeaderProperty =
            DependencyProperty.Register("RightHeader", typeof(object), typeof(Tab), new PropertyMetadata(null));

        public static readonly DependencyProperty LeftHeaderTemplateProperty =
            DependencyProperty.Register("LeftHeaderTemplate", typeof(DataTemplate), typeof(Tab), new PropertyMetadata(null));

        public static readonly DependencyProperty RightHeaderTemplateProperty =
            DependencyProperty.Register("RightHeaderTemplate", typeof(DataTemplate), typeof(Tab), new PropertyMetadata(null));

        public static readonly DependencyProperty IndicatorColorProperty =
            DependencyProperty.Register("IndicatorColor", typeof(Color), typeof(Tab), new PropertyMetadata(null));

        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(Tab), new PropertyMetadata(null));

        public static readonly DependencyProperty IndicatorWidthProperty =
            DependencyProperty.Register("IndicatorWidth", typeof(double), typeof(Tab), new PropertyMetadata(0d));

        public static readonly DependencyProperty IndicatorHeightProperty =
            DependencyProperty.Register("IndicatorHeight", typeof(double), typeof(Tab), new PropertyMetadata(0d));

        public static readonly DependencyProperty SingleSelectionFollowsFocusProperty =
            DependencyProperty.Register("SingleSelectionFollowsFocus", typeof(bool), typeof(Tab), new PropertyMetadata(true));


        #endregion Dependency Properties

        #region Custom Events

        public event TabSelectionChangedEvent SelectionChanged;
        private void OnSelectionChanged(int NewIndex, int OldIndex)
        {
            SelectionChanged?.Invoke(this, new TabSelectionChangedEventArgs() { NewIndex = NewIndex, OldIndex = OldIndex });
        }

        #endregion Custom Events
    }

    public delegate void TabSelectionChangedEvent(Tab sender, TabSelectionChangedEventArgs args);

    public class TabSelectionChangedEventArgs : EventArgs
    {
        public int NewIndex { get; set; }
        public int OldIndex { get; set; }
    }
}
