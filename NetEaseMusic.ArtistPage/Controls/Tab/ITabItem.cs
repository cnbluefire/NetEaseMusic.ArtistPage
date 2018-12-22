﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEaseMusic.ArtistPage.Controls.Tab
{
    public interface ITabItem
    {
        bool UnloadItemOutsideViewport { get; }
        void UpdateLoadState(bool Load);
    }
}
