﻿using Microsoft.Toolkit.Uwp.UI.Extensions;
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
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
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
            AddHandler(PointerPressedEvent, new PointerEventHandler(_PointerPressed), true);
            AddHandler(PointerReleasedEvent, new PointerEventHandler(_PointerReleased), true);
            AddHandler(PointerCanceledEvent, new PointerEventHandler(_PointerReleased), true);
            AddHandler(PointerWheelChangedEvent, new PointerEventHandler(_PointerWheelChanged), true);
        }

        private void _PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            UpdateScrollState();
        }

        private void _PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _LastPointer = null;
            UpdateScrollState();
        }

        private void _PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _LastPointer = e.Pointer;
            UpdateScrollState();
        }

        Pointer _LastPointer;
        SongService songService;
        ObservableCollection<HotSongModel> HotSongs { get; set; }

        private void ContentBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void HeaderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //ContentScrollViewer.Margin = new Thickness(0, InnerHeaderGrid.ActualHeight, 0, 0);
            ContentGrid.Margin = new Thickness(0, HeaderGrid.ActualHeight, 0, 0);
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ContentGrid.Width = e.NewSize.Width;
            ContentGrid.Height = e.NewSize.Height - InnerHeaderGrid.ActualHeight;
            HotSongList.Height = e.NewSize.Height - InnerHeaderGrid.ActualHeight - 44;

        }

        private async Task LoadAsync()
        {
            var songs = await songService.GetHotSongList();
            foreach (var song in songs)
            {
                HotSongs.Add(song);
            }
        }

        ScrollViewer HotSongScrollViewer;

        Visual ImageVisual;
        Visual HeaderVisual;
        Visual InnerHeaderVisual;
        ExpressionAnimation CenterPointBind;
        ExpressionAnimation ScaleBind;
        ExpressionAnimation OffsetYBind;
        CompositionPropertySet ScrollPropertySet;

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {

            ScrollViewer.SetVerticalScrollMode(HotSongList, ScrollMode.Disabled);

            ScrollPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(ContentScrollViewer);
            ImageVisual = ElementCompositionPreview.GetElementVisual(ImageGrid);
            HeaderVisual = ElementCompositionPreview.GetElementVisual(HeaderGrid);
            InnerHeaderVisual = ElementCompositionPreview.GetElementVisual(InnerHeaderGrid);


            CenterPointBind = ImageVisual.Compositor.CreateExpressionAnimation("Vector3(target.Size.X / 2,target.Size.Y / 2,0)");
            CenterPointBind.SetReferenceParameter("target", ImageVisual);
            ImageVisual.StartAnimation("CenterPoint", CenterPointBind);

            ScaleBind = ImageVisual.Compositor.CreateExpressionAnimation("Vector3(1.25 + (max(scroll.Translation.Y ,0)) / 200, 1.25 + (max(scroll.Translation.Y ,0)) / 200 ,0)");
            ScaleBind.SetReferenceParameter("scroll", ScrollPropertySet);
            ImageVisual.StartAnimation("Scale", ScaleBind);

            OffsetYBind = ImageVisual.Compositor.CreateExpressionAnimation("Clamp(scroll.Translation.Y / 2,(innerheader.Size.Y - header.Size.Y) / 2 ,0)");
            OffsetYBind.SetReferenceParameter("scroll", ScrollPropertySet);
            OffsetYBind.SetReferenceParameter("header", HeaderVisual);
            OffsetYBind.SetReferenceParameter("innerheader", InnerHeaderVisual);
            ImageVisual.StartAnimation("Offset.Y", OffsetYBind);

            await LoadAsync();
        }

        private void HotSongScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            UpdateScrollState();

        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            //if (sv.VerticalOffset < HeaderGrid.ActualHeight - InnerHeaderGrid.ActualHeight)
            //{
            //    ScrollViewer.SetVerticalScrollMode(HotSongList, ScrollMode.Disabled);
            //}
            //else
            //{
            //    ScrollViewer.SetVerticalScrollMode(HotSongList, ScrollMode.Auto);
            //}
            UpdateScrollState();
            LightDismiss.Opacity = (sv.VerticalOffset / (HeaderGrid.ActualHeight - InnerHeaderGrid.ActualHeight)) * 0.5 + 0.2;
        }

        private void UpdateScrollState()
        {
            if (HotSongScrollViewer == null) return;
            if (ContentScrollViewer.VerticalOffset < HeaderGrid.ActualHeight - InnerHeaderGrid.ActualHeight)
            {
                if (HotSongScrollViewer.VerticalScrollMode != ScrollMode.Disabled)
                {
                    ScrollViewer.SetVerticalScrollMode(HotSongList, ScrollMode.Disabled);
                }
            }
            else
            {
                if (HotSongScrollViewer.VerticalScrollMode != ScrollMode.Auto)
                {
                    ScrollViewer.SetVerticalScrollMode(HotSongList, ScrollMode.Auto);
                }
                else
                {
                    ScrollViewer.SetVerticalScrollMode(HotSongList, ScrollMode.Disabled);
                }
            }
        }

        private void HotSongList_Loaded(object sender, RoutedEventArgs e)
        {
            HotSongScrollViewer = HotSongList.FindDescendant<ScrollViewer>();

            HotSongScrollViewer.ViewChanged += HotSongScrollViewer_ViewChanged;

        }
    }
}
