using System;

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

         if (x >= 0)
         {
            PixelFIFOPush(color);
            PPU._context.Pfc.FIFOX++;
         }
      }

      return true;
   }

   public static void Fetch()
   {
      switch(PPU._context.Pfc.CurFetchState)
      {
         case FetchState.FS_TILE:
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
            }

            PPU._context.Pfc.CurFetchState = FetchState.FS_DATA0;
            PPU._context.Pfc.FetchX += 8;

            break;

         case FetchState.FS_DATA0:
            PPU._context.Pfc.BGWFetchData[1] = Bus.BusRead(
               (ushort)(LCD.LCDC_BGW_DATA_AREA() +
               (PPU._context.Pfc.BGWFetchData[0] * 16) +
               PPU._context.Pfc.TileY));

            PPU._context.Pfc.CurFetchState = FetchState.FS_DATA1;

            break;
         case FetchState.FS_DATA1:
            PPU._context.Pfc.BGWFetchData[2] = Bus.BusRead(
               (ushort)(LCD.LCDC_BGW_DATA_AREA() +
               (PPU._context.Pfc.BGWFetchData[0] * 16) +
               PPU._context.Pfc.TileY + 1));

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
