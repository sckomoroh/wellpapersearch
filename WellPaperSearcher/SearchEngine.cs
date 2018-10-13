using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;

namespace WellPaperSearcher {
    class SearchEngineEventArgs : EventArgs {
        private string url;
        private string thumbnailUrl;
        private string title;

        public string ImageURL
        {
            get 
            {
                return url;
            }
            set
            {
                url = value;
            }
        }

        public string ThumbnailURL
        {
            get
            {
                return thumbnailUrl;
            }
            set
            {
                thumbnailUrl = value;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
            }
        }
    }

    delegate void NewImageDelegate(object source, SearchEngineEventArgs arg);
    delegate void ImageSaved(object sender);

    class ImageUtils
    {
        private string fileName;
        private string fileUrl;
        private string fileDestPath;

        private IActiveDesktop activeDesktop;

        public event ImageSaved ImageSavedEvent = null;
        // --------------------------------------------------------------------
        public void initActiveDesktop()
        {
            activeDesktop = ActiveDesktop.GetActiveDesktop();
        }
        // --------------------------------------------------------------------
        public string saveImage(string url, string title, string destPath)
        {
            string illegal = title + ".jpg";
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach(char c in invalid) {
                illegal = illegal.Replace(c.ToString(), "");
            }

            if(!destPath.EndsWith("\\"))
                destPath += "\\";

            return saveImage(url, destPath + illegal);
        }
        // --------------------------------------------------------------------
        public string saveImage(string url, string destPath)
        {
            fileUrl = url;
            fileDestPath = destPath;
            return saveImage();
        }
        // --------------------------------------------------------------------
        public string saveImage()
        {
            try {
                WebRequest requestPic = WebRequest.Create(fileUrl);
                WebResponse responsePic = requestPic.GetResponse();
                Image webImage = Image.FromStream(responsePic.GetResponseStream());

                webImage.Save(fileDestPath);

                if(ImageSavedEvent != null)
                {
                    ImageSavedEvent(this);
                }

                return fileDestPath;
            }
            catch(Exception ex) {
                return null;
            }
        }
        // --------------------------------------------------------------------
        public string RemoveInvalidSymbols(string sName)
        {
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach(char c in invalid) {
                sName = sName.Replace(c.ToString(), "");
            }

            return sName;
        }
        // --------------------------------------------------------------------
        public void SaveImageThreadProc()
        {
            saveImage();
        }
        // --------------------------------------------------------------------
        public void SaveImageInThread(string url, string destPath)
        {
            fileUrl = url;
            fileDestPath = destPath;

            Thread thread = new Thread(new ThreadStart(this.SaveImageThreadProc));
            thread.Start();
        }
        // --------------------------------------------------------------------
        public Image retreiveImage(string url)
        {
            WebRequest requestPic = WebRequest.Create(url);
            WebResponse responsePic = requestPic.GetResponse();
            Image webImage = Image.FromStream(responsePic.GetResponseStream());

            return webImage;
        }
        // --------------------------------------------------------------------
        public void SetImageAsWellpaper() 
        {
            activeDesktop.SetWallpaper(fileName, 0);
            activeDesktop.ApplyChanges(AD_Apply.ALL);
        }
        // --------------------------------------------------------------------
        public void SetImageAsWellpaper(string filename) 
        {
            fileName = filename;
            SetImageAsWellpaper();
        }
        // --------------------------------------------------------------------
    }

    class SearchEngine {
        private int width = 800;
        private int height = 600;
        private int totalCount = 50;
        private int count = 0;
        private int offset = 0;
        private string searchKeyword = null;
        private Thread engineThread = null;

        const string appId = "16D1BD8FCFCD7290DCB21F8EA299A52641AAC3B9";

        public event NewImageDelegate NewImageEvent;

