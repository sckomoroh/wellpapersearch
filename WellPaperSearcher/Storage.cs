using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellPaperSearcher {


    class OnlineStorage : Dictionary<int, KeyValuePair<string, string> >
    {
        public int Add(string path, string title)
        {
            int key = _FindFreeId();
            Add(key, new KeyValuePair<string, string>(path, title));
            return key;
        }

        protected int _FindFreeId()
        {
            int key = 0;
            while(this.ContainsKey(key))   
            {
                key++;
            }
            return key;
        }
    }

    class GalleryStorage : Dictionary<int, string>
    {
    }
}
