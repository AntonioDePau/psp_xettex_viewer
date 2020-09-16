using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ConsoleProject.DTO;
using ConsoleProject.Services;

namespace ConsoleProject.View {

    public class Frontend {

        private static float zoomLevel = 1f;
        private static PictureBox pb = new PictureBox();
        private static Panel panel;
        private static Bitmap bmp;
        private static Bitmap obmp;
        private static ListBox imageList;
        private static List<Texture> images;
        private static Label info;

        public void ShowForm() {
            Form form = this.InitForm();
            panel = this.InitMainPanel(); // image display panel
            form.Controls.Add(panel);

            Panel infoPanel = new Panel {
                Location = new Point(0, 55),
                Anchor = (AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right)
            };
            form.Controls.Add(infoPanel);

            info = new Label();
            infoPanel.Controls.Add(info);

            Button extractButton = new Button();
            extractButton.Text = "Extract";
            extractButton.Location = new Point(0, 50);
            extractButton.Click += (s, e) => {
                ExportService.WriteTextures(images);
            };
            infoPanel.Controls.Add(extractButton);

            imageList = new ListBox();
            imageList.Location = new Point(205);
            imageList.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right);

            form.Controls.Add(imageList);
            imageList.SelectedIndexChanged += (s, e) => {
                DisplayImage();
            };

            panel.Controls.Add(pb);
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
                string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (fileList.Length > 0) { // is this IF required?
                    try {
                        images = new TextureConverter().ParseFile(fileList[0]); // TODO only reads first file
                        this.LoadImages();
                    } catch (InvalidDataException ex) {
                        Console.WriteLine(ex);
                        MessageBox.Show("No images found!\nIncorrect file?");
                    }
                }
            };

            form.ShowDialog();
        }

        private Form InitForm() {
            return new Form {
                Text = "XetTex Viewer",
                Width = 800,
                Height = 600,
                MinimumSize = new Size(800, 600)
            };
        }

        private Panel InitMainPanel() {
            return new Panel {
                Width = 200,
                Height = 100,
                BackColor = Color.White,
                AutoScroll = true,
                Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right)
            };
        }

        private void DrawPicture(MouseEventArgs mouseEvent = null) {
            int sv = panel.VerticalScroll.Value;
            panel.VerticalScroll.Value = sv >= 120 ? sv - 120 : sv;
            if (mouseEvent != null) zoomLevel *= mouseEvent.Delta > 0 ? 1.10f : 0.90f;
            zoomLevel = zoomLevel < .50f ? .50f : zoomLevel;
            zoomLevel = zoomLevel > 50f ? 50f : zoomLevel;

            int newW = (int)(obmp.Width * zoomLevel);
            int newH = (int)(obmp.Height * zoomLevel);
            bmp = new Bitmap(newW, newH);

            using (Graphics g = Graphics.FromImage(bmp)) {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(obmp, new Rectangle(Point.Empty, bmp.Size));
                g.DrawRectangle(new Pen(Brushes.Black, 1), new Rectangle(0, 0, bmp.Width - 1, bmp.Height - 1));
            }
            pb.Image = bmp;
            pb.Size = bmp.Size;
        }

        private void LoadImages() {
            imageList.Items.Clear();
            for (int i = 0; i < images.Count; i++) {
                imageList.Items.Add(images[i].Name);
            }

            if (images.Count > 0) {
                imageList.SelectedIndex = 0;
            }
            DisplayImage();
        }

        private void DisplayImage() {
            Texture t = images[imageList.SelectedIndex];
            bmp = t.Bitmap;
            obmp = bmp;
            DrawPicture();
            info.Text = t.Width + " x " + t.Height + " (" + t.BitsPerPixel + "bpp)";
        }

    }
}
