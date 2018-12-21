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
        }

        #region Field

        private bool _IsLoaded;

        CancellationTokenSource SizeChangedToken;

        ScrollViewer ScrollViewer;
        ITabHeader TabHeader;

        CompositionPropertySet ScrollPropertySet;

        int NowScrollIndex = -1;
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
            return (int)Math.Round(ScrollViewer.HorizontalOffset / ScrollViewer.ActualWidth);
        }

        private void TrySetupComposition()
        {
            if (TabHeader != null)
            {
                ScrollPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(ScrollViewer);
                TabHeader.SetTabsRootScrollPropertySet(ScrollPropertySet);
            }
        }

        private void SyncSelectedIndex(int Index, bool disableAnimation = false)
        {
            if (SelectedIndex > -1)
            {
                (ContainerFromIndex(Index) as ContentControl)?.StartBringIntoView(new BringIntoViewOptions() { AnimationDesired = !(disableAnimation) });
            }
            else
            {
                ScrollViewer.ChangeView(0, null, null, true);
            }
        }

        private void UpdateSelectedIndex(int NewIndex, int OldIndex)
        {
            if (OldIndex > -1)
            {
                var oldContainer = ContainerFromIndex(OldIndex);
                if (oldContainer is TabItem oldTabsItem)
                {
                    oldTabsItem.Selected = false;
                }
            }
            if (NewIndex > -1)
            {
                var newContainer = ContainerFromIndex(NewIndex);
                if (newContainer is TabItem newTabsItem)
                {
                    newTabsItem.Selected = true;
                    SelectedIndex = NewIndex;
                    SelectedItem = NewIndex;
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

        #endregion Private Methods

        #region Events

        private void OnHeaderSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SyncSelectedIndex(e.NewIndex);
            UpdateSelectedIndex(e.NewIndex,e.OldIndex);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
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
            _IsLoaded = true;
            UpdateSelectedIndex(SelectedIndex, -1);
            SyncSelectedIndex(SelectedIndex, true);
            TabHeader.OnTabsLoaded();
        }

        private void OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            var tmp = (int)((e.NextView.HorizontalOffset) / ScrollViewer.ActualWidth);

            if (NowScrollIndex != tmp)
            {
                TabHeader.SyncSelection(tmp);
            }
            NowScrollIndex = tmp;
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
                }
            }
            TabHeader.SetTabsWidth(e.NewSize.Width);

            SizeChangedToken?.Cancel();
            SizeChangedToken = new CancellationTokenSource();
            Task.Run(() => Task.Delay(50), SizeChangedToken.Token)
                .ContinueWith(
                    async (t) => await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
                        () => SyncSelectedIndex(SelectedIndex, true)));
        }

        #endregion Events

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

        public static readonly DependencyProperty IndicatorColorProperty =
            DependencyProperty.Register("IndicatorColor", typeof(Color), typeof(Tab), new PropertyMetadata(null));

        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(Tab), new PropertyMetadata(null));

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
