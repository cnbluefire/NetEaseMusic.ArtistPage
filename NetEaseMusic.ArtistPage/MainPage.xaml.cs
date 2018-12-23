using NetEaseMusic.ArtistPage.Models;
using NetEaseMusic.ArtistPage.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace NetEaseMusic.ArtistPage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            songService = new SongService();
            HotSongs = new ObservableCollection<HotSongModel>();
        }

        SongService songService;
        ObservableCollection<HotSongModel> HotSongs { get; set; }

        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(((FrameworkElement)sender).Name);
        }

        private void ContentBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void HeaderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ContentBorder.Margin = new Thickness(0, e.NewSize.Height, 0, 0);
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ContentBorder.Width = e.NewSize.Width;
            ContentBorder.Height = e.NewSize.Height;
        }

        private async Task LoadAsync()
        {
            var songs = await songService.GetHotSongList();
            foreach(var song in songs)
            {
                HotSongs.Add(song);
            }
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAsync();
        }
    }
}
