using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Assets_Editor
{
    public class ItemPakWriter
    {
        private const uint PAK_SIGNATURE = 0x4B415054; // "TPAK" in ASCII
        private const uint PAK_VERSION = 1;

        public class PakEntry
        {
            public uint ItemId { get; set; }
            public byte[] ImageData { get; set; }
        }

        public static void WritePakFile(string filePath, List<PakEntry> entries)
        {
            using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(fs);

            // Write header
            writer.Write(PAK_SIGNATURE);
            writer.Write(PAK_VERSION);
            writer.Write((uint)entries.Count);

            // Calculate offsets
            long indexSectionSize = entries.Count * 16; // ID(4) + Offset(8) + Size(4)
            long currentOffset = 12 + indexSectionSize; // Header(12) + Index

            // Write index section
            foreach (var entry in entries)
            {
                writer.Write(entry.ItemId);
                writer.Write(currentOffset);
                writer.Write(entry.ImageData.Length);
                currentOffset += entry.ImageData.Length;
            }

            // Write data section
            foreach (var entry in entries)
            {
                writer.Write(entry.ImageData);
            }
        }
    }

    public class ItemPakReader
    {
        private const uint PAK_SIGNATURE = 0x4B415054;

        private Dictionary<uint, (long Offset, int Size)> index;
        private string pakPath;

        public ItemPakReader(string filePath)
        {
            pakPath = filePath;
            index = new Dictionary<uint, (long, int)>();
            LoadIndex();
        }

        private void LoadIndex()
        {
            using FileStream fs = new(pakPath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fs);

            uint signature = reader.ReadUInt32();
            if (signature != PAK_SIGNATURE)
                throw new Exception("Invalid PAK file signature");

            uint version = reader.ReadUInt32();
            uint itemCount = reader.ReadUInt32();

            for (uint i = 0; i < itemCount; i++)
            {
                uint itemId = reader.ReadUInt32();
                long offset = reader.ReadInt64();
                int size = reader.ReadInt32();
                index[itemId] = (offset, size);
            }
        }

        public Bitmap GetItemImage(uint itemId)
        {
            if (!index.ContainsKey(itemId))
                return null;

            var (offset, size) = index[itemId];

            using FileStream fs = new(pakPath, FileMode.Open, FileAccess.Read);
            fs.Seek(offset, SeekOrigin.Begin);

            byte[] imageData = new byte[size];
            fs.Read(imageData, 0, size);

            using MemoryStream ms = new(imageData);
            return new Bitmap(ms);
        }

        public List<uint> GetAllItemIds()
        {
            return new List<uint>(index.Keys);
        }

        public bool ContainsItem(uint itemId)
        {
            return index.ContainsKey(itemId);
        }

        public int Count => index.Count;
    }
}
