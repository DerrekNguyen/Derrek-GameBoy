using System;

public static class PPUSM
{
   private static double TargetFrameTime = 1000.0 / 60.0;
   private static long PrevFrameTime = 0;
   private static long StartTimer = 0;
   private static long FrameCount = 0;

   private static void IncrementLy()
   {
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

   public static void PPUModeOAM()
   {
      if (PPU._context.LineTicks >= 80)
      {
         LCD.LCDS_MODE_SET((byte)LCDMode.MODE_XFER);
      }
   }

   public static void PPUModeXFER()
   {
      if (PPU._context.LineTicks >= 80 + 172)
      {
         LCD.LCDS_MODE_SET((byte)LCDMode.MODE_HBLANK);
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