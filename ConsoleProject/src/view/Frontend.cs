using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ConsoleProject.DTO;
using ConsoleProject.Services;
using log4net;

namespace ConsoleProject.View {

    public class Frontend {

        private static readonly ILog LOG = LogManager.GetLogger(typeof(Frontend));
        private static readonly string APPNAME = "XetTex Viewer";
        private float zoomLevel = 1f;
        private Form form;
        private PictureBox pictureBox = new PictureBox();
        private Panel panel;
        private Bitmap bmp;
        private Bitmap obmp;
        private ListBox imageListBox;
        private List<Texture> images;
        private Label labelImageInfo = new Label();
        private Label labelZoomInfo = new Label();

        public void ShowForm(string[] fileList = null) {
            form = this.InitForm();

            panel = this.InitMainPanel();
            form.Controls.Add(panel);

            form.Controls.Add(this.InitInfoPanel());

            imageListBox = this.InitListBox();
            form.Controls.Add(imageListBox);

            panel.Controls.Add(pictureBox);
            panel.MouseWheel += (s, e) => {
                DrawPicture(e);
            };

            form.AllowDrop = true;

            form.DragEnter += (s, e) => {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                    e.Effect = DragDropEffects.Copy;
                }
            };

            form.DragDrop += (s, e) => {
                string[] filesDropped = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                TryTextureFile(filesDropped);
            };

            if (fileList.Length > 0) {
                TryTextureFile(fileList);
            }

            form.ShowDialog();
        }

        private void TryTextureFile(string[] fileList) {
            try {
                //TODO: handle multiple files
                images = new TextureConverter().ParseFile(fileList[0]);
                LoadImages(fileList[0]);
            } catch {
                MessageBox.Show("No images found!\nIncorrect file?");
            }
        }

        private Button InitRepackButton() {
            Button repackButton = new Button {
                Text = "Repack folder into XET",
                Location = new Point(0, 45)
            };
            repackButton.Click += (s, e) => {
                RepackPngFiles();
            };
            return repackButton;
        }

        private Form InitForm() {
            return new Form {
                Text = APPNAME,
                Width = 400,
                Height = 200,
                MinimumSize = new Size(400, 200)
            };
        }

        private Panel InitMainPanel() {
            return new Panel {
                Width = 200,
                Height = 50,
                BackColor = Color.White,
                AutoScroll = true,
                Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right)
            };
        }

        private Button InitExtractButton() {
            Button extractButton = new Button {
                Text = "Extract",
                Location = new Point(0, 20)
            };
            extractButton.Click += (s, e) => {
                ExportService.WriteTextures(images);
            };
            return extractButton;
        }

        private Panel InitInfoPanel() {
            Panel infoPanel = new Panel {
                Location = new Point(0, 55),
                Anchor = (AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right)
            };

            infoPanel.Controls.Add(labelImageInfo);
            //Not sure if the following 3 lines should be here or somewhere else?
            labelZoomInfo.Location = new Point(infoPanel.Width - 60, 0);
            labelZoomInfo.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            labelZoomInfo.Text = "Zoom: " + zoomLevel;
            infoPanel.Controls.Add(labelZoomInfo);
            infoPanel.Controls.Add(this.InitExtractButton());
            infoPanel.Controls.Add(this.InitRepackButton());
            return infoPanel;
        }

        private ListBox InitListBox() {
            ListBox listBox = new ListBox {
                Location = new Point(205),
                Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right)
            };

            listBox.SelectedIndexChanged += (s, e) => {
                DisplayImage();
            };
            return listBox;
        }

        private void DrawPicture(MouseEventArgs mouseEvent = null) {
            if (obmp == null) {
                return;
            }
            int sv = panel.VerticalScroll.Value;
            panel.VerticalScroll.Value = sv >= 120 ? sv - 120 : sv;
            if (mouseEvent != null) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    zoomLevel += mouseEvent.Delta > 0 ? 0.01f : -0.01f;
                } else {
                    zoomLevel *= mouseEvent.Delta > 0 ? 1.10f : 0.90f;
                }
            }
            zoomLevel = zoomLevel < .50f ? .50f : zoomLevel;
            zoomLevel = zoomLevel > 12f ? 12f : zoomLevel;

            labelZoomInfo.Text = "Zoom: " + zoomLevel.ToString("n2");

            int newW = (int)(obmp.Width * zoomLevel);
            int newH = (int)(obmp.Height * zoomLevel);
            bmp = new Bitmap(newW, newH);

            using (Graphics g = Graphics.FromImage(bmp)) {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(obmp, new Rectangle(Point.Empty, bmp.Size));
                g.DrawRectangle(new Pen(Brushes.Black, 1), new Rectangle(0, 0, bmp.Width - 1, bmp.Height - 1));
            }
            pictureBox.Image = bmp;
            pictureBox.Size = bmp.Size;
        }

        private void LoadImages(string filename) {
            imageListBox.Items.Clear();
            for (int i = 0; i < images.Count; i++) {
                imageListBox.Items.Add(images[i].Name);
            }

            if (images.Count > 0) {
                imageListBox.SelectedIndex = 0;
            }
            DisplayImage();
            form.Text = APPNAME + " - " + Path.GetFileName(filename);
        }

        private void DisplayImage() {
            Texture t = images[imageListBox.SelectedIndex];
            bmp = t.Bitmap;
            obmp = bmp;
            DrawPicture();
            labelImageInfo.Text = t.Width + " x " + t.Height + " (" + t.BitsPerPixel + "bpp)";
        }

        private void RepackPngFiles() {
            try {
                List<Texture> imgs = new List<Texture>();
                string[] files = GetFilesFromFolderBrowser();
                foreach (string file in files) {
                    imgs.Add(TextureConverter.RepackTexture(file));
                }

                TextureConverter.SaveTexFile(imgs);
                // saved messagebox
            } catch (Exception ex) {
                LOG.Error("Failed to save texture. Reason:", ex);
                // error messagebox
            }
        }

        private string[] GetFilesFromFolderBrowser() {
            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                    return Directory.GetFiles(fbd.SelectedPath);
                }

                LOG.Warn("No files are selected.");
                return new string[0]; // return empty string array. TODO might replace with an exception.
            }
        }

    }
}
