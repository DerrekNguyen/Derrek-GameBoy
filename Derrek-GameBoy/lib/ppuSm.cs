using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System;

public static class PPUSM
{
   private static double TargetFrameTime = 1000.0 / 60.0;
   private static long PrevFrameTime = 0;
   private static long StartTimer = 0;
   private static long FrameCount = 0;

   public static bool windowVisible()
   {
      return (LCD.LCDC_WIN_ENABLE() && LCD._context.winX >= 0 &&
         LCD._context.winX <= 166 && LCD._context.winY >= 0 &&
         LCD._context.winY < PPU.YRES);
   }

   private static void IncrementLy()
   {
      if (PPUSM.windowVisible() && 
         LCD._context.ly >= LCD._context.winY &&
         LCD._context.ly < LCD._context.winY + PPU.YRES)
      {
         PPU._context.WindowLine++;
      }

      LCD._context.ly++;

      if (LCD._context.ly == LCD._context.lyCompare)
      {
         LCD.LCDS_LYC_SET(1);

         if (LCD.LCDS_STAT_INT(StatSrc.SS_LYC) != 0)
         {
            CPU.RequestInterrupt(InterruptType.IT_LCD_STAT);
         }
      }
      else
      {
         LCD.LCDS_LYC_SET(0);
      }
   }

   private static void LoadLineSprites()
   {
      int curY = LCD._context.ly;

      byte spriteHeight = (byte)LCD.LCDC_OBJ_HEIGHT();
      for (int i = 0; i < PPU._context.LineEntryArray.Length; i++)
      {
         if (PPU._context.LineEntryArray[i] == null)
            PPU._context.LineEntryArray[i] = new OAMLineEntry();
         PPU._context.LineEntryArray[i].entry = null;
         PPU._context.LineEntryArray[i].next = null;
      }

      for (int i = 0; i < 40; ++i)
      {
         OAMEntry e = PPU._context.OAMRam[i];

         if (e.x == 0)
         {
            // x = 0 means not visible
            continue;
         }

         if (PPU._context.LineSpriteCount >= 10)
         {
            // max 10 sprites per line
            break;
         }

         if (e.y <= curY + 16 && e.y + spriteHeight > curY + 16)
         {
            // this sprite is on the current line
            OAMLineEntry entry = PPU._context.LineEntryArray[
               PPU._context.LineSpriteCount++
            ];

            entry.entry = e;
            entry.next = null;

            if (PPU._context.LineSprites == null || PPU._context.LineSprites.entry.x > e.x)
            {
               entry.next = PPU._context.LineSprites;
               PPU._context.LineSprites = entry;
               continue;
            }

            // Do some sorting

            OAMLineEntry le = PPU._context.LineSprites;
            OAMLineEntry prev = le;

            while (le != null)
            {
               if (le.entry.x > e.x)
               {
                  prev.next = entry;
                  entry.next = le;
                  break;
               }

               if (le.next == null)
               {
                  le.next = entry;
                  break;
               }

               prev = le;
               le = le.next;
            }
         }
      }
   }

   public static void PPUModeOAM()
   {
      if (PPU._context.LineTicks >= 80)
      {
         LCD.LCDS_MODE_SET((byte)LCDMode.MODE_XFER);

         PPU._context.Pfc.CurFetchState = FetchState.FS_TILE;
         PPU._context.Pfc.LineX = 0;
         PPU._context.Pfc.FetchX = 0;
         PPU._context.Pfc.PushedX = 0;
         PPU._context.Pfc.FIFOX = 0;
      }

      if (PPU._context.LineTicks == 1)
      {
         // Read OAM on the first tick only
         PPU._context.LineSprites = null;
         PPU._context.LineSpriteCount = 0;

         PPUSM.LoadLineSprites();
      }
   }

   public static void PPUModeXFER()
   {
      Pipeline.Process();

      if (PPU._context.Pfc.PushedX >= PPU.XRES)
      {
         Pipeline.FIFOReset();
         LCD.LCDS_MODE_SET((byte)LCDMode.MODE_HBLANK);

         if (LCD.LCDS_STAT_INT(StatSrc.SS_HBLANK) != 0)
         {
            CPU.RequestInterrupt(InterruptType.IT_LCD_STAT);
         }
      }
   }

   public static void PPUModeVBLANK()
   {
      if (PPU._context.LineTicks >= PPU.TICKS_PER_LINE)
      {
         PPUSM.IncrementLy();

         if (LCD._context.ly >= PPU.LINES_PER_FRAME)
         {
            LCD.LCDS_MODE_SET((byte)LCDMode.MODE_OAM);
            LCD._context.ly = 0;
            PPU._context.WindowLine = 0;
         }

         PPU._context.LineTicks = 0;
      }
   }

   public static void PPUModeHBLANK()
   {
      if (PPU._context.LineTicks >= PPU.TICKS_PER_LINE)
      {
         PPUSM.IncrementLy();

         if (LCD._context.ly >= PPU.YRES)
         {
            LCD.LCDS_MODE_SET((byte)LCDMode.MODE_VBLANK);

            CPU.RequestInterrupt(InterruptType.IT_VBLANK);

            if (LCD.LCDS_STAT_INT(StatSrc.SS_VBLANK) != 0)
            {
               CPU.RequestInterrupt(InterruptType.IT_LCD_STAT);
            }

            PPU._context.CurrentFrame++;

            // calculate FPS
            UInt32 end = UI.GetTicks();
            UInt32 FrameTime = (uint)(end - PrevFrameTime);

            if (FrameTime < TargetFrameTime)
            {
               Emulator.Delay((uint)(TargetFrameTime - FrameTime));
            }

            if (end - StartTimer >= 1000)
            {
               UInt32 fps = (uint)FrameCount;
               StartTimer = end;
               FrameCount = 0;

               Console.WriteLine($"FPS: {fps}");
            }

            FrameCount++;
            PrevFrameTime = UI.GetTicks();
         }
         else
         {
            LCD.LCDS_MODE_SET((byte)LCDMode.MODE_OAM);
         }

         PPU._context.LineTicks = 0;
      }
   }
}