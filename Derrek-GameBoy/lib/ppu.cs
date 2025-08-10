using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class OAMEntry
{
   public byte y;
   public byte x;
   public byte tile;

   private byte flags;

   public byte cgb_pn
   {
      get => (byte)(flags & 0b00000111);
      set => flags = (byte)((flags & ~0b00000111) | (value & 0b00000111));
   }

   public byte cgb_vram_bank
   {
      get => (byte)((flags >> 3) & 0b1);
      set => flags = (byte)((flags & ~0b00001000) | ((value & 0b1) << 3));
   }

   public byte pn
   {
      get => (byte)((flags >> 4) & 0b1);
      set => flags = (byte)((flags & ~0b00010000) | ((value & 0b1) << 4));
   }

   public byte x_flip
   {
      get => (byte)((flags >> 5) & 0b1);
      set => flags = (byte)((flags & ~0b00100000) | ((value & 0b1) << 5));
   }

   public byte y_flip
   {
      get => (byte)((flags >> 6) & 0b1);
      set => flags = (byte)((flags & ~0b01000000) | ((value & 0b1) << 6));
   }

   public byte bgp
   {
      get => (byte)((flags >> 7) & 0b1);
      set => flags = (byte)((flags & ~0b10000000) | ((value & 0b1) << 7));
   }
   public byte[] GetBytes()
   {
      return [y, x, tile, flags];
   }

   public void SetBytes(byte[] data)
   {
      if (data.Length != 4) throw new ArgumentException("OAM entry must be 4 bytes");
      y = data[0];
      x = data[1];
      tile = data[2];
      flags = data[3];
   }
}

public enum FetchState
{
   FS_TILE,
   FS_DATA0,
   FS_DATA1,
   FS_IDLE,
   FS_PUSH
}

public class FIFOEntry
{
   public FIFOEntry? next;
   public UInt32 data;

   public FIFOEntry(UInt32 value)
   {
      data = value;
      next = null;
   }
}

public class FIFO
{
   public FIFOEntry? head;
   public FIFOEntry? tail;
   public UInt32 size;

   public FIFO()
   {
      head = null;
      tail = null;
      size = 0;
   }
}

public class PixelFIFOContext
{
   public FetchState CurFetchState;
   public FIFO PixelFIFO = new FIFO();
   public byte LineX;
   public byte PushedX;
   public byte FetchX;
   public byte[] BGWFetchData = new byte[3];
   public byte[] FetchEntryData = new byte[6];
   public byte MapY;
   public byte MapX;
   public byte TileY;
   public byte FIFOX;
}

/*
 Bit7   BG and Window over OBJ (0=No, 1=BG and Window colors 1-3 over the OBJ)
 Bit6   Y flip          (0=Normal, 1=Vertically mirrored)
 Bit5   X flip          (0=Normal, 1=Horizontally mirrored)
 Bit4   Palette number  **Non CGB Mode Only** (0=OBP0, 1=OBP1)
 Bit3   Tile VRAM-Bank  **CGB Mode Only**     (0=Bank 0, 1=Bank 1)
 Bit2-0 Palette number  **CGB Mode Only**     (OBP0-7)
 */

public class PPUContext
{
   public OAMEntry[] OAMRam = new OAMEntry[40];
   public byte[] Vram = new byte[0x2000];

   public PixelFIFOContext Pfc;

   public UInt32 CurrentFrame;
   public UInt32 LineTicks;
   public UInt32[]? VideoBuffer;

   public PPUContext()
   {
      for (int i = 0; i < 40; i++)
      {
         OAMRam[i] = new OAMEntry();
      }
   }
}

public static class PPU
{
   public static PPUContext _context = new();

   public const int LINES_PER_FRAME = 154;
   public const int TICKS_PER_LINE = 456;
   public const int YRES = 144;
   public const int XRES = 160;

   public static void Init()
   {
      _context.CurrentFrame = 0;
      _context.LineTicks = 0;
      _context.VideoBuffer = new UInt32[160 * 144];

      _context.Pfc.LineX = 0;
      _context.Pfc.PushedX = 0;
      _context.Pfc.FetchX = 0;
      _context.Pfc.PixelFIFO.size = 0;

      LCD.Init();
      LCD.LCDS_MODE_SET((byte)LCDMode.MODE_OAM);
   }

   public static void Tick()
   {
      _context.LineTicks++;

      switch (LCD.LCDS_MODE())
      {
         case LCDMode.MODE_OAM:
            PPUSM.PPUModeOAM();
            break;
         case LCDMode.MODE_XFER:
            PPUSM.PPUModeXFER();
            break;
         case LCDMode.MODE_VBLANK:
            PPUSM.PPUModeVBLANK();
            break;
         case LCDMode.MODE_HBLANK:
            PPUSM.PPUModeHBLANK();
            break;
      }
   }

   public static void OAMWrite(UInt16 address, byte value)
   {
      if (address >= 0xFE00)
      {
         address -= 0xFE00;
      }

      int entryIndex = address / 4;
      int byteOffset = address % 4;

      var entry = _context.OAMRam[entryIndex];
      var bytes = entry.GetBytes();

      bytes[byteOffset] = value;
      entry.SetBytes(bytes);
   }

   public static byte OAMRead(UInt16 address) 
   {
      if (address >= 0xFE00)
      {
         address -= 0xFE00;
      }

      int entryIndex = address / 4;
      int byteOffset = address % 4;

      var entry = _context.OAMRam[entryIndex];
      var bytes = entry.GetBytes();

      return bytes[byteOffset];
   }

   public static void VRamWrite(UInt16 address, byte value)
   {
      _context.Vram[address - 0x8000] = value;
   }

   public static byte VRamRead(UInt16 address)
   {
      return _context.Vram[address - 0x8000];
   }
}