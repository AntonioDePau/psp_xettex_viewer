using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using XetEditor.DTO;
using XetEditor.Utils;
using log4net;

namespace XetEditor.Services {

    public class TextureConverter {

        private static readonly ILog LOG = LogManager.GetLogger(typeof(TextureConverter));

        public List<Texture> ParseFile(string filename) {
            using (BigEndianBinaryReader reader = new BigEndianBinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
                TexHeaderMap textureHeader = this.PopulateHeaderMap(reader);

                if (textureHeader.FileExtension != "XET.") {
                    throw new InvalidDataException("Invalid file extension!");
                }

                LOG.Info("Image count:" + textureHeader.FileCount.ToString());

                List<Texture> images = new List<Texture>();
                for (int i = 0; i < textureHeader.FileCount; i++) {
                    images.Add(this.PopulateTexture(i, reader, textureHeader));
                }

                return images;
            }
        }

        // TODO refactor
        public static Texture RepackTexture(string file) {
            string extension = ".png";
            if (Path.GetExtension(file).ToLower() != extension) {
                LOG.Warn("Selected file is not the required .png extension!");
                throw new InvalidDataException("Invalid file extension!");
            }

            Image image1 = Image.FromFile(file);
            Bitmap bmp2 = new Bitmap(image1);
            LOG.Debug("Pixel format: " + bmp2.PixelFormat);

            List<Color> palette = new List<Color>();
            List<Color> completeColors = new List<Color>();

            for (int y = 0; y < bmp2.Height; y++) {
                for (int x = 0; x < bmp2.Width; x++) {
                    Color color = bmp2.GetPixel(x, y);
                    if (palette.IndexOf(color) == -1 && palette.Count < 256) {
                        palette.Add(color);
                    }
                    if (completeColors.IndexOf(color) == -1) {
                        completeColors.Add(color);
                    }
                }
            }

            LOG.Info("Saved " + palette.Count + " colors out of " + completeColors.Count);

            // TODO add comment what this does
            while (palette.Count < 16) {
                palette.Insert(0, Color.FromArgb(0, 0, 0, 0));
            }

            // TODO is this condition is always TRUE from the above loop?
            // TODO add comment what this does
            if (palette.Count > 16) {
                while (palette.Count < 256) {
                    palette.Insert(0, Color.FromArgb(0, 0, 0, 0));
                }
            }

            palette.Sort((x, y) => x.A.CompareTo(y.A));

            if (LOG.IsDebugEnabled) {
                LOG.Debug("New palette:");
                for (int ci = 0; ci < palette.Count; ci++) {
                    Color c = palette[ci];
                    LOG.Debug(ci + ": " + c.ToString());
                }
            }

            byte[] paletteBinary = new byte[(palette.Count) * 4];

            LOG.Info("Total color count: " + palette.Count);

            for (int i = 0; i < palette.Count; i++) {
                paletteBinary[i * 4] = palette[i].R;
                paletteBinary[i * 4 + 1] = palette[i].G;
                paletteBinary[i * 4 + 2] = palette[i].B;
                paletteBinary[i * 4 + 3] = palette[i].A;
            }

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
                    if (colorIndex == -1) {
                        colorIndex = ColorCompare.GetClosest(texture.Colors, c);
                        LOG.Info("Could not find: " + c.ToString());
                        LOG.Info("Replaced with: " + texture.Colors[colorIndex].ToString());
                    }
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

            return texture;
        }

        private TexHeaderMap PopulateHeaderMap(BigEndianBinaryReader f) {
            return new TexHeaderMap {
                FileExtension = System.Text.Encoding.UTF8.GetString(f.ReadBytes(4), 0, 4),
                FileVersion = f.ReadInt32(),
                FileCount = f.ReadInt16(),
                FileCountB = f.ReadInt16(),
                Unk0c = f.ReadInt32(),
                FileListOffset = f.ReadInt32(),
                FileInfoOffset = f.ReadInt32(),
                FileNamesOffset = f.ReadInt32()
            };
        }