        // --------------------------------------------------------------------
        public string SearchKeyword
        {
            get
            {
                return searchKeyword;
            }
            set
            {
                searchKeyword = value;
            }
        }
        // --------------------------------------------------------------------
        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }
        // --------------------------------------------------------------------
        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }
        // --------------------------------------------------------------------
        public int Count
        {
            get
            {
                return totalCount;
            }
            set
            {
                totalCount = value;
            }
        }
        // --------------------------------------------------------------------
        public void MakeRequest()
        {
            count = 0;
            offset = 0;
            while(totalCount > offset) 
            {
                if(totalCount - count > offset)
                    if(totalCount > 50)
                        count = 50;
                    else
                        count = totalCount;
                else
                    count = totalCount - offset;

                HttpWebRequest request = _prepareRequest(SearchKeyword);
                _parseResponse((HttpWebResponse)request.GetResponse());
                offset += 50;
                count += 50;
            }
        }
        // --------------------------------------------------------------------
        public void MakeRequest(string searchTerm)
        {
            searchKeyword = searchTerm;
            MakeRequest();
        }
        // --------------------------------------------------------------------
        public void StartInThread(string searchTerm)
        {
            if(engineThread == null || !engineThread.IsAlive)
            {
                searchKeyword = searchTerm;
                engineThread = new Thread(new ThreadStart(this.MakeRequest));
                engineThread.Start();
            }
        }
        // --------------------------------------------------------------------
        public void StartInThread() {
            if(engineThread == null || !engineThread.IsAlive) {
                engineThread = new Thread(new ThreadStart(this.MakeRequest));
                engineThread.Start();
            }
        }
        // --------------------------------------------------------------------
        public bool IsAlive
        {
            get
            {
                if(engineThread == null)
                    return false;
                return engineThread.IsAlive;
            }
        }
        // --------------------------------------------------------------------
        public void Join()
        {
            if(engineThread == null)
                return;
            engineThread.Join();
        }
        // --------------------------------------------------------------------
        protected void _parseResponse(HttpWebResponse response)
        {
            XmlDocument document = new XmlDocument();
            document.Load(response.GetResponseStream());

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(
            document.NameTable);
            nsmgr.AddNamespace(
                "api",
                "http://schemas.microsoft.com/LiveSearch/2008/04/XML/element");

            XmlNodeList errors = document.DocumentElement.SelectNodes(
                "./api:Errors/api:Error",
                nsmgr);

            if(errors.Count > 0) 
            {
                // TODO: implement error handling
            }
            else 
            {
                _parseImages(document.DocumentElement, nsmgr);
            }
        }
        // --------------------------------------------------------------------
        protected void _parseImages(XmlNode root, XmlNamespaceManager nsmgr)
        {
            string imageNamespaceUri =
                        "http://schemas.microsoft.com/LiveSearch/2008/04/XML/multimedia";
            nsmgr.AddNamespace("mms", imageNamespaceUri);

            XmlNode image = root.SelectSingleNode("./mms:Image", nsmgr);
            XmlNodeList results = image.SelectNodes(
                "./mms:Results/mms:ImageResult",
                nsmgr);

            foreach(XmlNode result in results) {
                SearchEngineEventArgs evtArgs = new SearchEngineEventArgs();
                evtArgs.ImageURL = result.SelectSingleNode("./mms:MediaUrl", nsmgr).InnerText;
                evtArgs.Title = result.SelectSingleNode("./mms:Title", nsmgr).InnerText;
                evtArgs.ThumbnailURL = result.SelectSingleNode("./mms:Thumbnail/mms:Url",nsmgr).InnerText;

                if(NewImageEvent != null)
                {
                    NewImageEvent(this, evtArgs);
                }
            }
        }
        // --------------------------------------------------------------------
        protected HttpWebRequest _prepareRequest(string searchTerm)
        {
            string requestString = "http://api.bing.net/xml.aspx"
                                        + "?AppId=" + appId
                                        + "&Query=" + searchTerm
                                        + "&Sources=Image"
                                        + "&Version=2.0"
                                        + "&Market=en-us"
                                        + "&Adult=Moderate"
                                        + "&Image.Count=" + count
                                        + "&Image.Offset=" + offset
                                        + "&Image.Filters=Size:Large+Size:Height:" + height + "+Size:Width:" + width;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(
                requestString);

            return request;
        }
        // --------------------------------------------------------------------
    }
}
