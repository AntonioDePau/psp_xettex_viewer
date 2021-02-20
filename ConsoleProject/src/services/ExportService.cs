using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ConsoleProject.DTO;

namespace ConsoleProject.Services {

    public static class ExportService {

        public static void WriteBitmap(List<Texture> images) {
            if (images == null || images.Count == 0) return;
            string directoryName = "Xet2Bmp";
            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

            for (int i = 0; i < images.Count; i++) {
                Texture img = images[i];
                List<byte> file = new List<byte>();
                byte[] b = { 0x42, 0x4D, 0, 0, 0, 0, 0, 0, 0, 0 };
                file.AddRange(b);
                int dataOffset = 54 + img.Colors.Count * 4;
                file.AddRange(BitConverter.GetBytes(dataOffset));
                file.AddRange(BitConverter.GetBytes(40));
                file.AddRange(BitConverter.GetBytes(img.Width));
                file.AddRange(BitConverter.GetBytes(img.Height));
                file.AddRange(BitConverter.GetBytes((short)1));
                file.AddRange(BitConverter.GetBytes((short)img.BitsPerPixel));
                file.AddRange(BitConverter.GetBytes(0));
                file.AddRange(BitConverter.GetBytes(0));
                file.AddRange(BitConverter.GetBytes(2834));
                file.AddRange(BitConverter.GetBytes(2834));
                file.AddRange(BitConverter.GetBytes(img.Colors.Count));
                file.AddRange(BitConverter.GetBytes(img.Colors.Count));

                for (int ci = 0; ci < img.Colors.Count; ci++) {
                    Color c = img.Colors[ci];
                    if (c.R == 0 && c.G == 0 && c.B == 0) c = Color.FromArgb(255, 0, 144, 64);
                    file.Add(c.B);
                    file.Add(c.G);
                    file.Add(c.R);
                    file.Add(c.A);
                }

                int bitsPerPixelMultiplier = 8 / img.BitsPerPixel;
                Console.WriteLine("BPP: " + img.BitsPerPixel);
                for (int y = (img.Height) - 1; y >= 0; y--) {
                    for (int x = 0; x < (img.Width / bitsPerPixelMultiplier); x++) {
                        byte by = img.Unswizzled[(y * (img.Width / bitsPerPixelMultiplier)) + x];
                        if (img.BitsPerPixel == 4)
                            by = (byte)((by & 0xF0) >> 4 | (by & 0x0F) << 4);
                        file.Add(by);
                    }
                }
                file.InsertRange(2, (BitConverter.GetBytes(file.Count)));
                file.RemoveRange(6, 4);

                File.WriteAllBytes(directoryName + "/" + img.Name + ".bmp", file.ToArray());
            }
        }

        public static void WriteTextures(List<Texture> images, string epath) {
            if (images == null || images.Count == 0) {
                return;
            }

            string extension = "Png";
            //string directoryName = "Xet2" + extension;
            string directoryName = epath;
            ImageFormat format = ImageFormat.Bmp;

            switch (extension) {
                case "Gif":
                    format = ImageFormat.Gif;
                    break;
                case "Png":
                    format = ImageFormat.Png;
                    break;
                default:
                    format = ImageFormat.Bmp;
                    break;
            }

            if (!Directory.Exists(directoryName)) {
                Directory.CreateDirectory(directoryName);
            }

            for (int i = 0; i < images.Count; i++) {
                Console.WriteLine(images[i].Bitmap.PixelFormat.ToString());
                images[i].Bitmap.Save(directoryName + "/" + images[i].Name + "." + extension.ToLower(), format);
            }
        }
    }
}
