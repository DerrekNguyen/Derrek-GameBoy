using System;

public class DMAContext
{
   public bool active;
   public byte curByte;
   public byte value;
   public byte startDelay;
}

public static class DMA
{
   public static DMAContext _context = new DMAContext();

   public static void Start(byte start)
   {
      _context.active = true;
      _context.curByte = 0;
      _context.startDelay = 2;
      _context.value = start;
   }

   public static void Tick()
   {
      if (!_context.active)
      {
         return;
      }
      if (_context.startDelay > 0)
      {
         _context.startDelay--;
         return;
      }

      PPU.OAMWrite(_context.curByte, Bus.BusRead((ushort)((_context.value * 0x100) + _context.curByte)));
   
      _context.curByte++;
      _context.active = (_context.curByte < 0xA0);
   }

   public static bool Transferring()
   {
      return _context.active;
   }
}
