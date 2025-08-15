using System;

public class GamePadState
{
   public bool start;
   public bool select;
   public bool a;
   public bool b;
   public bool up;
   public bool down;
   public bool left;
   public bool right;
}

public class GamePadContext
{
   public bool buttonSel;
   public bool dirSel;

   public GamePadState controller = new GamePadState();
}

public static class GamePad
{
   public static GamePadContext _context = new GamePadContext();

   public static void Init()
   {

   }

   public static bool ButtonSelect()
   {
      return _context.buttonSel;
   }

   public static bool DirSelect()
   {
      return _context.dirSel;
   }

   public static void SetSel(byte value)
   {
      _context.buttonSel = (value & 0x20) != 0 ? true : false;
      _context.dirSel = (value & 0x10) != 0 ? true : false;
   }

   public static byte GetOutput()
   {
      byte output = 0xCF;

      if (!ButtonSelect())
      {
         if (_context.controller.start)
         {
            output &= 0b11110111;
         }
         if (_context.controller.select)
         {
            output &= 0b11111011;
         }
         if (_context.controller.a)
         {
            output &= 0b11111110;
         }
         if (_context.controller.b)
         {
            output &= 0b11111101;
         }
      }

      if (!DirSelect())
      {
         if (_context.controller.left)
         {
            output &= 0b11111101;
         }
         if (_context.controller.right)
         {
            output &= 0b11111110;
         }
         if (_context.controller.up)
         {
            output &= 0b11111011;
         }
         if (_context.controller.down)
         {
            output &= 0b11110111;
         }
      }

      return output;
   }
}
