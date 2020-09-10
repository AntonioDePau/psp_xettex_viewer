using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

static class CTEinfoParser{
	
	static int cint(byte[] x){
		int i = 0;
		//Console.WriteLine(x.Length);
		if(x.Length==2) i = BitConverter.ToInt16(x, 0);
		if(x.Length==4) i = BitConverter.ToInt32(x, 0);
		return i;
	}
	
	static byte[] to4(byte[] b){
		byte[] a = {0,0,0,0};
		for(int i = 0;i<b.Length;i++){
			a[(i+4-b.Length)] = b[i];
		}
		return a;
	}
	
	static byte[] cbyte(int x){
		byte[] b = BitConverter.GetBytes(x);
		//Array.Reverse(b); //byte to littleEndian
		return to4(b);
	}
	
	static byte[] readAt(this BinaryReader reader, int a, int b){
		reader.BaseStream.Seek(a, SeekOrigin.Begin);
		return reader.ReadBytes(b);
	}
	
	static List<Dictionary<string, dynamic>> images;
	
	static void parse(string fn){
		images = new List<Dictionary<string, dynamic>>();
		using(BinaryReader f = new BinaryReader(File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))){
			int p = 16;
			string output = "";
			
			int files = cint(f.readAt(8,2));
			output+="[items_count]\n";
			output+="count="+files.ToString();
			
			for(int i=0;i<files;i++){
				p=16;
				int offset1 = cint(f.readAt(p,4));
				Console.WriteLine("offset1 (imgs infos): " + offset1);
				p+=4;
				
				int offset2 = cint(f.readAt(p,4));
				Console.WriteLine("offset2 (duplicate Data offset (bpp, paletteOffset)): " + offset2);
				p+=4;
				
				int offset3 = cint(f.readAt(p,4));
				Console.WriteLine("offset3 (nameOffset): " + offset3);
				p=offset1+(4*i)+(12*i);
				
				int bpp = cint(f.readAt(p,2))==5?8:4;
				Console.WriteLine("BPP: " + bpp);
				p=offset1+12+(4*i)+(12*i);
				
				int offset4 = cint(f.readAt(p,4));
				Console.WriteLine("offset4 (img data?): " + offset4);
				p=offset4;
				
				int imgWidth = cint(f.readAt(p,2));
				Console.WriteLine("imgWidth: " + imgWidth);
				//Console.WriteLine(imgWidth);
				p+=2;
				
				int imgHeight = cint(f.readAt(p,2));
				Console.WriteLine("imgHeight: " + imgHeight);
				p+=2;
				
				int UnknownInt1 = cint(f.readAt(p,2));
				Console.WriteLine("UnknownInt1: " + UnknownInt1);
				
				int UnknownInt2 = cint(f.readAt(p+2,2));
				Console.WriteLine("UnknownInt2: " + UnknownInt2);
				p+=4;
				
				int dataStart = cint(f.readAt(p,4));
				Console.WriteLine("dataStart: " + dataStart);
				p=offset2+4+(8*i);
				
				int paletteOffset = cint(f.readAt(p,4));
				Console.WriteLine("paletteOffset: " + paletteOffset);
				p=24;
				
				int nameOffset = cint(f.readAt(p,4));
				Console.WriteLine("nameOffset: " + nameOffset);
				
				byte[] nb = f.readAt(nameOffset,(int)f.BaseStream.Length-nameOffset);
				string name = System.Text.Encoding.UTF8.GetString(nb, 0, nb.Length).Split('\x00')[i];
				
				byte[] data = f.readAt(dataStart, paletteOffset-dataStart);
				byte[] palette = f.readAt(paletteOffset, offset4);
				
				
				byte[] unswizzled = UnSwizzle(data, 0, imgWidth, imgHeight, bpp);
				
				images.Add(
					new Dictionary<string, dynamic>{
						{"name", name},
						{"width", imgWidth},
						{"height", imgHeight},
						{"bpp", bpp},
						{"binary", data},
						{"unswizzled", unswizzled},
						{"palette", palette}
					}
				);
				
				Console.WriteLine("name: " + name);
				Console.WriteLine();
				output+="\n[item_"+i.ToString()+"]";
				output+="\nname="+name;
				output+="\nplatform=PSP";
				output+="\noffset="+dataStart.ToString();
				output+="\nwidth="+imgWidth.ToString();
				output+="\nheight="+imgHeight.ToString();
				output+="\nBPP="+bpp.ToString();
				output+="\nmipmaps=-1";
				output+="\npalette_offset="+paletteOffset.ToString();
				output+="\nswizzling=Enabled";
				//output+="\n";
			}
		}
		
