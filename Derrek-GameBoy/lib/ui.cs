using SDL2;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

public static class UI
{
   private static int SCREEN_WIDTH = 1024;
   private static int SCREEN_HEIGHT = 768;

   public static IntPtr sdlWindow;
   public static IntPtr sdlRenderer;
   public static IntPtr sdlTexture;
   public static IntPtr screen;

   public static IntPtr sdlDebugWindow;
   public static IntPtr sdlDebugRenderer;
   public static IntPtr sdlDebugTexture;
   public static IntPtr debugScreen;

   private static int scale = 4;

   public static void UIInit()
   {
      SDL2.SDL.SDL_Init(SDL2.SDL.SDL_INIT_VIDEO);
      SDL2.SDL_ttf.TTF_Init();

      SDL2.SDL.SDL_CreateWindowAndRenderer(SCREEN_WIDTH, SCREEN_HEIGHT, 0, out sdlWindow, out sdlRenderer);

      SDL2.SDL.SDL_CreateWindowAndRenderer(16 * 8 * scale, 32 * 8 * scale, 0, out sdlDebugWindow, out sdlDebugRenderer);

      debugScreen = SDL2.SDL.SDL_CreateRGBSurface(
         0,
         (16 * 8 * scale) + (16 * scale),
         (32 * 8 * scale) + (64 * scale),
         32,
         0x00FF0000,
         0x0000FF00,
         0x000000FF,
         0xFF000000);

      sdlDebugTexture = SDL2.SDL.SDL_CreateTexture(
         sdlDebugRenderer,
         SDL2.SDL.SDL_PIXELFORMAT_ARGB8888,
         (int)SDL2.SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
         (16 * 8 * scale) + (16 * scale),
         (32 * 8 * scale) + (64 * scale)
      );

      int x, y;
      SDL2.SDL.SDL_GetWindowPosition(sdlWindow, out x, out y);
      SDL2.SDL.SDL_SetWindowPosition(sdlDebugWindow, x + SCREEN_WIDTH + 10, y);
   }

   private static ulong[] tileColors = {0xFFFFFFFF, 0xFFAAAAAA, 0xFF555555, 0xFF000000};

   public static UInt32 GetTicks()
   {
      return SDL2.SDL.SDL_GetTicks();
   }

   public static void DisplayTile(
      nint surface, 
      UInt16 startLocation, 
      UInt16 tileNum, 
      int x, 
      int y)
   {
      SDL2.SDL.SDL_Rect rc;

      for (int tileY = 0; tileY < 16; tileY+= 2)
      {
         byte b1 = Bus.BusRead((ushort)(startLocation + (tileNum * 16) + tileY));
         byte b2 = Bus.BusRead((ushort)(startLocation + (tileNum * 16) + tileY + 1));

         for (int bit = 7; bit >= 0; bit--)
         {
            byte hi = (byte)(((b1 >> bit) & 0x1) << 1);
            byte lo = (byte)((b2 >> bit) & 0x1);

            byte color = (byte)(hi | lo);

            rc.x = x + ((7 - bit) * scale);
            rc.y = y + (tileY / 2 * scale);
            rc.w = scale;
            rc.h = scale;

            SDL2.SDL.SDL_FillRect(surface, ref rc, (uint)tileColors[color]);
         }
      }
   }

   private static void UpdateDebugWindow()
   {
      int xDraw = 0;
      int yDraw = 0;
      int tileNum = 0;
      ushort addr = 0x8000;

      for (int y = 0; y < 24; y++)
      {
         for (int x = 0; x < 16; x++)
         {
            DisplayTile(debugScreen, addr, (ushort)tileNum, xDraw, yDraw);
            xDraw += (8 * scale);
            tileNum++;
         }

         yDraw += (8 * scale);
         xDraw = 0;
      }

      // Blit the surface to the screen and update the window
      SDL2.SDL.SDL_BlitSurface(debugScreen, IntPtr.Zero, SDL2.SDL.SDL_GetWindowSurface(sdlDebugWindow), IntPtr.Zero);
      SDL2.SDL.SDL_UpdateWindowSurface(sdlDebugWindow);
   }


   public static void UIUpdate()
   {
      UpdateDebugWindow();
   }

   public static void UIHandleEvents()
   {
      SDL2.SDL.SDL_Event e;
      while (SDL2.SDL.SDL_PollEvent(out e) > 0)
      {
         //TODO: SDL2.SDL.SDL_UpdateWindowSurface(sdlWindow);
         //TODO: SDL2.SDL.SDL_UpdateWindowSurface(sdlTraceWindow);
         //TODO: SDL2.SDL.SDL_UpdateWindowSurface(sdlDebugWindow);

         if (e.type == SDL2.SDL.SDL_EventType.SDL_WINDOWEVENT)
         {
            switch (e.window.windowEvent)
            {
               case SDL2.SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                  Emulator.GetContext().Die = true;
                  break;
            }
         }
      }
   }
}