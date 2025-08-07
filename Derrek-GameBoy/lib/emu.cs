using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using SDL2;

public class EmuContext
{
   public bool Paused;
   public bool Running;
   public bool Die = false;
   public ulong Ticks;
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

   private static Thread cpuThread;
   public static EmuContext GetContext() => _context;

   public static void Delay(uint ms)
   {
      System.Threading.Thread.Sleep((int)ms);
   }

   public static void CPURun()
   {
      Timer.Init();
      CPU.Init();

      _context.Paused = false;
      _context.Running = true;
      _context.Ticks = 0;

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
            return;
         }
      }
   }

   public static int EmuRun(String[] argv)
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

      UI.UIInit();

      try
      {
         cpuThread = new Thread(CPURun);
         cpuThread.Start();
      }
      catch (ThreadStateException ex)
      {
         Console.WriteLine("Thread already started: " + ex.Message);
      }
      catch (OutOfMemoryException ex)
      {
         Console.WriteLine("Not enough system resources to start thread: " + ex.Message);
      }
      catch (Exception ex)
      {
         Console.WriteLine("Unexpected error starting thread: " + ex.Message);
      }

      while (!_context.Die)
      {
         Thread.Sleep(1);
         UI.UIHandleEvents();
         UI.UIUpdate();
      }

      _context.Running = false;

      if (cpuThread != null && cpuThread.IsAlive)
      {
         cpuThread.Join();
      }

      SDL2.SDL.SDL_DestroyRenderer(UI.sdlRenderer);
      SDL2.SDL.SDL_DestroyWindow(UI.sdlWindow);
      SDL2.SDL_ttf.TTF_Quit();
      SDL2.SDL.SDL_Quit();

      return 1;
   }

   /// <summary>
   /// Pass an amount of CPU cycles to synchronize the PPU.
   /// </summary>
   /// <param name="emu_cycles">number of CPU cycles</param>
   public static void EmuCycle(int CPUCycles) 
   {
      //TODO
      int n = CPUCycles * 4;

      for (int i = 0; i < n; i++)
      {
         _context.Ticks++;
         Timer.Tick();
      }
   }
}