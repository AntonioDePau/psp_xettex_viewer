using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace XetTexTool {
	
	static class PSP {
		
		public static byte[] UnSwizzle(byte[] source, int offset, int width, int height, int bpp) {
			int destinationOffset = 0;

			// Incorperate the bpp into the width
			width = (width * bpp) >> 3;

			byte[] destination = new byte[width * height];

			int rowblocks = (width / 16);

			int magicNumber = 8;

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int blockX = x / 16;
					int blockY = y / magicNumber;

					int blockIndex = blockX + ((blockY) * rowblocks);
					int blockAddress = blockIndex * 16 * magicNumber;

					destination[destinationOffset] = source[offset + blockAddress + (x - blockX * 16) + ((y - blockY * magicNumber) * 16)];
					destinationOffset++;
				}
			}

			return destination;
		}	
	}
	
	class TexHeaderMap {
		public int FileExtension { get; set; }
		public int FileVersion { get; set; }
		public short FileCount { get; set; }
		public short FileCountB { get; set; }
		public int Unk0c { get; set; }
		public int FileListOffset { get; set; }
		public int FileInfoOffset { get; set; }
		public int FileNamesOffset { get; set; }
	}
	
	class TexTexture {
		public short Width { get; set; }
		public short Height { get; set; }
		public short Largest { get; set; }
		public short Unk06 { get; set; }
		public int BitsPerPixel { get; set; }
		public int Number { get; set; }
		public int Unk08 { get; set; }
		public byte[] Binary { get; set; }
		public byte[] Unswizzle { get; set; }
		public byte[] Palette { get; set; }
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

		static List<Dictionary<string, dynamic>> images;

		static void parse(string fn) {
			images = new List<Dictionary<string, dynamic>>();
			using(BinaryReader f = new BinaryReader(File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
				int p = 16;
				string output = "";
				
				//Currently breaking everything since the values are read as BE rather than LE
				TexHeaderMap TexFile = new TexHeaderMap {
					FileExtension = f.ReadInt32(),
					FileVersion = f.ReadInt32(),
					FileCount = f.ReadInt16(),
					FileCountB = f.ReadInt16(),
					Unk0c = f.ReadInt32(),
					FileListOffset = f.ReadInt32(),
					FileInfoOffset = f.ReadInt32(),
					FileNamesOffset = f.ReadInt32()
				};

				output += "[items_count]\n";
				output += "count=" + TexFile.FileCount.ToString();

				for (int i = 0; i < TexFile.FileCount; i++) {
					TexTexture texture = new TexTexture();
					
					f.BaseStream.Seek(TexFile.FileListOffset + (4 * i) + (12 * i), SeekOrigin.Begin);
					
					texture.BitsPerPixel = f.ReadInt32() == 5 ?  8 : 4;
					Console.WriteLine("BPP: " + texture.BitsPerPixel);
					//p = TexFile.FileListOffset + 12 + (4 * i) + (12 * i);
					//f.BaseStream.Seek(10, SeekOrigin.Current);
					
					texture.Number = f.ReadInt32();
					texture.Unk08 = f.ReadInt32();

					//int offset4 = cint(f.readAt(p, 4));
					
					int offset4 = f.ReadInt32();
					Console.WriteLine("offset4 (img data?): " + offset4);
					p = offset4;

					texture.Width = f.ReadInt16();
					Console.WriteLine("imgWidth: " + texture.Width);
					//p += 2;

					texture.Height = f.ReadInt16();
					Console.WriteLine("imgHeight: " + texture.Height);
					//p += 2;

					texture.Largest = f.ReadInt16();
					//int UnknownInt1 = cint(f.readAt(p, 2));
					//Console.WriteLine("UnknownInt1: " + UnknownInt1);

					//int UnknownInt2 = cint(f.readAt(p + 2, 2));
					//Console.WriteLine("UnknownInt2: " + UnknownInt2);
					//p += 4;
					texture.Unk06 = f.ReadInt16();

					int dataStart = f.ReadInt32();
					Console.WriteLine("dataStart: " + dataStart);
					f.BaseStream.Seek(TexFile.FileInfoOffset + 4 + (8 * i), SeekOrigin.Begin);

					int paletteOffset = f.ReadInt32();
					Console.WriteLine("paletteOffset: " + paletteOffset);
					p = 24;

					int nameOffset = cint(f.readAt(p, 4));
					Console.WriteLine("nameOffset: " + nameOffset);

					byte[] nb = f.readAt(nameOffset, (int) f.BaseStream.Length - nameOffset);
					string name = System.Text.Encoding.UTF8.GetString(nb, 0, nb.Length).Split('\x00') [i];

					byte[] data = f.readAt(dataStart, paletteOffset - dataStart);
					byte[] palette = f.readAt(paletteOffset, offset4);

					byte[] unswizzled = PSP.UnSwizzle(data, 0, (int) texture.Width, (int) texture.Height, (int) texture.BitsPerPixel);

					images.Add(
						new Dictionary<string, dynamic> { { "name", name },
							{ "width", (int) texture.Width },
							{ "height", (int) texture.Height },
							{ "bpp", (int) texture.BitsPerPixel },
							{ "binary", data },
							{ "unswizzled", unswizzled },
							{ "palette", palette }
						}
					);

					Console.WriteLine("name: " + name);
					Console.WriteLine();
					output += "\n[item_" + i.ToString() + "]";
					output += "\nname=" + name;
					output += "\nplatform=PSP";
					output += "\noffset=" + dataStart.ToString();
					output += "\nwidth=" + texture.Width.ToString();
					output += "\nheight=" + texture.Height.ToString();
					output += "\nBPP=" + texture.BitsPerPixel.ToString();
					output += "\nmipmaps=-1";
					output += "\npalette_offset=" + paletteOffset.ToString();
					output += "\nswizzling=Enabled";
				}
			}

			ShowForm(images);
		}

		static Bitmap GetBitmapFromPalette(Dictionary<string, dynamic> image) {
			int width = image["width"];
			int height = image["height"];
			byte[] data = image["unswizzled"];
			byte[] palette = image["palette"];
			int bitsPerPixel = image["bpp"];

			Bitmap bmp = new Bitmap(width, height);
			int row = 0;
			int bitsPerPixelMultiplier = 8 / bitsPerPixel;

			for (int x = 0; x < data.Length * bitsPerPixelMultiplier; x++) {
				if (x >= width + (width * row)) row++;
				int col = (x - (width * row));
				int dataIndex = x;
				int paletteIndex = 0;
				if (bitsPerPixel == 4) {
					dataIndex = (x % 2 == 0 ? x : x - 1) / 2;
					if (dataIndex >= data.Length) break;
					if (x % 2 == 0) {
						paletteIndex = ((int) (data[dataIndex] & 0x0F)) * 4;
					} else {
						paletteIndex = ((int) (data[dataIndex] >> 4)) * 4;
					}
				} else {
					paletteIndex = ((int) data[dataIndex]) * 4;
				}
				Color c = Color.FromArgb(palette[paletteIndex + 3], palette[paletteIndex], palette[paletteIndex + 1], palette[paletteIndex + 2]);
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

		static void ShowForm(List<Dictionary<string, dynamic>> images) {
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

			ListBox lb = new ListBox();
			lb.Location = new Point(205);
			lb.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right);
			for(int i = 0; i < images.Count; i++) {
				lb.Items.Add(images[i]["name"]);
			}
			f.Controls.Add(lb);
			lb.SelectedIndexChanged += (s, e) => {
				bmp = GetBitmapFromPalette(images[lb.SelectedIndex]);
				obmp = bmp;
				DrawPicture();
			};
			
			if(images.Count > 0){
				lb.SelectedIndex = 0;
			}
			obmp = bmp;

			p.Controls.Add(pb);
			p.MouseWheel += (s, e) => {
				DrawPicture(e);
			};

			f.ShowDialog();
		}

		static void Main(string[] args) {
			if (args.Length > 0 && File.Exists(args[0])) {
				parse(args[0]);
			} else {
				Console.WriteLine("Please input a file!");
				Console.ReadLine();
			}
		}
	}
}
