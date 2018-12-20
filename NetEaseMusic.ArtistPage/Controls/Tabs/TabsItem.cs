using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace NetEaseMusic.ArtistPage.Controls.Tabs
{
    [ContentProperty(Name = "Content")]
    public sealed class TabsItem : ContentControl
    {
        public TabsItem()
        {
            this.DefaultStyleKey = typeof(TabsItem);
        }

        private bool lazyLoaded = false;

        private void LazyLoad()
        {
            if (lazyLoaded) return;
            lazyLoaded = true;
            VisualStateManager.GoToState(this, "Load", false);
        }


        public bool Selected
        {
            get { return (bool)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(bool), typeof(TabsItem), new PropertyMetadata(false, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is TabsItem sender)
                    {
                        if (a.NewValue is true)
                        {
                            sender.LazyLoad();
                        }
                    }
                }
            }));




        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(TabsItem), new PropertyMetadata(null));


    }
}
