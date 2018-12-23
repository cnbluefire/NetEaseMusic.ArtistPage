using NetEaseMusic.ArtistPage.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NetEaseMusic.ArtistPage.Services
{
    public class SongService
    {
        public async Task<IList<HotSongModel>> GetHotSongList()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/gem.json"));
            var str = string.Empty;
            using (var reader = new StreamReader(await file.OpenStreamForReadAsync()))
            {
                str = reader.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<IList<HotSongModel>>(str);
        }
    }
}