        private Texture PopulateTexture(int i, BigEndianBinaryReader f, TexHeaderMap textureHeader) {
            Texture texture = new Texture();

            f.BaseStream.Seek(textureHeader.FileListOffset + (4 * i) + (12 * i), SeekOrigin.Begin);
            texture.BitsPerPixel = f.ReadInt32() == 5 ? 8 : 4; // TODO documentation
            texture.Number = f.ReadInt32();
            texture.Unk08 = f.ReadInt32();
            texture.InfoOffset = f.ReadInt32();

            f.BaseStream.Seek(texture.InfoOffset, SeekOrigin.Begin);
            texture.Width = f.ReadInt16();
            texture.Height = f.ReadInt16();
            texture.Largest = f.ReadInt16();
            texture.Unk06 = f.ReadInt16();
            texture.DataOffset = f.ReadInt32();

            f.BaseStream.Seek(textureHeader.FileInfoOffset + 4 + (8 * i), SeekOrigin.Begin);
            texture.PaletteOffset = f.ReadInt32();

            byte[] nb = f.ReadAt(textureHeader.FileNamesOffset, (int)f.BaseStream.Length - textureHeader.FileNamesOffset);
            string name = System.Text.Encoding.UTF8.GetString(nb, 0, nb.Length).Split('\x00')[i];
            texture.Name = name;

            texture.Binary = f.ReadAt(texture.DataOffset, texture.PaletteOffset - texture.DataOffset);
            texture.Palette = f.ReadAt(texture.PaletteOffset, texture.InfoOffset - texture.PaletteOffset);
            texture.Unswizzled = UnSwizzle(texture);
            texture.Bitmap = GetBitmapFromPalette(texture);

            LOG.Info(texture);

            return texture;
        }
        
        private List<Color> GetColorsFromPalette(byte[] palette) {
            List<Color> colors = new List<Color>();
            for (int i = 0; i < palette.Length; i+=4) {
                Color col = Color.FromArgb(palette[i + 3], palette[i], palette[i + 1], palette[i + 2]);
                colors.Add(col);
            }
            return colors;
        }
        
        private Bitmap GetBitmapFromPalette(Texture texture) {
            Bitmap bmp = new Bitmap(texture.Width, texture.Height);
            int row = -1;
            int bitsPerPixelMultiplier = 8 / texture.BitsPerPixel;
            
            texture.Colors = GetColorsFromPalette(texture.Palette);

            for (int x = 0; x < texture.Unswizzled.Length * bitsPerPixelMultiplier; x++) {
                if (x % texture.Width == 0) {
                    row++;
                }

                int col = (x - (texture.Width * row));
                int dataIndex = x;
                int paletteIndex = 0;
                if (texture.BitsPerPixel == 4) {
                    if (x % 2 == 0) {
                        paletteIndex = (texture.Unswizzled[dataIndex / 2] & 0x0f);
                    } else {
                        paletteIndex = (texture.Unswizzled[(dataIndex - 1) / 2] >> 4);                      
                    }
                } else {
                    paletteIndex = texture.Unswizzled[dataIndex];
                }
                Color c = texture.Colors[paletteIndex];
                bmp.SetPixel(col, row, c);
            }

            return bmp;
        }

