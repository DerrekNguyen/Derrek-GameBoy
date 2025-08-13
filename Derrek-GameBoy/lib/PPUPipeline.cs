using System;
using System.ComponentModel;

public static class Pipeline
{
   public static void PixelFIFOPush(UInt32 value)
   {
      FIFOEntry next = new FIFOEntry(value);
      next.next = null;

      if (PPU._context.Pfc.PixelFIFO.head == null)
      {
         // First entry
         PPU._context.Pfc.PixelFIFO.head = next;
         PPU._context.Pfc.PixelFIFO.tail = next;
      }
      else
      {
         PPU._context.Pfc.PixelFIFO.tail.next = next;
         PPU._context.Pfc.PixelFIFO.tail = next;
      }

      PPU._context.Pfc.PixelFIFO.size++;
   }

   public static UInt32 PixelFIFOPop()
   {
      if (PPU._context.Pfc.PixelFIFO.size <= 0)
      {
         Console.WriteLine("ERR IN PIXEL FIFO");
         Environment.Exit(-8);
      }

      FIFOEntry popped = PPU._context.Pfc.PixelFIFO.head;
      PPU._context.Pfc.PixelFIFO.head = popped.next;
      PPU._context.Pfc.PixelFIFO.size--;

      if (PPU._context.Pfc.PixelFIFO.head == null)
      {
         PPU._context.Pfc.PixelFIFO.tail = null;
      }

      return popped.data;
   }

   private static UInt32 FetchSpritePixels(int bit, UInt32 color, byte bgColor) 
   {
      for (int i = 0; i < PPU._context.FetchedEntryCount; ++i)
      {
         int spX = (PPU._context.FetchedEntries[i].x - 8) + 
                   (LCD._context.scrollX % 8);

         if (spX + 8 < PPU._context.Pfc.FIFOX)
         {
            // past pixel point already
            continue;
         }

         int offset = PPU._context.Pfc.FIFOX - spX;

         if (offset < 0 || offset > 7)
         {
            continue;
         }

         bit = (7 - offset);

         if (PPU._context.FetchedEntries[i].x_flip != 0)
         {
            bit = offset;
         }

         byte hi = (byte)(((PPU._context.Pfc.FetchEntryData[i * 2] & (1 << bit)) != 0) ? 1 : 0);
         byte lo = (byte)((((PPU._context.Pfc.FetchEntryData[i * 2 + 1] & (1 << bit)) != 0) ? 1 : 0) << 1);

         bool bgPriority = PPU._context.FetchedEntries[i].bgp != 0 ? true : false;

         if ((hi | lo) == 0)
         {
            // transparent
            continue;
         }

         if (!bgPriority || bgColor == 0)
         {
            color = (PPU._context.FetchedEntries[i].pn != 0) ?
                     LCD._context.sp2Colors[hi | lo] :
                     LCD._context.sp1Colors[hi | lo];

            if ((hi | lo) != 0)
            {
               break;
            }
         }
      }

      return color;
   }

   public static bool FIFOAdd()
   {
      if (PPU._context.Pfc.PixelFIFO.size > 8)
      {
         // FIFO is full
         return false;
      }

      int x = PPU._context.Pfc.FetchX - (8 - (LCD._context.scrollX % 8));

      for (int i = 0; i < 8;  i++)
      {
         int bit = 7 - i;
         byte hi = (byte)((PPU._context.Pfc.BGWFetchData[1] & (1 << bit)) != 0 ? 1 : 0);
         byte lo = (byte)(((PPU._context.Pfc.BGWFetchData[2] & (1 << bit)) != 0 ? 1 : 0) << 1);
         UInt32 color = LCD._context.bgColors[hi | lo];

         if (!LCD.LCDC_BGW_ENABLE())
         {
            color = LCD._context.bgColors[0];
         }

         if (LCD.LCDC_OBJ_ENABLE())
         {
            color = Pipeline.FetchSpritePixels(bit, color, (byte)(hi | lo));
         }

         if (x >= 0)
         {
            PixelFIFOPush(color);
            PPU._context.Pfc.FIFOX++;
         }
      }

      return true;
   }

   private static void LoadSpriteTile()
   {
      OAMLineEntry le = PPU._context.LineSprites;

      while (le != null)
      {
         int spX = (le.entry.x - 8) + (LCD._context.scrollX % 8);

         if (((spX >= PPU._context.Pfc.FetchX) && spX < PPU._context.Pfc.FetchX + 8)
            || ((spX + 8 >= PPU._context.Pfc.FetchX) && ((spX + 8) < PPU._context.Pfc.FetchX + 8)))
         {
            // need to add entry
            PPU._context.FetchedEntries[PPU._context.FetchedEntryCount++] = le.entry;
         }

         le = le.next;

         if (le == null || PPU._context.FetchedEntryCount >= 3)
         {
            // max checking 3 sprites on pixels
            break;
         }
      }
   }

