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

   public static void Fetch()
   {
      switch(PPU._context.Pfc.CurFetchState)
      {
         case FetchState.FS_TILE:

            break;
         case FetchState.FS_DATA0:

            break;
         case FetchState.FS_DATA1:

            break;
         case FetchState.FS_IDLE:

            break;
         case FetchState.FS_PUSH:

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
}
