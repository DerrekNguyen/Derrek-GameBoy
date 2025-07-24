using System;
public class RAMContext
{
   public byte[] wram = new byte[0x2000];
   public byte[] hram = new byte[0x80];
}

public static class RAM
{
   public static RAMContext _context = new();

   public static byte WRamRead(UInt16 address)
   {
      address -= 0xC000;

      if (address >= 0x2000)
      {
         Console.WriteLine($"INVALID WRAM ADDRESS {(address + 0xC000):X8}");
         Environment.Exit(-1);
      }

      return _context.wram[address];
   }
   
   public static void WRamWrite(UInt16 address, byte value)
   {
      address -= 0xC000;
      _context.wram[address] = value;
   }

   public static byte HRamRead(UInt16 address)
   {
      address -= 0xFF80;

      return _context.hram[address];
   }

   public static void HRamWrite(UInt16 address, byte value)
   {
      address -= 0xFF80;

      _context.hram[address] = value;
   }

}