		ShowForm(images);
	}
	
	
	public static byte[] UnSwizzle(byte[] source, int offset, int width, int height, int bpp){
		//https://github.com/nickworonekin/puyotools/blob/c3402f2d25fc5218591c7f53dd66bc736096e64e/src/GimSharp/GimTexture/GimDataCodec.cs
		int destinationOffset = 0;

		// Incorperate the bpp into the width
		width = (width * bpp) >> 3;

		byte[] destination = new byte[width * height];

		int rowblocks = (width / 16);
		
		int magicNumber = 8;

		for (int y = 0; y < height; y++){
			for (int x = 0; x < width; x++){
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
	
	static Bitmap GetBitmapFromPalette(Dictionary<string,dynamic> image){
		int w = image["width"];
		int h = image["height"];
		byte[] data = image["unswizzled"];
		byte[] palette = image["palette"];
		int bpp = image["bpp"];
		
		Bitmap bmp = new Bitmap(w, h);
		int hp = 0;
		int bppm = 8 / bpp;
		Console.WriteLine("Parsing " + data.Length);
		Console.WriteLine();
		for(int x = 0; x < data.Length * bppm; x++){
			if(x >= w + (w * hp)) hp++;
			int tx = (x - (w * hp));
			int bx = x;
			int pi = 0;
			if(bpp==4){
				bx = (x % 2 == 0 ? x : x - 1) / 2;
				if(bx >= data.Length) break;
				if(x % 2 == 0){
					pi = ((int)(data[bx] & 0x0F)) * 4;
				}else{
					pi = ((int)(data[bx] >> 4)) * 4;
				}
			}else{
				pi = ((int)data[bx]) * 4;
			}
			Color c = Color.FromArgb(palette[pi+3],palette[pi],palette[pi+1],palette[pi+2]);
			bmp.SetPixel(tx, hp, c);
		}
		return bmp;
	}
	
	public static void DrawPicture(MouseEventArgs e = null){
		int sv = p.VerticalScroll.Value;
		p.VerticalScroll.Value = sv >= 120 ? sv - 120 : sv;
		if(e!=null) zoomLevel *= e.Delta > 0 ? 1.10f : 0.90f;
		zoomLevel = zoomLevel < .50f ? .50f : zoomLevel;
		zoomLevel = zoomLevel > 50f ? 50f : zoomLevel;
		//Console.WriteLine(zoomLevel);
		int newW = (int)((float)obmp.Width * zoomLevel);
		int newH = (int)((float)obmp.Height * zoomLevel);
		bmp = new Bitmap(newW, newH);
		//Console.WriteLine(zoomLevel.ToString("0.0##"));
		using (Graphics g = Graphics.FromImage(bmp)){
			g.InterpolationMode = InterpolationMode.NearestNeighbor;
			g.DrawImage(obmp, new Rectangle(Point.Empty, bmp.Size));
			g.DrawRectangle(new Pen(Brushes.Black, 1), new Rectangle(0, 0, bmp.Width-1, bmp.Height-1));
		}
		pb.Image = bmp;
		pb.Size = bmp.Size;
	}
		
	public static float zoomLevel = 1f;
	public static PictureBox pb;
	
	public static Panel p;
	public static Bitmap bmp;
	public static Bitmap obmp;
	
	static void ShowForm(List<Dictionary<string,dynamic>> images){
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
		for(int i = 0; i<images.Count; i++){
			lb.Items.Add(images[i]["name"]);
		}
		f.Controls.Add(lb);
		lb.SelectedIndexChanged += (s,e) => {
			bmp = GetBitmapFromPalette(images[lb.SelectedIndex]);
			obmp = bmp;
			DrawPicture();
		};
		
		if(images.Count > 0) bmp = GetBitmapFromPalette(images[0]);
		obmp = bmp;
		
		pb = new PictureBox();

		//pb.Width = bmp.Width;
		//pb.Height = bmp.Height;
		DrawPicture();
		p.Controls.Add(pb);
		p.MouseWheel += (s, e) => {
			DrawPicture(e);
		};
		
		f.ShowDialog();
	}
	
	static void Main(string[] args){
		if(args.Length>0 && File.Exists(args[0])){
			parse(args[0]);
		}else{
			Console.WriteLine("Please input a file!");
		}
		Console.ReadLine();
	}
}
