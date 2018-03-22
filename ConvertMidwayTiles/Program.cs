using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ConvertMidwayTiles {
    public class PaletteEntry {
        public byte Alpha;
        public byte Blue;
        public byte Green;
        public byte Red;

        public PaletteEntry(byte r, byte g, byte b, byte a) {
            Red = r;
            Green = g;
            Blue = b;
            Alpha = a;
        }
    }

    public class Palette {
        public List<PaletteEntry> Entries = new List<PaletteEntry>();
    }

    internal static class Program {
        private static readonly Palette PaletteZero = new Palette {
            Entries = {
                new PaletteEntry(0x24, 0x92, 0xff, 0xff),
                new PaletteEntry(0xff, 0x00, 0x00, 0xff),
                new PaletteEntry(0xb6, 0x00, 0x00, 0xff),
                new PaletteEntry(0xff, 0x00, 0x49, 0xff),
                
                new PaletteEntry(0xdb, 0x92, 0x24, 0xff),
                new PaletteEntry(0x00, 0x00, 0x6d, 0xff),
                new PaletteEntry(0x6d, 0x6d, 0x49, 0xff),
                new PaletteEntry(0x49, 0x49, 0x24, 0xff),
                
                new PaletteEntry(0x00, 0x00, 0x6d, 0xff),
                new PaletteEntry(0x00, 0x00, 0x00, 0xff),
                new PaletteEntry(0xdb, 0x6d, 0x24, 0xff),
                new PaletteEntry(0x6d, 0x24, 0x00, 0xff),
                
                new PaletteEntry(0x92, 0x49, 0x00, 0xff),
                new PaletteEntry(0x00, 0x49, 0x00, 0xff),
                new PaletteEntry(0x00, 0x6d, 0x00, 0xff),
                new PaletteEntry(0xff, 0xff, 0xff, 0xff)
            }
        };

        private static readonly Palette PaletteOne = new Palette {
            Entries = {
                new PaletteEntry(0x6d, 0x49, 0x00, 0xff),
                new PaletteEntry(0x92, 0x24, 0x00, 0xff),
                new PaletteEntry(0xdb, 0x92, 0x00, 0xff),
                new PaletteEntry(0x49, 0x24, 0x00, 0xff),
                
                new PaletteEntry(0xb6, 0x6d, 0x00, 0xff),
                new PaletteEntry(0x6d, 0x24, 0x00, 0xff),
                new PaletteEntry(0x00, 0x6d, 0x00, 0xff),
                new PaletteEntry(0x00, 0x24, 0xb6, 0xff),
                
                new PaletteEntry(0xff, 0xff, 0xff, 0xff),
                new PaletteEntry(0x00, 0x00, 0x00, 0xff),
                new PaletteEntry(0x24, 0x92, 0xff, 0xff),
                new PaletteEntry(0xff, 0x00, 0x00, 0xff),
                
                new PaletteEntry(0x6d, 0x6d, 0x6d, 0xff),
                new PaletteEntry(0x49, 0x49, 0x49, 0xff),
                new PaletteEntry(0x00, 0x00, 0x6d, 0xff),
                new PaletteEntry(0xff, 0xff, 0xff, 0xff)
            }
        };

        private static byte FindPaletteIndex(Color color) {
            for (var i = 0; i < PaletteOne.Entries.Count; i++) {
                var entry = PaletteOne.Entries[i];

                if (entry.Red == color.R
                &&  entry.Green == color.G
                &&  entry.Blue == color.B) {
                    return (byte) i;
                }
            }

            return 0;
        }

        public static void RipSprites() {
            const int SourceTilesWidth = 38;
            const int SourceTilesHeight = 7;
            const int SpriteWidth = 32;
            const int SpriteHeight = 32;

            var tiles = new Bitmap("/Users/jeff/Desktop/timber-sprites.png");
            var allSpriteBytes = new List<byte>();

            var cy = 0;
            var cx = 0;
            var tile = 0;

            for (var ty = 0; ty < SourceTilesHeight; ty++) {
                for (var tx = 0; tx < SourceTilesWidth; tx++) {
                    var index = 0;
                    var spriteBytes = new byte[SpriteWidth * SpriteHeight];
                    for (var y = 0; y < SpriteHeight; y++) {
                        for (var x = 0; x < SpriteWidth; x++) {
                            var color = tiles.GetPixel(cx + x, cy + y);
                            var paletteIndex = FindPaletteIndex(color);       
                            spriteBytes[index++] = paletteIndex;
                            allSpriteBytes.Add(paletteIndex);
                        }
                    }

                    var bitmap = new Bitmap(SpriteWidth, SpriteHeight, PixelFormat.Format8bppIndexed);

                    index = 0;
                    var palette = bitmap.Palette;
                    foreach (var entry in PaletteZero.Entries)
                        palette.Entries[index++] = Color.FromArgb(
                            entry.Alpha,
                            entry.Red,
                            entry.Green,
                            entry.Blue);
                    bitmap.Palette = palette;

                    var bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadWrite,
                        bitmap.PixelFormat);
                    var buffer = bitmapData.Scan0;

                    var srcOffset = 0;
                    var destOffset = 0;
                    for (var y = 0; y < SpriteHeight; y++) {
                        for (var x = 0; x < SpriteWidth; x++) {
                            Marshal.WriteByte(buffer, destOffset++, spriteBytes[srcOffset++]);
                        }
                    }

                    bitmap.UnlockBits(bitmapData);
                    bitmap.Save($"/Users/jeff/temp/sprite_{tile++}.bmp", ImageFormat.Bmp);

                    cx += SpriteWidth + 1;
                }

                cx = 0;
                cy += SpriteHeight + 1;
            }

            File.WriteAllBytes("/Users/jeff/temp/timfg.bin", allSpriteBytes.ToArray());
        }

        public static void RipTiles() {
            const int SourceTilesWidth = 75;
            const int SourceTilesHeight = 14;
            const int TileWidth = 16;
            const int TileHeight = 16;

            var tiles = new Bitmap("/Users/jeff/Desktop/timber-tiles-pal1.png");
            var allTileBytes = new List<byte>();

            var cy = 0;
            var cx = 0;
            var tile = 0;

            for (var ty = 0; ty < SourceTilesHeight; ty++) {
                for (var tx = 0; tx < SourceTilesWidth; tx++) {
                    var index = 0;
                    var tileBytes = new byte[TileWidth * TileHeight];
                    for (var y = 0; y < TileHeight; y++) {
                        for (var x = 0; x < TileWidth; x++) {
                            var color = tiles.GetPixel(cx + x, cy + y);
                            var paletteIndex = FindPaletteIndex(color);
                            tileBytes[index++] = paletteIndex;
                            allTileBytes.Add(paletteIndex);
                        }
                    }

                    var bitmap = new Bitmap(TileWidth, TileHeight, PixelFormat.Format8bppIndexed);

                    index = 0;
                    var palette = bitmap.Palette;
                    foreach (var entry in PaletteOne.Entries)
                        palette.Entries[index++] = Color.FromArgb(
                            entry.Alpha,
                            entry.Red,
                            entry.Green,
                            entry.Blue);
                    bitmap.Palette = palette;

                    var bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadWrite,
                        bitmap.PixelFormat);
                    var buffer = bitmapData.Scan0;

                    var srcOffset = 0;
                    var destOffset = 0;
                    for (var y = 0; y < TileHeight; y++) {
                        for (var x = 0; x < TileWidth; x++) {
                            Marshal.WriteByte(buffer, destOffset++, tileBytes[srcOffset++]);
                        }
                    }

                    bitmap.UnlockBits(bitmapData);
                    bitmap.Save($"/Users/jeff/temp/tile_{tile++}.bmp", ImageFormat.Bmp);

                    cx += TileWidth + 1;
                }

                cx = 0;
                cy += TileHeight + 1;
            }

            File.WriteAllBytes("/Users/jeff/temp/timbg.bin", allTileBytes.ToArray());
        }

        public static void Main(string[] args) {
            RipTiles();
            
//            UInt32 gpfsel1 = unchecked((UInt32) ~(7 << 12));
//            gpfsel1 |= 2 << 12;
//            Console.WriteLine("{0:x8}", gpfsel1);            
//            
//            UInt32 gpfsel2 = unchecked((UInt32) ~(7 << 15));
//            gpfsel2 |= 2 << 15;            
//            Console.WriteLine("{0:x8}", gpfsel2);            
//            
//            Console.WriteLine("{0:x8}", gpfsel1 & gpfsel2);            
        }
        
    }

}