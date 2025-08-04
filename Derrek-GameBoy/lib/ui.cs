using SDL2;
using System;

public static class UI
{
   private static int SCREEN_WIDTH = 1024;
   private static int SCREEN_HEIGHT = 768;

   public static IntPtr sdlWindow;
   public static IntPtr sdlRenderer;
   public static IntPtr sdlTexture;
   public static IntPtr screen;

   public static void UIInit()
   {
      SDL2.SDL.SDL_Init(SDL2.SDL.SDL_INIT_VIDEO);
      SDL2.SDL_ttf.TTF_Init();

      SDL2.SDL.SDL_CreateWindowAndRenderer(SCREEN_WIDTH, SCREEN_HEIGHT, 0, out sdlWindow, out sdlRenderer);
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