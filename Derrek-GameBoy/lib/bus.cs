using System;

// 0x0000 - 0x3FFF : ROM Bank 0
// 0x4000 - 0x7FFF : ROM Bank 1 - Switchable
// 0x8000 - 0x97FF : CHR RAM
// 0x9800 - 0x9BFF : BG Map 1
// 0x9C00 - 0x9FFF : BG Map 2
// 0xA000 - 0xBFFF : Cartridge RAM
// 0xC000 - 0xCFFF : RAM Bank 0
// 0xD000 - 0xDFFF : RAM Bank 1-7 - switchable - Color only
// 0xE000 - 0xFDFF : Reserved - Echo RAM
// 0xFE00 - 0xFE9F : Object Attribute Memory
// 0xFEA0 - 0xFEFF : Reserved - Unusable
// 0xFF00 - 0xFF7F : I/O Registers
// 0xFF80 - 0xFFFE : Zero Page
public static class Bus
{
   public static byte BusRead(UInt16 address)
   {
      if (address < 0x8000)
      {
         // Rom data
         return Cart.CartRead(address);
      } 
      else if (address < 0xA000)
      {
         // Char/map data
         return PPU.VRamRead(address);
      }
      else if (address < 0xC000)
      {
         // Cartridge RAM
         return Cart.CartRead(address);
      }
      else if (address < 0xE000)
      {
         // WRAM (Working RAM)
         return RAM.WRamRead(address);
      }
      else if (address < 0xFE00)
      {
         // Reserved Echo RAM
         return 0;
      }
      else if (address < 0xFEA0)
      {
         // 0AM
         return PPU.OAMRead(address);
      }
      else if (address < 0xFF00)
      {
         // Reserved Unusable
         return 0;
      }
      else if (address < 0xFF80)
      {
         // I/O registers
         // TODO
         return IO.IORead(address);
      }
      else if (address == 0xFFFF)
      {
         // CPU Enable register
         // TODO
         return CPU._context.ieRegister;
      }

      return RAM.HRamRead(address);
   }

   public static void BusWrite(UInt16 address, byte value)
   {
      if (address < 0x8000)
      {
         // ROM data
         Cart.CartWrite(address, value);
      }
      else if (address < 0xA000)
      {
         // Char/map data
         PPU.VRamWrite(address, value);
      }
      else if (address < 0xC000)
      {
         // Cartridge RAM
         Cart.CartWrite(address, value);
      }
      else if (address < 0xE000)
      {
         // WRAM
         RAM.WRamWrite(address, value);
      }
      else if (address < 0xFE00)
      {
         // Reserved echo RAM
      }
      else if (address < 0xFEA0)
      {
         // OAM
         PPU.OAMWrite(address, value);
      }
      else if (address < 0xFF00)
      {
         // Unsuable reserved
      }
      else if (address < 0xFF80)
      {
         // I/O Registers
         // TODO
         IO.IOWrite(address, value);
      }
      else if (address == 0xFFFF)
      {
         // CPU SET ENABLE REGISTER
         // TODO
         CPU._context.ieRegister = value;
      }
      else
      {
         // HRAM
         RAM.HRamWrite(address, value);
      }
   }

   public static UInt16 BusRead16(UInt16 address) 
   {
      UInt16 lo = Bus.BusRead(address);
      UInt16 hi = Bus.BusRead((UInt16)(address + 1));

      return (UInt16)(lo | (hi << 8));
   }

   public static void BusWrite16(UInt16 address, UInt16 value)
   {
      BusWrite((UInt16)(address + 1), (byte)((value >> 8) & 0xFF));
      BusWrite(address, (byte)(value & 0xFF));
   }
}