using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace WellPaperSearcher {
    public partial class DownloadForm : Form {
        private Dictionary<string, string> files = new Dictionary<string, string>();
        private string destPath;
        private ImageUtils imageUtils = new ImageUtils();
        private int mProgress = 0;

        private delegate void _ImageLoaded();
        private delegate void _LoadComplete();

        public DownloadForm() {
            InitializeComponent();
            imageUtils.ImageSavedEvent +=new ImageSaved(OnImageSavedEvent);
        }

        public void Add(string url, string title)
        {
            files[url] = title;
        }

        public string DestinationPath
        {
            get
            {
                return destPath;
            }

            set
            {
                destPath = value;
            }
        }

        public void Clear()
        {
            files.Clear();
        }

        public void ThreadProc()
        {
            foreach (KeyValuePair<string, string> item in files)
            {
                string sPath = destPath + "\\" + imageUtils.RemoveInvalidSymbols(item.Value) + ".jpg";
                imageUtils.saveImage(item.Key, sPath);
            }

            if(this.InvokeRequired) {
                this.Invoke(new _LoadComplete(OnLoadComplete), new object[] { });
            }

        }

        public void OnImageSavedEvent(object sender)
        {
            if(this.InvokeRequired) {
                mProgress++;
                this.Invoke(new _ImageLoaded(OnImageLoaded), new object[] { });
            }
        }

        public void OnLoadComplete()
        {
            Hide();
        }

        private void OnImageLoaded()
        {
            loadProgressBar.Value = mProgress;
            loadLabel.Text = mProgress + "/" + files.Count;
        }

        private void OnFormShown(object sender, EventArgs e) {
            mProgress = 0;
            loadProgressBar.Maximum = files.Count;
            loadProgressBar.Minimum = 0;

            loadLabel.Text = "0/" + files.Count;

            Thread thread = new Thread(new ThreadStart(ThreadProc));
            thread.Start();
        }
    }
}
