using System;
using System.Reflection.Metadata.Ecma335;

public static class CPUFetch
{
   public static void Fetch_Data(CPUContext _context)
   {
      _context.memDest = 0;
      _context.destIsMem = false;

      if (_context.CurrInst == null)
      {
         return;
      }

      switch (_context.CurrInst.mode)
      {
         case AddrMode.AM_IMP:
            return;

         case AddrMode.AM_R:
            _context.fetchedData = CPUUtil.CPUReadReg(_context.CurrInst.reg1);
            return;

         case AddrMode.AM_R_R:
            _context.fetchedData = CPUUtil.CPUReadReg(_context.CurrInst.reg2);
            return;

         case AddrMode.AM_R_D8:
            _context.fetchedData = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);
            _context.regs.pc++;
            return;

         case AddrMode.AM_R_D16:
         case AddrMode.AM_D16:
            UInt16 low1 = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);

            UInt16 high1 = Bus.BusRead((UInt16)(_context.regs.pc + 1));
            Emulator.EmuCycle(1);

            _context.fetchedData = (UInt16)(low1 | (high1 << 8));

            _context.regs.pc += 2;
            return;

         case AddrMode.AM_MR_R:
            _context.fetchedData = CPUUtil.CPUReadReg(_context.CurrInst.reg2);
            _context.memDest = CPUUtil.CPUReadReg(_context.CurrInst.reg1);
            _context.destIsMem = true;

            if (_context.CurrInst.reg1 == RegType.RT_C)
            {
               _context.memDest |= 0xFF00;
            }
            return;

         case AddrMode.AM_R_MR:
            UInt16 addr1 = CPUUtil.CPUReadReg(_context.CurrInst.reg2);

            if (_context.CurrInst.reg1 == RegType.RT_C)
            {
               addr1 |= 0xFF00;
            }

            _context.fetchedData = Bus.BusRead(addr1);
            Emulator.EmuCycle(1);
            return;

         case AddrMode.AM_R_HLI:
            _context.fetchedData = CPUUtil.CPUReadReg(_context.CurrInst.reg2);
            Emulator.EmuCycle(1);
            CPUUtil.CPUSetReg(RegType.RT_HL, (ushort)(CPUUtil.CPUReadReg(RegType.RT_HL) + 1));
            return;

         case AddrMode.AM_R_HLD:
            _context.fetchedData = CPUUtil.CPUReadReg(_context.CurrInst.reg2);
            Emulator.EmuCycle(1);
            CPUUtil.CPUSetReg(RegType.RT_HL, (ushort)(CPUUtil.CPUReadReg(RegType.RT_HL) - 1));
            return;

         case AddrMode.AM_HLI_R:
            _context.fetchedData = CPUUtil.CPUReadReg(_context.CurrInst.reg2);
            _context.memDest = CPUUtil.CPUReadReg(_context.CurrInst.reg1);
            _context.destIsMem = true;
            CPUUtil.CPUSetReg(RegType.RT_HL, (ushort)(CPUUtil.CPUReadReg(RegType.RT_HL) + 1));
            return;

         case AddrMode.AM_HLD_R:
            _context.fetchedData = CPUUtil.CPUReadReg(_context.CurrInst.reg2);
            _context.memDest = CPUUtil.CPUReadReg(_context.CurrInst.reg1);
            _context.destIsMem = true;
            CPUUtil.CPUSetReg(RegType.RT_HL, (ushort)(CPUUtil.CPUReadReg(RegType.RT_HL) - 1));
            return;

         case AddrMode.AM_R_A8:
            _context.fetchedData = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);
            _context.regs.pc++;
            return;

         case AddrMode.AM_A8_R:
            _context.memDest = (ushort)(Bus.BusRead(_context.regs.pc) | 0xFF00);
            _context.destIsMem = true;
            Emulator.EmuCycle(1);
            _context.regs.pc++;
            return;

         case AddrMode.AM_HL_SPR:
            _context.fetchedData = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);
            _context.regs.pc++;
            return;

         case AddrMode.AM_D8:
            _context.fetchedData = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);
            _context.regs.pc++;
            return;

         case AddrMode.AM_A16_R:
         case AddrMode.AM_D16_R:
            UInt16 low2 = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);

            UInt16 high2 = Bus.BusRead((UInt16)(_context.regs.pc + 1));
            Emulator.EmuCycle(1);

            _context.memDest = (ushort)(low2 | (high2 << 8));
            _context.destIsMem = true;

            _context.regs.pc += 2;
            _context.fetchedData = CPUUtil.CPUReadReg(_context.CurrInst.reg2);
            return;

         case AddrMode.AM_MR_D8:
            _context.fetchedData = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);
            _context.regs.pc++;
            _context.memDest = CPUUtil.CPUReadReg(_context.CurrInst.reg1);
            _context.destIsMem = true;
            return;

         case AddrMode.AM_MR:
            _context.memDest = CPUUtil.CPUReadReg(_context.CurrInst.reg1);
            _context.destIsMem = true;
            _context.fetchedData = Bus.BusRead(CPUUtil.CPUReadReg(_context.CurrInst.reg1));
            Emulator.EmuCycle(1);
            return;

         case AddrMode.AM_R_A16:
            UInt16 low3 = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);

            UInt16 high3 = Bus.BusRead((UInt16)(_context.regs.pc + 1));
            Emulator.EmuCycle(1);

            UInt16 addr2 = (ushort)(low3 | (high3 << 8));
            _context.fetchedData = Bus.BusRead(addr2);
            Emulator.EmuCycle(1);

            _context.regs.pc += 2;

            return;


         default:
            Console.WriteLine($"Unknown Address mode: {_context.CurrInst.mode} ({_context.curOpcode:X2})");
            Environment.Exit(-7);
            return;
      }
   }
}