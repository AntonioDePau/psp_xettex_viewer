using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ConsoleProject.DTO;
using ConsoleProject.Services;
using ConsoleProject.Utils;

namespace ConsoleProject.View {

    public class Frontend {

        private float zoomLevel = 1f;
        private PictureBox pictureBox = new PictureBox();
        private Panel panel;
        private Bitmap bmp;
        private Bitmap obmp;
        private ListBox imageListBox;
        private List<Texture> images;
        private Label labelImageInfo = new Label();
        private Label labelZoomInfo = new Label();

        public void ShowForm(string[] arguments = null) {
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
                this.ParseNewFile(fileList);
            };

            this.ParseNewFile(arguments);
            form.ShowDialog();
        }

        private Button InitRepackButton() {
            Button repackButton = new Button {
                Text = "Repack folder into XET",
                Location = new Point(0, 45)
            };
            repackButton.Click += (s, e) => {
                GetPngFiles();
            };
            return repackButton;
        }

        private Form InitForm() {
            return new Form {
                Text = "XetTex Viewer",
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
        
        private void ParseNewFile(string[] fileList) {
            if (fileList.Length > 0) { // is this IF required?
                try {
                    images = new TextureConverter().ParseFile(fileList[0]); // TODO only reads first file
                    this.LoadImages();
                } catch (InvalidDataException ex) {
                    Console.WriteLine(ex);
                    MessageBox.Show("No images found!\nIncorrect file?");
                }
            }
        }

        private void DrawPicture(MouseEventArgs mouseEvent = null) {
            if (obmp == null) return;
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
            labelImageInfo.Text = t.Width + " x " + t.Height + " (" + t.BitsPerPixel + "bpp)";
        }

        private void GetPngFiles() {
            List<Texture> imgs = new List<Texture>();

            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                    string[] files = Directory.GetFiles(fbd.SelectedPath);

                    string extension = "Png";
                    for (int f = 0; f < files.Length; f++) {
                        string file = files[f];
                        if (Path.GetExtension(file).ToLower() != "." + extension.ToLower()) continue;

                        Image image1 = Image.FromFile(file);

                        Bitmap bmp2 = new Bitmap(image1);

                        List<Color> palette = new List<Color>();

                        Console.WriteLine(bmp2.PixelFormat);
                        for (int y = 0; y < bmp2.Height; y++) {
                            for (int x = 0; x < bmp2.Height; x++) {
                                Color c = bmp2.GetPixel(x, y);
                                if (palette.IndexOf(c) == -1) {
                                    if (palette.Count < 256) palette.Add(c);
                                }
                            }
                        }

                        while (palette.Count < 16) palette.Insert(0, Color.FromArgb(0, 0, 0, 0));

                        if (palette.Count > 16) while (palette.Count < 256) palette.Insert(0, Color.FromArgb(0, 0, 0, 0));

                        palette.Sort((x, y) => x.A.CompareTo(y.A));

                        byte[] paletteBinary = new byte[(palette.Count) * 4];

                        for (int i = 0; i < palette.Count; i++) {
                            paletteBinary[i * 4] = palette[i].R;
                            paletteBinary[i * 4 + 1] = palette[i].G;
                            paletteBinary[i * 4 + 2] = palette[i].B;
                            paletteBinary[i * 4 + 3] = palette[i].A;
                        }
                        Console.WriteLine("Total color count: " + palette.Count);

                        Texture texture = new Texture(bmp2.Width, bmp2.Height);
                        texture.BitsPerPixel = palette.Count <= 16 ? 4 : 8;
                        texture.Colors = palette;
                        texture.Palette = paletteBinary;
                        texture.Bitmap = bmp2;
                        texture.Name = Path.GetFileNameWithoutExtension(file);

                        int BitMultiplier = 8 / texture.BitsPerPixel;
                        int DataSize = (texture.Width * texture.Height) / BitMultiplier;

                        byte[] Unswizzled = new byte[DataSize];
                        int dataIndex = 0;
                        for (int y = 0; y < bmp2.Height; y++) {
                            for (int x = 0; x < bmp2.Width; x++) {
                                Color c = bmp2.GetPixel(x, y);
                                int colorIndex = texture.Colors.IndexOf(c);
                                if (colorIndex == -1) colorIndex = ColorCompare.GetClosest(texture.Colors, c);
                                if (texture.BitsPerPixel == 4) {
                                    if (dataIndex % 2 == 0) {
                                        Unswizzled[dataIndex / 2] = (byte)colorIndex;
                                    } else {
                                        Unswizzled[(dataIndex - 1) / 2] |= (byte)((byte)colorIndex << 4 & 0xf0);
                                    }
                                } else {
                                    Unswizzled[dataIndex] = (byte)colorIndex;
                                }
                                dataIndex++;
                            }
                        }

                        texture.Unswizzled = Unswizzled;
                        texture.Binary = SwizzleService.Swizzle(texture);
                        imgs.Add(texture);
                    }
                }
            }
            TextureConverter.SaveTexFile(imgs);
        }
    }
}