   private static void LoadSpriteData(byte offset)
   {
      int curY = LCD._context.ly;
      byte spriteHeight = (byte)LCD.LCDC_OBJ_HEIGHT();

      for (int i = 0; i < PPU._context.FetchedEntryCount; i++)
      {
         byte ty = (byte)(((curY + 16) - PPU._context.FetchedEntries[i].y) * 2);

         if (PPU._context.FetchedEntries[i].y_flip != 0)
         {
            // flipped upside down
            ty = (byte)(((spriteHeight * 2) - 2) - ty);
         }

         byte tileIndex = PPU._context.FetchedEntries[i].tile;

         if (spriteHeight == 16)
         {
            tileIndex &= 0b11111110; // remove last bit
         }

         PPU._context.Pfc.FetchEntryData[(i * 2) + offset] =
            Bus.BusRead((ushort)(0x8000 + (tileIndex * 16) + ty + offset));
      }
   }

   private static void LoadWindowTile()
   {
      if (!PPUSM.windowVisible())
      {
         return;
      }

      byte windowY = LCD._context.winY;

      if (PPU._context.Pfc.FetchX + 7 >= LCD._context.winX &&
         PPU._context.Pfc.FetchX + 7 < LCD._context.winX + PPU.YRES + 14)
      {
         if (LCD._context.ly >= windowY &&
            LCD._context.ly < windowY + PPU.XRES)
         {
            byte wTileY = (byte)(PPU._context.WindowLine / 8);

            PPU._context.Pfc.BGWFetchData[0] = Bus.BusRead((ushort)((LCD.LCDC_WIN_MAP_AREA() +
               (PPU._context.Pfc.FetchX + 7 - LCD._context.winX) / 8) + (wTileY * 32)));

            if (LCD.LCDC_BGW_DATA_AREA() == 0x8800)
            {
               PPU._context.Pfc.BGWFetchData[0] += 128;
            }
         }
      }
   }

   public static void Fetch()
   {
      switch(PPU._context.Pfc.CurFetchState)
      {
         case FetchState.FS_TILE:
            PPU._context.FetchedEntryCount = 0;

            if (LCD.LCDC_BGW_ENABLE())
            {
               PPU._context.Pfc.BGWFetchData[0] = Bus.BusRead(
                  (ushort)(LCD.LCDC_BG_MAP_AREA() +
                  (PPU._context.Pfc.MapX / 8) +
                  ((PPU._context.Pfc.MapY / 8) * 32)));

               if (LCD.LCDC_BGW_DATA_AREA() == 0x8800)
               {
                  PPU._context.Pfc.BGWFetchData[0] += 128;
               }

               Pipeline.LoadWindowTile();
            }

            if (LCD.LCDC_OBJ_ENABLE() && PPU._context.LineSprites != null)
            {
               Pipeline.LoadSpriteTile();
            }

            PPU._context.Pfc.CurFetchState = FetchState.FS_DATA0;
            PPU._context.Pfc.FetchX += 8;

            break;

         case FetchState.FS_DATA0:
            PPU._context.Pfc.BGWFetchData[1] = Bus.BusRead(
               (ushort)(LCD.LCDC_BGW_DATA_AREA() +
               (PPU._context.Pfc.BGWFetchData[0] * 16) +
               PPU._context.Pfc.TileY));

            Pipeline.LoadSpriteData(0);

            PPU._context.Pfc.CurFetchState = FetchState.FS_DATA1;

            break;
         case FetchState.FS_DATA1:
            PPU._context.Pfc.BGWFetchData[2] = Bus.BusRead(
               (ushort)(LCD.LCDC_BGW_DATA_AREA() +
               (PPU._context.Pfc.BGWFetchData[0] * 16) +
               PPU._context.Pfc.TileY + 1));

            Pipeline.LoadSpriteData(1);

            PPU._context.Pfc.CurFetchState = FetchState.FS_IDLE;

            break;
         case FetchState.FS_IDLE:
            PPU._context.Pfc.CurFetchState = FetchState.FS_PUSH;

            break;
         case FetchState.FS_PUSH:
            if (Pipeline.FIFOAdd())
            {
               PPU._context.Pfc.CurFetchState = FetchState.FS_TILE;
            }

            break;
      }
   }

   public static void PushPixel()
   {
      if (PPU._context.Pfc.PixelFIFO.size > 8)
      {
         UInt32 pixelData = Pipeline.PixelFIFOPop();

         if (PPU._context.Pfc.LineX >= (LCD._context.scrollX % 8))
         {
            PPU._context.VideoBuffer[
               PPU._context.Pfc.PushedX + 
               LCD._context.ly * PPU.XRES
            ] = pixelData;

            PPU._context.Pfc.PushedX++;
         }

         PPU._context.Pfc.LineX++;
      }
   }

   public static void Process()
   {
      PPU._context.Pfc.MapY = (byte)(LCD._context.ly + LCD._context.scrollY);
      PPU._context.Pfc.MapX = (byte)(PPU._context.Pfc.FetchX + LCD._context.scrollX);
      PPU._context.Pfc.TileY = (byte)(((LCD._context.ly + LCD._context.scrollY) % 8) * 2);

      if ((PPU._context.LineTicks & 1) == 0) // Even line
      {
         Pipeline.Fetch();
      }

      Pipeline.PushPixel();
   }

   public static void FIFOReset()
   {
      while (PPU._context.Pfc.PixelFIFO.size > 0)
      {
         PixelFIFOPop();
      }

      PPU._context.Pfc.PixelFIFO.head = null;
   }
}
