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

        private float zoomLevel = 1f;
        private PictureBox pictureBox = new PictureBox();
        private Panel panel;
        private Bitmap bmp;
        private Bitmap obmp;
        private ListBox imageListBox;
        private List<Texture> images;
        private Label labelInfo = new Label();

        public void ShowForm() {
            Form form = this.InitForm();
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

        private Button InitExtractButton() {
            Button extractButton = new Button {
                Text = "Extract",
                Location = new Point(0, 50)
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

            infoPanel.Controls.Add(labelInfo);
            infoPanel.Controls.Add(this.InitExtractButton());
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
            pictureBox.Image = bmp;
            pictureBox.Size = bmp.Size;
        }

        private void LoadImages() {
            imageListBox.Items.Clear();
            for (int i = 0; i < images.Count; i++) {
                imageListBox.Items.Add(images[i].Name);
            }

            if (images.Count > 0) {
                imageListBox.SelectedIndex = 0;
            }
            DisplayImage();
        }

        private void DisplayImage() {
            Texture t = images[imageListBox.SelectedIndex];
            bmp = t.Bitmap;
            obmp = bmp;
            DrawPicture();
            labelInfo.Text = t.Width + " x " + t.Height + " (" + t.BitsPerPixel + "bpp)";
        }

    }
}
