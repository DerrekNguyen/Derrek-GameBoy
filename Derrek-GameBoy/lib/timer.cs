using Newtonsoft.Json.Linq;
using System;

public class TimerContext
{
   public UInt16 div = 0;
   public byte tima = 0;
   public byte tma = 0;
   public byte tac = 0;
}

public static class Timer
{
   public static TimerContext _context = new TimerContext();
   
   public static void Init()
   {
      _context.div = 0xAC00;
   }

   public static void Tick()
   {
      UInt16 prevDiv = _context.div;

      _context.div++;

      bool timerUpdate = false;

      switch (_context.tac & (0b11))
      {
         case 0b00:
            timerUpdate = (prevDiv & (1 << 9)) != 0 && ((_context.div & (1 << 9)) == 0);
            break;
         case 0b01:
            timerUpdate = (prevDiv & (1 << 3)) != 0 && ((_context.div & (1 << 3)) == 0);
            break;
         case 0b10:
            timerUpdate = (prevDiv & (1 << 5)) != 0 && ((_context.div & (1 << 5)) == 0);
            break;
         case 0b11:
            timerUpdate = (prevDiv & (1 << 7)) != 0 && ((_context.div & (1 << 7)) == 0);
            break;
      }

      if (timerUpdate && (_context.tac & (1 << 2)) != 0)
      {
         _context.tima++;

         if (_context.tima == 0xFF)
         {
            _context.tima = _context.tma;

            CPU.RequestInterrupt(InterruptType.IT_TIMER);
         }
      }
   }

   public static void Write(UInt16 address, byte value)
   {
      switch (address)
      {
         // DIV
         case 0xFF04:
            _context.div = 0;
            break;

         // TIMA
         case 0xFF05:
            _context.tima = value;
            break;

         // TMA
         case 0xFF06:
            _context.tma = value;
            break;

         // TAC
         case 0xFF07:
            _context.tac = value;
            break;
      }
   }

   public static byte Read(UInt16 address)
   {
      switch (address)
      {
         // DIV
         case 0xFF04:
            return (byte)(_context.div >> 8);

         // TIMA
         case 0xFF05:
            return (_context.tima);

         // TMA
         case 0xFF06:
            return (_context.tma);

         // TAC
         case 0xFF07:
            return (_context.tac);

         default:
            return 0;
      }
   }
}