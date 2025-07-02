using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using SDL2;

public class EmuContext
{
   public bool Paused { get; set; }
   public bool Running { get; set; }
   public ulong Ticks { get; set; }
}

/* 
  Emu components:

  |Cart|
  |CPU|
  |Address Bus|
  |PPU|
  |Timer|

*/
public static class Emulator
{
   private static readonly EmuContext _context = new();
   public static EmuContext GetContext() => _context;

   public static void Delay(uint ms)
   {
      System.Threading.Thread.Sleep((int)ms);
   }
   public static int Run(String[] argv)
   {
      if (argv.Length < 1)
      {
         Console.WriteLine("Usage: emu <rom_file>\n");
      }
         
      if (!Cart.CartLoad(argv[0]))
      {
         Console.WriteLine($"Failed to load ROM file: {argv[0]}");
         return -2;
      }

      Console.WriteLine("Cart loaded...\n");

      SDL2.SDL.SDL_Init(SDL2.SDL.SDL_INIT_VIDEO);
      Console.WriteLine("SDL INIT");

      SDL2.SDL_ttf.TTF_Init();   
      Console.WriteLine("TTF INIT");

      _context.Paused = false;
      _context.Running = true;
      _context.Ticks = 0;

      CPU.CPU_Init();

      while (_context.Running)
      {
         if (_context.Paused)
         {
            Delay(10);
            continue;
         }


         if (!CPU.CPU_Step())
         {
            Console.WriteLine("CPU Stopped");
            return -3;
         }

         _context.Ticks++;
      }

      return 1;
   }
}