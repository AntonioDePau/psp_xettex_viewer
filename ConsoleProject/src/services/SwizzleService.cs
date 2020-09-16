using System;
using ConsoleProject.DTO;

namespace ConsoleProject.Services {

    // Swizzling textures is benefical to the GPU hardware's caching
    public static class SwizzleService {

        public static byte[] Swizzle(Texture texture) {
            int offset = 0;

            int height = texture.Height;
            // Incorperate the bpp into the width
            int width = (texture.Width * texture.BitsPerPixel) >> 3;

            byte[] destination = new byte[width * height];
            Console.WriteLine("Max: " + destination.Length + " - " + texture.Unswizzled.Length);

            int rowblocks = (width / 16);

            int magicNumber = 8;

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int blockX = x / 16;
                    int blockY = y / magicNumber;

                    int blockIndex = blockX + ((blockY) * rowblocks);
                    int blockAddress = blockIndex * 16 * magicNumber;

                    int destinationOffset = blockAddress + (x - blockX * 16) + ((y - blockY * magicNumber) * 16);

                    destination[destinationOffset] = texture.Unswizzled[offset];
                    offset++;
                }
            }

            return destination;
        }

        public static byte[] UnSwizzle(NewTexture texture) {
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
                    int offset = blockAddress + (x - blockX * 16) + ((y - blockY * magicNumber) * 16);
                    destination[destinationOffset] = texture.Binary[offset];
                    destinationOffset++;
                }
            }

            return destination;
        }
         
    }
}