        private Bitmap GetIndexedBitmapFromPalette(Texture texture) {
            Bitmap bmp = new Bitmap(texture.Width, texture.Height, PixelFormat.Format8bppIndexed);
            if (texture.BitsPerPixel == 4)
                bmp = new Bitmap(texture.Width, texture.Height, PixelFormat.Format4bppIndexed);
            int row = 0;
            int bitsPerPixelMultiplier = 8 / texture.BitsPerPixel;

            ColorPalette pal = bmp.Palette;
            List<Color> colors = new List<Color>();
            byte[] palette = texture.Palette;
            for (int c = 0; c < pal.Entries.Length; c++) {
                Color col = Color.FromArgb(palette[c * 4 + 3], palette[c * 4], palette[c * 4 + 1], palette[c * 4 + 2]);
                pal.Entries[c] = col;
                texture.Colors.Add(col);
            }

            bmp.Palette = pal;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

            byte[] bytes = new byte[data.Height * data.Stride];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            for (int x = 0; x < texture.Unswizzled.Length; x++) {
                if (x >= texture.Width + (texture.Width * row)){
                    row++;
                }

                int col = (x - (texture.Width * row));
                int dataIndex = x;
                int paletteIndex = 0;
                if (texture.BitsPerPixel == 4) {
                    paletteIndex = (texture.Unswizzled[dataIndex] & 0xF0) >> 4 | (texture.Unswizzled[dataIndex] & 0x0F) << 4;
                } else {
                    paletteIndex = texture.Unswizzled[dataIndex];
                }
                int p = row * (texture.Width) + col;
                bytes[p] = (byte)paletteIndex;
            }


            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        private byte[] UnSwizzle(Texture texture) {
            NewTexture swizzledTexture = new NewTexture {
                Width = texture.Width,
                Height = texture.Height,
                BitsPerPixel = texture.BitsPerPixel,
                Binary = texture.Binary
            };
            return SwizzleService.UnSwizzle(swizzledTexture);
        }

        public static void SaveTexFile(List<Texture> images) {
            TexHeaderMap header = new TexHeaderMap((short)images.Count);

            List<byte> file = Enumerable.Repeat((byte)0x00, 48).ToList();
            for (int i = 0; i < images.Count; i++) {
                Texture texture = images[i];
                texture.DataOffset = file.Count;
                LOG.Debug(texture.Binary.Length);

                file.AddRange(texture.Binary.ToList());
                texture.PaletteOffset = file.Count;
                file.AddRange(texture.Palette.ToList());
                texture.InfoOffset = file.Count;
                file.AddRange(BitConverter.GetBytes((short)texture.Width).ToList());
                file.AddRange(BitConverter.GetBytes((short)texture.Height).ToList());
                file.AddRange(BitConverter.GetBytes(texture.Largest).ToList());
                file.AddRange(new List<byte> { 0x00, 0x00 });
                file.AddRange(BitConverter.GetBytes(texture.DataOffset).ToList());
                file.AddRange(new List<byte> { 0x00, 0x00, 0x00, 0x00 });
            }

            header.FileListOffset = file.Count;
            for (int i = 0; i < images.Count; i++) {
                Texture texture = images[i];
                file.AddRange(BitConverter.GetBytes(texture.BitsPerPixel == 8 ? 5 : 4).ToList());
                file.AddRange(BitConverter.GetBytes(i).ToList());
                file.AddRange(BitConverter.GetBytes(0).ToList());
                file.AddRange(BitConverter.GetBytes(texture.InfoOffset).ToList());
            }

            header.FileInfoOffset = file.Count;
            for (int i = 0; i < images.Count; i++) {
                Texture texture = images[i];
                file.AddRange(BitConverter.GetBytes((short)3).ToList());
                file.AddRange(BitConverter.GetBytes((short)(texture.BitsPerPixel == 8 ? 8447 : 767)).ToList());
                file.AddRange(BitConverter.GetBytes(texture.PaletteOffset).ToList());
            }
            if (file.Count % 16 != 0) file.AddRange(Enumerable.Repeat((byte)0x00, 16 - (file.Count % 16)).ToList());

            header.FileNamesOffset = file.Count;
            for (int i = 0; i < images.Count; i++) {
                Texture texture = images[i];
                file.AddRange(System.Text.Encoding.UTF8.GetBytes(texture.Name).ToList());
                if (i < images.Count) file.Add(0);
            }
            if (file.Count % 16 != 0) file.AddRange(Enumerable.Repeat((byte)0x00, 16 - (file.Count % 16)).ToList());

            file.RemoveRange(0, 4);
            file.InsertRange(0, System.Text.Encoding.UTF8.GetBytes(header.FileExtension).ToList());

            file.RemoveRange(4, 4);
            file.InsertRange(4, BitConverter.GetBytes(1).ToList());

            file.RemoveRange(8, 2);
            file.InsertRange(8, BitConverter.GetBytes(header.FileCount).ToList());

            file.RemoveRange(10, 2);
            file.InsertRange(10, BitConverter.GetBytes(header.FileCountB).ToList());

            file.RemoveRange(16, 4);
            file.InsertRange(16, BitConverter.GetBytes(header.FileListOffset).ToList());

            file.RemoveRange(20, 4);
            file.InsertRange(20, BitConverter.GetBytes(header.FileInfoOffset).ToList());

            file.RemoveRange(24, 4);
            file.InsertRange(24, BitConverter.GetBytes(header.FileNamesOffset).ToList());

            //IMPLEMENT PADDING HERE???
            File.WriteAllBytes("__TEST.xet", file.ToArray());
        }
    }

}
