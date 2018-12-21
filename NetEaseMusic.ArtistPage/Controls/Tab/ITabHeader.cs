using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;

namespace NetEaseMusic.ArtistPage.Controls.Tab
{
    public interface ITabHeader
    {
        void SetTabsWidth(double Width);
        void SetTabsRootScrollPropertySet(CompositionPropertySet ScrollPropertySet);
        void OnTabsLoaded();
        void SyncSelection(int Index);
        event SelectionChangedEventHandler SelectionChanged;
        ItemCollection Items { get; }
        int SelectedIndex { get; set; }
    }
}
