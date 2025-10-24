using SDL2;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class UI
{
   private static int SCREEN_WIDTH = 716;
   private static int SCREEN_HEIGHT = 716;

   public static IntPtr sdlWindow;
   public static IntPtr sdlRenderer;
   public static IntPtr sdlTexture;
   public static IntPtr screen;

   public static IntPtr sdlDebugWindow;
   public static IntPtr sdlDebugRenderer;
   public static IntPtr sdlDebugTexture;
   public static IntPtr debugScreen;

   private static int scale = 4;
   private static int scaleMain = 20;

   public static void UIInit()
   {
      SDL2.SDL.SDL_Init(SDL2.SDL.SDL_INIT_VIDEO);
      SDL2.SDL_ttf.TTF_Init();

      SDL2.SDL.SDL_CreateWindowAndRenderer(SCREEN_WIDTH, SCREEN_HEIGHT, 0, out sdlWindow, out sdlRenderer);
      SDL2.SDL.SDL_SetWindowTitle(sdlWindow, "Derrek's GameBoy");

      screen = SDL2.SDL.SDL_CreateRGBSurface(0, SCREEN_WIDTH, SCREEN_HEIGHT, 32,
                                             0xFF000000,
                                             0x00FF0000,
                                             0x0000FF00,
                                             0x000000FF);

      sdlTexture = SDL2.SDL.SDL_CreateTexture(sdlRenderer,
                                              SDL2.SDL.SDL_PIXELFORMAT_ARGB8888,
                                              (int)SDL2.SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                                              SCREEN_WIDTH, SCREEN_HEIGHT);
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

      SDL2.SDL.SDL_BlitSurface(debugScreen, IntPtr.Zero, SDL2.SDL.SDL_GetWindowSurface(sdlDebugWindow), IntPtr.Zero);
      SDL2.SDL.SDL_UpdateWindowSurface(sdlDebugWindow);
   }


   //public static void UIUpdate()
   //{
   //   SDL2.SDL.SDL_Rect rc;
   //   rc.x = rc.y = 0;
   //   rc.w = rc.h = 2048;

   //   UInt32[] VideoBuffer = PPU._context.VideoBuffer;

   //   for (int lineNum = 0; lineNum < PPU.YRES; lineNum++)
   //   {
   //      for (int x = 0; x < PPU.XRES; x++)
   //      {
   //         rc.x = x * scale;
   //         rc.y = lineNum * scale;
   //         rc.w = scale;
   //         rc.h = scale;

   //         SDL2.SDL.SDL_FillRect(screen, ref rc, VideoBuffer[x + (lineNum * PPU.XRES)]);
   //      }
   //   }

   //   SDL.SDL_Surface s = Marshal.PtrToStructure<SDL.SDL_Surface>(screen);
   //   SDL2.SDL.SDL_UpdateTexture(sdlTexture, IntPtr.Zero, s.pixels, s.pitch);
   //   SDL2.SDL.SDL_RenderClear(sdlRenderer);
   //   SDL2.SDL.SDL_RenderCopy(sdlRenderer, sdlTexture, IntPtr.Zero, IntPtr.Zero);
   //   SDL2.SDL.SDL_RenderPresent(sdlRenderer);

   //   UpdateDebugWindow();
   //}

   public static void UIUpdate()
   {
      // Lock the texture so we can write directly to its pixel buffer
      IntPtr pixels;
      int pitch;
      SDL.SDL_LockTexture(sdlTexture, IntPtr.Zero, out pixels, out pitch);

      // Copy video buffer to the locked texture memory
      unsafe
      {
         // PPU._context.VideoBuffer must already be in the right SDL pixel format
         fixed (uint* src = PPU._context.VideoBuffer)
         {
            byte* dst = (byte*)pixels;
            for (int y = 0; y < PPU.YRES; y++)
            {
               // Copy one row of pixels
               Buffer.MemoryCopy(
                   src + (y * PPU.XRES),
                   dst + (y * pitch),
                   pitch,
                   PPU.XRES * sizeof(uint)
               );
            }
         }
      }

      // Unlock texture
      SDL.SDL_UnlockTexture(sdlTexture);

      // Render
      SDL.SDL_RenderClear(sdlRenderer);
      SDL.SDL_Rect destRect = new SDL.SDL_Rect
      {
         x = 0,
         y = 0,
         w = PPU.XRES * scaleMain,
         h = PPU.YRES * scaleMain
      };
      SDL.SDL_RenderCopy(sdlRenderer, sdlTexture, IntPtr.Zero, ref destRect);
      SDL.SDL_RenderPresent(sdlRenderer);

      UpdateDebugWindow();
   }

   public static void OnKey(bool down, UInt32 keyCode)
   {
      switch (keyCode)
      {
         case (uint)SDL2.SDL.SDL_Keycode.SDLK_z:
            GamePad._context.controller.b = down;
            break;
         case (uint)SDL2.SDL.SDL_Keycode.SDLK_x:
            GamePad._context.controller.a = down;
            break;
         case (uint)SDL2.SDL.SDL_Keycode.SDLK_RETURN:
            GamePad._context.controller.start = down;
            break;
         case (uint)SDL2.SDL.SDL_Keycode.SDLK_TAB:
            GamePad._context.controller.select = down;
            break;
         case (uint)SDL2.SDL.SDL_Keycode.SDLK_UP:
            GamePad._context.controller.up = down;
            break;
         case (uint)SDL2.SDL.SDL_Keycode.SDLK_DOWN:
            GamePad._context.controller.down = down;
            break;
         case (uint)SDL2.SDL.SDL_Keycode.SDLK_LEFT:
            GamePad._context.controller.left = down;
            break;
         case (uint)SDL2.SDL.SDL_Keycode.SDLK_RIGHT:
            GamePad._context.controller.right = down;
            break;
      }
   }

   public static void UIHandleEvents()
   {
      SDL2.SDL.SDL_Event e;
      while (SDL2.SDL.SDL_PollEvent(out e) > 0)
      {
         if (e.type == SDL2.SDL.SDL_EventType.SDL_KEYDOWN)
         {
            UI.OnKey(true, (uint)e.key.keysym.sym);
         }

         if (e.type == SDL2.SDL.SDL_EventType.SDL_KEYUP)
         {
            UI.OnKey(false, (uint)e.key.keysym.sym);
         }

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