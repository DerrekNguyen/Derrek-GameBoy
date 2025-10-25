using System;
using System.Net;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class LCDContext
{
   // registers
   public byte lcdc;
   public byte lcds;
   public byte scrollY;
   public byte scrollX;
   public byte ly;
   public byte lyCompare;
   public byte dma;
   public byte bgPalette;
   public byte[] objPalette = new byte[2];
   public byte winY;
   public byte winX;

   // other data;
   public UInt32[] bgColors = new UInt32[4];
   public UInt32[] sp1Colors = new UInt32[4];
   public UInt32[] sp2Colors = new UInt32[4];
}

public enum LCDMode
{
   MODE_HBLANK,
   MODE_VBLANK,
   MODE_OAM,
   MODE_XFER
}

public enum StatSrc
{
   SS_HBLANK = (1 << 3),
   SS_VBLANK = (1 << 4),
   SS_OAM = (1 << 5),
   SS_LYC = (1 << 6),
}

public static class LCD
{
   public static LCDContext _context = new LCDContext();

   public static bool LCDC_BGW_ENABLE()
   {
      return Common.BIT(_context.lcdc, 0);
   }

   public static bool LCDC_OBJ_ENABLE()
   {
      return Common.BIT(_context.lcdc, 1);
   }

   public static int LCDC_OBJ_HEIGHT()
   {
      return Common.BIT(_context.lcdc, 2) ? 16 : 8;
   }

   public static int LCDC_BG_MAP_AREA()
   {
      return Common.BIT(_context.lcdc, 3) ? 0x9C00 : 0x9800;
   }

   public static int LCDC_BGW_DATA_AREA()
   {
      return Common.BIT(_context.lcdc, 4) ? 0x8000 : 0x8800;
   }

   public static bool LCDC_WIN_ENABLE()
   {
      return Common.BIT(_context.lcdc, 5);
   }

   public static int LCDC_WIN_MAP_AREA()
   {
      return Common.BIT(_context.lcdc, 6) ? 0x9C00 : 0x9800;
   }

   public static bool LCDC_LCD_ENABLE()
   {
      return Common.BIT(_context.lcdc, 7);
   }

   public static LCDMode LCDS_MODE()
   {
      return (LCDMode)(_context.lcds & 0b11);
   }
   
   public static void LCDS_MODE_SET(byte mode)
   {
      _context.lcds &= 0b11111100;
      _context.lcds |= mode;
   }

   public static bool LCDS_LYC()
   {
      return Common.BIT(_context.lcds, 2);
   }

   public static void LCDS_LYC_SET(byte b)
   {
      Common.BIT_SET(ref _context.lcds, 2, (sbyte)b);
   } 

   public static byte LCDS_STAT_INT(StatSrc src)
   {
      return (byte)(_context.lcds & (byte)src);
   }

   public static ulong[] colorsDefault = {0xFFFFFFFF, 0xFFAAAAAA, 0xFF555555, 0xFF000000};

   public static void Init()
   {
      _context.lcdc = 0x91;
      _context.scrollX = 0;
      _context.scrollY = 0;
      _context.ly = 0;
      _context.lyCompare = 0;
      _context.bgPalette = 0xFC;
      _context.objPalette[0] = 0xFF;
      _context.objPalette[1] = 0xFF;
      _context.winY = 0;
      _context.winX = 0;

      for (int i = 0; i < 4; i++)
      {
         _context.bgColors[i] = (uint)colorsDefault[i];
         _context.sp1Colors[i] = (uint)colorsDefault[i];
         _context.sp2Colors[i] = (uint)colorsDefault[i];
      }
   }

   public static byte Read(UInt16 address)
   {
      byte offset = (byte)(address - 0xFF40);
      return offset switch
      {
         0x00 => _context.lcdc,
         0x01 => _context.lcds,
         0x02 => _context.scrollY,
         0x03 => _context.scrollX,
         0x04 => _context.ly,
         0x05 => _context.lyCompare,
         0x06 => _context.dma,
         0x07 => _context.bgPalette,
         0x08 => _context.objPalette[0],
         0x09 => _context.objPalette[1],
         0x0A => _context.winY,
         0x0B => _context.winX,
         _ => 0
      };
   }

   public static void UpdatePalette(byte paletteData, byte pal)
   {
      uint[] pColors = _context.bgColors;

      switch (pal)
      {
         case 1:
            pColors = _context.sp1Colors;
            break;
         case 2:
            pColors = _context.sp2Colors;
            break;
      }

      pColors[0] = (uint)colorsDefault[paletteData & 0b11];
      pColors[1] = (uint)colorsDefault[(paletteData >> 2) & 0b11];
      pColors[2] = (uint)colorsDefault[(paletteData >> 4) & 0b11];
      pColors[3] = (uint)colorsDefault[(paletteData >> 6) & 0b11];
   }

   public static void Write(UInt16 address, byte value)
   {
      byte offset = (byte)(address - 0xFF40);
      switch (offset)
      {
         case 0x00: _context.lcdc = value; break;
         case 0x01: _context.lcds = value; break;
         case 0x02: _context.scrollY = value; break;
         case 0x03: _context.scrollX = value; break;
         case 0x04: break; // read-only
         case 0x05: _context.lyCompare = value; break;
         case 0x06: _context.dma = value; break;
         case 0x07: _context.bgPalette = value; break;
         case 0x08: _context.objPalette[0] = value; break;
         case 0x09: _context.objPalette[1] = value; break;
         case 0x0A: _context.winY = value; break;
         case 0x0B: _context.winX = value; break;
         default:
            break;
      }

      if (address == 0xFF46)
      {
         // 0xFF46 = DMA
         DMA.Start(value);
      }
      else if (address == 0xFF47)
      {
         UpdatePalette(value, 0);
      }
      else if (address == 0xFF48)
      {
         UpdatePalette((byte)(value & 0b11111100), 1);
      }
      else if (address == 0xFF49)
      {
         UpdatePalette((byte)(value & 0b11111100), 2);
      }
   }
}