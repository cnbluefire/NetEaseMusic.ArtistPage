using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEaseMusic.ArtistPage.Models
{
    public class HotSongModel : ObservableObject
    {
        private int _Index;
        private string _Name;
        private string _SubName;
        private string _Label;
        private string _Album;

        public int Index
        {
            get => _Index;
            set => Set(ref _Index, value);
        }

        public string Name
        {
            get => _Name;
            set => Set(ref _Name, value);
        }

        public string SubName
        {
            get => _SubName;
            set => Set(ref _SubName, value);
        }

        public string Label
        {
            get => _Label;
            set => Set(ref _Label, value);
        }

        public string Album
        {
            get => _Album;
            set => Set(ref _Album, value);
        }
    }
}
