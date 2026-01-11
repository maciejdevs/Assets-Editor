# PAK File Format Specification

## Overview

The **PAK** (Package Archive) format is a custom binary file format designed for efficient storage and retrieval of sprite images by ID. It stores multiple PNG images in a single file with an index for fast random access.

## File Structure

### Binary Layout

```
┌─────────────────────────────────────────┐
│            HEADER (12 bytes)            │
├─────────────────────────────────────────┤
│    INDEX SECTION (16 bytes × count)     │
├─────────────────────────────────────────┤
│       DATA SECTION (variable size)      │
└─────────────────────────────────────────┘
```

### Detailed Specification

#### 1. Header Section (12 bytes)

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0x00   | 4    | uint32 | Signature: `0x4B415054` ("TPAK") |
| 0x04   | 4    | uint32 | Version: `1`                   |
| 0x08   | 4    | uint32 | Item Count                     |

#### 2. Index Section (16 bytes per entry)

For each item (repeated `Item Count` times):

| Offset   | Size | Type   | Description                          |
|----------|------|--------|--------------------------------------|
| 0x00     | 4    | uint32 | Item ID (sprite/object identifier)   |
| 0x04     | 8    | int64  | Data Offset (absolute file position) |
| 0x0C     | 4    | int32  | Data Size (bytes)                    |

**Total Index Size**: `16 × Item Count` bytes

#### 3. Data Section (variable size)

Sequential PNG image data for each item. Images are stored in the same order as the index entries.

Each data block contains:
- Complete PNG file data (including PNG header and all chunks)
- Variable size based on image complexity and compression

## Format Properties

| Property              | Value                          |
|-----------------------|--------------------------------|
| **File Extension**    | `.pak`                        |
| **Byte Order**        | Little-endian                 |
| **Image Format**      | PNG (32-bit ARGB)             |
| **Max Items**         | 4,294,967,295 (uint32 max)    |
| **Max File Size**     | ~9 EB (int64 offset limit)    |
| **Compression**       | PNG internal compression      |

## Reading Algorithm

```
1. Read header (12 bytes)
2. Verify signature = 0x4B415054
3. Read item count
4. Load index into memory (16 × count bytes)
5. For each ID lookup:
   a. Find index entry by ID
   b. Seek to data offset
   c. Read data size bytes
   d. Decode PNG from bytes
```

## Performance Characteristics

- **Index Lookup**: O(1) with hash map, O(log n) with binary search
- **Image Load**: Single file seek + read operation
- **Memory Footprint**: Index only (~16 bytes per item)
- **Random Access**: Yes (efficient)
- **Sequential Access**: Yes (efficient)

## Usage Example

### C# Implementation

```csharp
// Reading
var pakReader = new ItemPakReader("sprites.pak");
Bitmap sprite = pakReader.GetItemImage(3392);

// Check availability
if (pakReader.ContainsItem(5001))
{
    var img = pakReader.GetItemImage(5001);
}

// List all IDs
var allIds = pakReader.GetAllItemIds();
Console.WriteLine($"Total sprites: {pakReader.Count}");
```

### Python Implementation Example

```python
import struct
from PIL import Image
from io import BytesIO

class PakReader:
    def __init__(self, filepath):
        self.filepath = filepath
        self.index = {}
        self._load_index()
    
    def _load_index(self):
        with open(self.filepath, 'rb') as f:
            signature = struct.unpack('<I', f.read(4))[0]
            assert signature == 0x4B415054, "Invalid PAK signature"
            
            version = struct.unpack('<I', f.read(4))[0]
            count = struct.unpack('<I', f.read(4))[0]
            
            for _ in range(count):
                item_id = struct.unpack('<I', f.read(4))[0]
                offset = struct.unpack('<q', f.read(8))[0]
                size = struct.unpack('<i', f.read(4))[0]
                self.index[item_id] = (offset, size)
    
    def get_image(self, item_id):
        if item_id not in self.index:
            return None
        
        offset, size = self.index[item_id]
        with open(self.filepath, 'rb') as f:
            f.seek(offset)
            data = f.read(size)
            return Image.open(BytesIO(data))

# Usage
pak = PakReader('sprites.pak')
image = pak.get_image(3392)
image.show()
```

## Advantages

**Single File Distribution**: All sprites in one file  
**Fast Random Access**: Direct seek to any item by ID  
**Efficient Storage**: PNG compression reduces file size  
**Simple Format**: Easy to implement in any language  
**No Unpacking Required**: Load images directly from archive  
**Scalable**: Handles millions of items efficiently  

## Limitations

**Read-Only**: Format is designed for read operations  
**No Compression**: Archive itself is not compressed (PNG images are)  
**Fixed Index**: Adding items requires rebuilding the entire file  

## Version History

- **Version 1** (2026-01-11): Initial format specification
  - Basic header + index + data structure
  - PNG image storage
  - uint32 IDs, int64 offsets

## Tools

- **Assets Editor**: Create PAK files from sprites
- **ItemPakWriter**: C# class for writing PAK files
- **ItemPakReader**: C# class for reading PAK files

## File Example

```
Offset   | Data (hex)                      | Description
---------|---------------------------------|------------------
0x000000 | 54 50 41 4B                     | Signature "TPAK"
0x000004 | 01 00 00 00                     | Version: 1
0x000008 | 02 00 00 00                     | Count: 2 items
0x00000C | 01 00 00 00                     | Item ID: 1
0x000010 | 2C 00 00 00 00 00 00 00         | Offset: 0x2C
0x000018 | A5 03 00 00                     | Size: 933 bytes
0x00001C | 02 00 00 00                     | Item ID: 2
0x000020 | D1 03 00 00 00 00 00 00         | Offset: 0x3D1
0x000028 | 7B 02 00 00                     | Size: 635 bytes
0x00002C | 89 50 4E 47 0D 0A 1A 0A ...     | PNG data (item 1)
0x0003D1 | 89 50 4E 47 0D 0A 1A 0A ...     | PNG data (item 2)
```

## Related Formats

- **SPR**: Original Tibia sprite format (RLE compressed)
- **DAT**: Tibia metadata format (references sprites)
- **ZIP**: General-purpose archive (slower random access)
- **TAR**: UNIX archive format (sequential access)
