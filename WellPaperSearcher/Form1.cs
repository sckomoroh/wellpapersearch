using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace WellPaperSearcher {
    public partial class Form1 : Form {
        SearchEngine searchEngine = new SearchEngine();
        private delegate void _ImageFoundDelegate(Image image, int id);
        private OnlineStorage onlineStorage = new OnlineStorage();
        private Size[] resolutionArray = new Size[15];
        private ImageUtils imageUtils = new ImageUtils();

        public Form1() {
            InitializeComponent();
            searchEngine.NewImageEvent +=new NewImageDelegate(OnImageFound);

            listView1.LargeImageList = itemsImageList;
            searchEngine.Count = 10;
            searchEngine.Height = 1050;
            searchEngine.Width = 1680;

            resolutionArray[0] = new Size(600, 800);
            resolutionArray[1] = new Size(1024,768);
            resolutionArray[2] = new Size(1152,864);
            resolutionArray[3] = new Size(1280,720);
            resolutionArray[4] = new Size(1280, 800);
            resolutionArray[5] = new Size(1280,960);
            resolutionArray[6] = new Size(1280,1024);
            resolutionArray[7] = new Size(1360,768);
            resolutionArray[8] = new Size(1440,900);
            resolutionArray[9] = new Size(1600,900);
            resolutionArray[10] = new Size(1600,1200);
            resolutionArray[11] = new Size(1680,1050);
            resolutionArray[12] = new Size(1920,1080);
            resolutionArray[13] = new Size(1920,1200);
            resolutionArray[14] = new Size(2560,1440);
            resComboBox.SelectedItem = resComboBox.Items[0];

            string sAppData = System.Environment.GetEnvironmentVariable("APPDATA");

            System.IO.Directory.CreateDirectory(sAppData + "\\Images");

            imageUtils.initActiveDesktop();
        }

        private void OnSearchButtonClick(object sender, EventArgs e) 
        {
            if(searchEngine.IsAlive)
                return;
            
            listView1.LargeImageList.Images.Clear();
            listView1.Clear();
            onlineStorage.Clear();

            string searchTerm = searchComboBox.Text;
            searchEngine.StartInThread(searchTerm);
        }

        private void OnImageFoundDelegate(Image img, int id)
        {
            listView1.LargeImageList.Images.Add(img);
            ImageListViewItem item = new ImageListViewItem();
            item.Id = id;

            item.ImageIndex = listView1.LargeImageList.Images.Count - 1;
            item.Checked = true;
            listView1.Items.Add(item);
        }

        private void OnImageFound(object source, SearchEngineEventArgs arg)
        {
            if(this.InvokeRequired) 
            {
                int id = onlineStorage.Add(arg.ImageURL, arg.Title);
                Image img = imageUtils.retreiveImage(arg.ThumbnailURL);

                this.Invoke(new _ImageFoundDelegate(OnImageFoundDelegate), new object[] { img, id });
            }
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e) 
        {
            e.Graphics.FillRectangle(new SolidBrush(e.BackColor), e.Bounds);
            e.Graphics.DrawString(searchComboBox.Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds);

            if(e.Index == 0)
            {
                Rectangle commentBound = e.Bounds;
                
                Font newFont = new Font(e.Font,FontStyle.Bold);

                int iWidth = (int)e.Graphics.MeasureString("Suggestion", newFont).Width;
                commentBound.X = commentBound.Right - iWidth - 5;

                //commentBound = new Rectangle(iLeft, e.Bounds.y)

                Brush brush = null;
                if(e.State == DrawItemState.Focus) 
                {
                    brush = new SolidBrush(Color.White);
                }
                else
                {
                    brush = new SolidBrush(SystemColors.Highlight);
                }
                e.Graphics.DrawString("Suggestion", newFont, brush, commentBound);
            }
        }

        private void OnTimerShot(object sender, EventArgs e) 
        {
            changeTimer.Enabled = false;
            string sFileName = System.Environment.GetEnvironmentVariable("APPDATA") + "\\Images\\temp.jpg";
            string delFileName;

            if(File.Exists(sFileName)) 
            {
                delFileName = sFileName;
                sFileName = System.Environment.GetEnvironmentVariable("APPDATA") + "\\Images\\temp1.jpg";
            }
            else
            {
                delFileName = System.Environment.GetEnvironmentVariable("APPDATA") + "\\Images\\temp1.jpg";
            }
            Random rnd = new Random();

            int iIndex = rnd.Next(onlineStorage.Count - 1);
            imageUtils.saveImage(onlineStorage[iIndex].Key, sFileName);
            imageUtils.SetImageAsWellpaper(sFileName);

            File.Delete(delFileName);
            changeTimer.Enabled = true;
        }

        private void OnMouseClick(object sender, MouseEventArgs e) 
        {
            ListViewItem item = listView1.GetItemAt(e.X, e.Y);
            if(item == null)
                return;

            ImageListViewItem imgItem = (ImageListViewItem)item;

        }

        private void OnSetWellpaper(object sender, EventArgs e) 
        {
            if(listView1.SelectedItems.Count > 0) 
            {
                ImageListViewItem imgItem = (ImageListViewItem)listView1.SelectedItems[0];
                string imgPath = imageUtils.saveImage(onlineStorage[imgItem.Id].Key, onlineStorage[imgItem.Id].Value, "c:\\Users\\sckomoroh\\test_folder");
                imageUtils.SetImageAsWellpaper(imgPath);
            }
        }

        private void OnAddToGallery(object sender, EventArgs e) 
        {

        }

        private void OnImageCountChange(object sender, EventArgs e) 
        {
            searchEngine.Count = (int)imageCountUpDown.Value;
        }

        private void OnResolutionChanged(object sender, EventArgs e) 
        {
            Size size = resolutionArray[resComboBox.SelectedIndex];
            searchEngine.Width = size.Width;
            searchEngine.Height = size.Height;
        }

        private void OnResChanged(object sender, EventArgs e) {
            Size size = resolutionArray[resComboBox.SelectedIndex];
            searchEngine.Width = size.Width;
            searchEngine.Height = size.Height;
        }

        private void OnSwitchTimeChange(object sender, EventArgs e) 
        {
            changeTimer.Interval = (int)switchTimeUpDown.Value * 1000;
        }

        private void OnSelectAllOnline(object sender, EventArgs e) 
        {
            foreach(ListViewItem item in listView1.Items) 
            {
                item.Checked = true;
            }
        }

        private void OnUnselectAllOnline(object sender, EventArgs e) 
        {
            foreach(ListViewItem item in listView1.Items)
            {
                item.Checked = false;
            }
        }

        private void OnDownloadSelected(object sender, EventArgs e) 
        {
            DownloadForm form = new DownloadForm();
            form.DestinationPath = System.Environment.GetEnvironmentVariable("APPDATA") + "\\Images\\";
            foreach(ListViewItem listItem in listView1.Items) 
            {
                if(listItem.Checked) 
                {
                    ImageListViewItem imgItem = (ImageListViewItem)listItem;

                    string sUrl = onlineStorage[imgItem.Id].Key;
                    string sTitle = onlineStorage[imgItem.Id].Value;

                    form.Add(sUrl, sTitle);
                }
            }

            form.ShowDialog();
        }

        private void OnDownloadAll(object sender, EventArgs e) 
        {
            DownloadForm form = new DownloadForm();
            form.DestinationPath = System.Environment.GetEnvironmentVariable("APPDATA") + "\\Images\\";
            foreach (KeyValuePair<int, KeyValuePair<string, string>> item in onlineStorage)
            {
                string sUrl = item.Value.Key;
                string sTitle = item.Value.Value;

                form.Add(sUrl, sTitle);
            }

            form.ShowDialog();
        }

        private void OnStartSlideshowOnline(object sender, EventArgs e) {
            changeTimer.Enabled = true;
        }

        private void OnStopSlideshowOnline(object sender, EventArgs e) {
            changeTimer.Enabled = false;
        }

    }

    class ImageListViewItem : ListViewItem
    {
        private int id;

        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }
    }
}
