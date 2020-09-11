using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace XetTexTool {
	
	class BigEndianBinaryReader : BinaryReader {
		
        public BigEndianBinaryReader(Stream input) : base(input) {
            
        }

        public override short ReadInt16() {
            byte[] b = ReadBytes(2);
			b.Reverse();
            return (short) BitConverter.ToInt16(b, 0);
        }
		
        public override int ReadInt32() {
            byte[] b = ReadBytes(4);
			b.Reverse();
			return BitConverter.ToInt32(b, 0);
        }
		
        public override long ReadInt64() {
            byte[] b = ReadBytes(8);
			b.Reverse();
            return (long) BitConverter.ToInt64(b, 0);
        }
	}
	
	static class PSP {
		
		public static byte[] UnSwizzle(TexTexture texture) {
			int destinationOffset = 0;

			int height = texture.Height;
			// Incorperate the bpp into the width
			int width = (texture.Width * texture.BitsPerPixel) >> 3;

			byte[] destination = new byte[width * height];

			int rowblocks = (width / 16);

			int magicNumber = 8;

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int blockX = x / 16;
					int blockY = y / magicNumber;

					int blockIndex = blockX + ((blockY) * rowblocks);
					int blockAddress = blockIndex * 16 * magicNumber;

					destination[destinationOffset] = texture.Binary[blockAddress + (x - blockX * 16) + ((y - blockY * magicNumber) * 16)];
					destinationOffset++;
				}
			}

			return destination;
		}	
	}
	
	class TexHeaderMap {
		public string FileExtension { get; set; }
		public int FileVersion { get; set; }
		public short FileCount { get; set; }
		public short FileCountB { get; set; }
		public int Unk0c { get; set; }
		public int FileListOffset { get; set; }
		public int FileInfoOffset { get; set; }
		public int FileNamesOffset { get; set; }
	}
	
	class TexTexture {
		public int Width { get; set; }
		public int Height { get; set; }
		public short Largest { get; set; }
		public short Unk06 { get; set; }
		public int DataOffset { get; set; }
		public int BitsPerPixel { get; set; }
		public int Number { get; set; }
		public int Unk08 { get; set; }
		public int InfoOffset { get; set; }
		public byte[] Binary { get; set; }
		public byte[] Unswizzled { get; set; }
		public byte[] Palette { get; set; }
		public string Name { get; set; }
	}
	
	static class XetTexTool {

		static int cint(byte[] x) {
			if (x.Length == 2) {
				return BitConverter.ToInt16(x, 0);
			} else if (x.Length == 4) {
				return BitConverter.ToInt32(x, 0);
			} else {
				// throw exception;
				return -1;
			}
		}

		// TODO refactor this.
		static byte[] to4(byte[] b) {
			byte[] a = { 0, 0, 0, 0 };
			for (int i = 0; i < b.Length; i++) {
				a[(i + 4 - b.Length)] = b[i];
			}
			return a;
		}

		static byte[] cbyte(int x) {
			byte[] b = BitConverter.GetBytes(x);
			return to4(b);
		}

		static byte[] readAt(this BinaryReader reader, int a, int b) {
			reader.BaseStream.Seek(a, SeekOrigin.Begin);
			return reader.ReadBytes(b);
		}

		static List<TexTexture> parseFile(string fn) {
			List<TexTexture> images = new List<TexTexture>();
			using(BigEndianBinaryReader f = new BigEndianBinaryReader(File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
				string output = "";
				
				TexHeaderMap TexFile = new TexHeaderMap {
					FileExtension = System.Text.Encoding.UTF8.GetString(f.ReadBytes(4), 0, 4),
					FileVersion = f.ReadInt32(),
					FileCount = f.ReadInt16(),
					FileCountB = f.ReadInt16(),
					Unk0c = f.ReadInt32(),
					FileListOffset = f.ReadInt32(),
					FileInfoOffset = f.ReadInt32(),
					FileNamesOffset = f.ReadInt32()
				};
				
				if(TexFile.FileExtension != "XET.") return images;
				
				output += "[items_count]\n";
				output += "count=" + TexFile.FileCount.ToString();

				for (int i = 0; i < TexFile.FileCount; i++) {
					TexTexture texture = new TexTexture();
					
					f.BaseStream.Seek(TexFile.FileListOffset + (4 * i) + (12 * i), SeekOrigin.Begin);
					texture.BitsPerPixel = f.ReadInt32() == 5 ?  8 : 4;
					Console.WriteLine("BPP: " + texture.BitsPerPixel);
					
					texture.Number = f.ReadInt32();
					
					texture.Unk08 = f.ReadInt32();
					
					texture.InfoOffset = f.ReadInt32();
					Console.WriteLine("offset4 (img data?): " + texture.InfoOffset);
					
					f.BaseStream.Seek(texture.InfoOffset, SeekOrigin.Begin);
					texture.Width = (int) f.ReadInt16();
					Console.WriteLine("imgWidth: " + texture.Width);

					texture.Height = (int) f.ReadInt16();
					Console.WriteLine("imgHeight: " + texture.Height);

					texture.Largest = f.ReadInt16();

					texture.Unk06 = f.ReadInt16();

					texture.DataOffset = f.ReadInt32();
					Console.WriteLine("dataStart: " + texture.DataOffset);
					
					f.BaseStream.Seek(TexFile.FileInfoOffset + 4 + (8 * i), SeekOrigin.Begin);
					int paletteOffset = f.ReadInt32();
					Console.WriteLine("paletteOffset: " + paletteOffset);					

					byte[] nb = f.readAt(TexFile.FileNamesOffset, (int) f.BaseStream.Length - TexFile.FileNamesOffset);
					string name = System.Text.Encoding.UTF8.GetString(nb, 0, nb.Length).Split('\x00') [i];
					texture.Name = name;

					texture.Binary = f.readAt(texture.DataOffset, paletteOffset - texture.DataOffset);
					texture.Palette = f.readAt(paletteOffset, texture.InfoOffset);

					texture.Unswizzled = PSP.UnSwizzle(texture);

					images.Add(texture);

					Console.WriteLine("name: " + name);
					Console.WriteLine();
					output += "\n[item_" + i.ToString() + "]";
					output += "\nname=" + name;
					output += "\nplatform=PSP";
					output += "\noffset=" + texture.DataOffset.ToString();
					output += "\nwidth=" + texture.Width.ToString();
					output += "\nheight=" + texture.Height.ToString();
					output += "\nBPP=" + texture.BitsPerPixel.ToString();
					output += "\nmipmaps=-1";
					output += "\npalette_offset=" + paletteOffset.ToString();
					output += "\nswizzling=Enabled";
				}
			}

			return images;
		}

		static Bitmap GetBitmapFromPalette(TexTexture texture) {
			Bitmap bmp = new Bitmap(texture.Width, texture.Height);
			int row = 0;
			int bitsPerPixelMultiplier = 8 / texture.BitsPerPixel;

			for (int x = 0; x < texture.Unswizzled.Length * bitsPerPixelMultiplier; x++) {
				if (x >=  texture.Width + (texture.Width * row)) row++;
				int col = (x - (texture.Width * row));
				int dataIndex = x;
				int paletteIndex = 0;
				if (texture.BitsPerPixel == 4) {
					dataIndex = (x % 2 == 0 ? x : x - 1) / 2;
					if (dataIndex >= texture.Unswizzled.Length) break;
					if (x % 2 == 0) {
						paletteIndex = ((int) (texture.Unswizzled[dataIndex] & 0x0F)) * 4;
					} else {
						paletteIndex = ((int) (texture.Unswizzled[dataIndex] >> 4)) * 4;
					}
				} else {
					paletteIndex = ((int) texture.Unswizzled[dataIndex]) * 4;
				}
				Color c = Color.FromArgb(texture.Palette[paletteIndex + 3], texture.Palette[paletteIndex], texture.Palette[paletteIndex + 1], texture.Palette[paletteIndex + 2]);
				bmp.SetPixel(col, row, c);
			}
			return bmp;
		}

		public static void DrawPicture(MouseEventArgs e = null) {
			int sv = p.VerticalScroll.Value;
			p.VerticalScroll.Value = sv >= 120 ? sv - 120 : sv;
			if (e != null) zoomLevel *= e.Delta > 0 ? 1.10f : 0.90f;
			zoomLevel = zoomLevel < .50f? .50f : zoomLevel;
			zoomLevel = zoomLevel > 50f ? 50f : zoomLevel;

			int newW = (int) ((float) obmp.Width * zoomLevel);
			int newH = (int) ((float) obmp.Height * zoomLevel);
			bmp = new Bitmap(newW, newH);

			using(Graphics g = Graphics.FromImage(bmp)) {
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.DrawImage(obmp, new Rectangle(Point.Empty, bmp.Size));
				g.DrawRectangle(new Pen(Brushes.Black, 1), new Rectangle(0, 0, bmp.Width - 1, bmp.Height - 1));
			}
			pb.Image = bmp;
			pb.Size = bmp.Size;
		}

		public static float zoomLevel = 1f;
		public static PictureBox pb = new PictureBox();
		public static Panel p;
		public static Bitmap bmp;
		public static Bitmap obmp;
		public static ListBox imageList;
		public static List<TexTexture> images;
		public static Label info;
		
		static void loadImages(){
			
			imageList.Items.Clear();
			for(int i = 0; i < images.Count; i++) {
				imageList.Items.Add(images[i].Name);
			}
			
			if(images.Count > 0){
				imageList.SelectedIndex = 0;
			}
			displayImage();
		}
		
		static void displayImage(){
			TexTexture t = images[imageList.SelectedIndex];
			bmp = GetBitmapFromPalette(t);
			obmp = bmp;
			DrawPicture();
			info.Text =  t.Width + " x " + t.Height + " (" + t.BitsPerPixel + "bpp)";
		}

		static void ShowForm(string[] args) {
			Form f = new Form();
			f.Text = "XetTex Viewer";
			f.Width = 350;
			f.Height = 150;
			f.MinimumSize = new Size(f.Width, f.Height);

			p = new Panel();
			p.Width = 200;
			p.Height = 50;
			p.BackColor = Color.Gray;
			p.AutoScroll = true;
			p.Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right);
			f.Controls.Add(p);
			
			Panel infoPanel = new Panel();
			infoPanel.Location = new Point(0,55);
			infoPanel.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right);
			f.Controls.Add(infoPanel);
			
			info = new Label();
			infoPanel.Controls.Add(info);

			imageList = new ListBox();
			imageList.Location = new Point(205);
			imageList.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right);
			
			f.Controls.Add(imageList);
			imageList.SelectedIndexChanged += (s, e) => {
				displayImage();
			};
			obmp = bmp;

			p.Controls.Add(pb);
			p.MouseWheel += (s, e) => {
				DrawPicture(e);
			};
			
			f.AllowDrop = true;
		
			f.DragEnter += (s, e) => {
				if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
			};
			
			f.DragDrop += (s, e) => {
				string[] fileList = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
				if(fileList.Length > 0){
					List<TexTexture> tempImages = parseFile(fileList[0]);
					if(tempImages.Count == 0){
						MessageBox.Show("No images found!\nIncorrect file?");
					}else{
						images = tempImages;
						loadImages();
					}
				}
			};
			
			if (args.Length > 0 && File.Exists(args[0])) {
				images = parseFile(args[0]);
				loadImages();
			}

			f.ShowDialog();
		}

		[STAThread]
		static void Main(string[] args) {
			ShowForm(args);
		}
	}
}
