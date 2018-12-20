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

namespace NetEaseMusic.ArtistPage.Controls.Tabs
{
    [ContentProperty(Name = "Items")]
    public sealed class Tabs : ItemsControl
    {
        public Tabs()
        {
            this.DefaultStyleKey = typeof(Tabs);
            this.Loaded += OnLoaded;
        }

        #region Field

        private bool _IsLoaded;

        ObservableCollection<object> Headers = new ObservableCollection<object>();
        CancellationTokenSource SizeChangedToken;

        ScrollViewer ScrollViewer;
        TabsHeaderView TabsHeaderView;

        CompositionPropertySet ScrollPropertySet;
        #endregion Field

        #region Overrides

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ScrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            TabsHeaderView = GetTemplateChild("TabsHeaderView") as TabsHeaderView;

            TabsHeaderView.ItemsSource = Headers;
            TabsHeaderView.SelectionChanged += OnHeaderSelectionChanged;

            this.SizeChanged += OnSizeChanged;

            SetupComposition();
        }


        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TabsItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TabsItem;
        }

        #endregion Overrides

        #region Private Methods

        private void SetupComposition()
        {
            ScrollPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(ScrollViewer);
            TabsHeaderView.SetTabsRootScrollPropertySet(ScrollPropertySet);
        }

        private void UpdateSelectedIndex(int Index, bool disableAnimation = false)
        {
            if (SelectedIndex > -1)
            {
                (ContainerFromIndex(Index) as ContentControl)?.StartBringIntoView(new BringIntoViewOptions() { AnimationDesired = !disableAnimation });
            }
            else
            {
                ScrollViewer.ChangeView(0, null, null, true);
            }
        }

        private void SyncSelectedIndex(int NewIndex, int OldIndex)
        {
            if (OldIndex > -1)
            {
                var oldContainer = ContainerFromIndex(OldIndex);
                if (oldContainer is TabsItem oldTabsItem)
                {
                    oldTabsItem.Selected = false;
                }
            }
            if (NewIndex > -1)
            {
                var newContainer = ContainerFromIndex(NewIndex);
                if (newContainer is TabsItem newTabsItem)
                {
                    newTabsItem.Selected = true;
                    SelectedIndex = NewIndex;
                    SelectedItem = NewIndex;
                    TabsHeaderView.SelectedIndex = NewIndex;
                    OnTabsSelectionChanged(NewIndex, OldIndex);
                }
                else
                {
                    OnTabsSelectionChanged(-1, OldIndex);
                }
            }
            else
            {
                OnTabsSelectionChanged(-1, OldIndex);
            }
        }

        private void SyncSelectedIndex(object NewItem, object OldItem)
        {
            SyncSelectedIndex(Items.IndexOf(NewItem), Items.IndexOf(OldItem));
        }

        #endregion Private Methods

        #region Events

        private void OnHeaderSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedIndex(TabsHeaderView.SelectedIndex);
            SyncSelectedIndex(TabsHeaderView.SelectedIndex, SelectedIndex);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var header in Items.Select(c => (ContainerFromItem(c) as TabsItem)?.Header))
            {
                Headers.Add(header);
            }
            if (Items.Count > 0)
            {
                if (SelectedIndex == -1)
                {
                    SyncSelectedIndex(0, -1);
                }
                else
                {
                    SyncSelectedIndex(SelectedIndex, -1);
                }
            }
            _IsLoaded = true;
            SyncSelectedIndex(SelectedIndex, -1);
            UpdateSelectedIndex(SelectedIndex, true);
            TabsHeaderView.OnTabsLoaded();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ScrollViewer == null) return;
            foreach (var item in Items)
            {
                if (ContainerFromItem(item) is TabsItem tabsItem)
                {
                    tabsItem.Width = ScrollViewer.ActualWidth;
                }
            }
            TabsHeaderView.SetTabsWidth(e.NewSize.Width);

            SizeChangedToken?.Cancel();
            SizeChangedToken = new CancellationTokenSource();
            Task.Run(() => Task.Delay(50), SizeChangedToken.Token)
                .ContinueWith(
                    async (t) => await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
                        () => UpdateSelectedIndex(SelectedIndex, true)));
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
            DependencyProperty.Register("IndicatorColor", typeof(Color), typeof(Tabs), new PropertyMetadata(null));

        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(Tabs), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(Tabs), new PropertyMetadata(null, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is Tabs sender)
                    {
                        if (sender._IsLoaded)
                        {
                            sender.SyncSelectedIndex(a.NewValue, a.OldValue);
                        }
                    }
                }
            }));

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(Tabs), new PropertyMetadata(-1, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is Tabs sender)
                    {
                        if (sender._IsLoaded)
                        {
                            sender.SyncSelectedIndex((int)a.NewValue, (int)a.OldValue);
                        }
                    }
                }
            }));

        #endregion Dependency Properties

        #region Custom Events

        public event TabsSelectionChangedEvent TabsSelectionChanged;
        private void OnTabsSelectionChanged(int NewIndex, int OldIndex)
        {
            TabsSelectionChanged?.Invoke(this, new TabsSelectionChangedEventArgs() { NewIndex = NewIndex, OldIndex = OldIndex });
        }

        #endregion Custom Events
    }

    public delegate void TabsSelectionChangedEvent(Tabs sender, TabsSelectionChangedEventArgs args);

    public class TabsSelectionChangedEventArgs : EventArgs
    {
        public int NewIndex { get; set; }
        public int OldIndex { get; set; }
    }
}
