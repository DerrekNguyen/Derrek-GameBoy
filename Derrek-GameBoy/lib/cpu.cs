using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;

public unsafe class CPUContext
{
   public CPU_Registers regs;

   // Current fetch...
   public UInt16 fetchData;
   public UInt16 memDest;
   public byte curOpcode;

   public Instruction CurrInst;

   public bool destIsMem;
   public bool halted;
   public bool stepping;
}

public static class CPU
{
   public static CPUContext _context = new();
   public static void CPU_Init()
   {
      _context.regs.pc = 0x100;
   }

   public static void Fetch_Instruction()
   {
      _context.curOpcode = Bus.BusRead(_context.regs.pc++);
      _context.CurrInst = Instructions.Instruction_By_Opcode(_context.curOpcode);

      if (_context.CurrInst == null)
      {
         Console.WriteLine($"Unknown Instruction: {_context.curOpcode:X2}");
         Environment.Exit(-7);
         return;
      }
   }

   public static void Fetch_Data()
   {
      _context.memDest = 0;
      _context.destIsMem = false;

      switch (_context.CurrInst.mode)
      {
         case AddrMode.AM_IMP:
            return;

         case AddrMode.AM_R:
            _context.fetchData = CPUUtil.CPUReadReg(_context.CurrInst.reg1);
            return;

         case AddrMode.AM_R_D8:
            _context.fetchData = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);
            _context.regs.pc++;
            return;

         case AddrMode.AM_D16:
            {
               UInt16 lo = Bus.BusRead(_context.regs.pc);
               Emulator.EmuCycle(1);

               UInt16 hi = Bus.BusRead((UInt16)(_context.regs.pc + 1));
               Emulator.EmuCycle(1);

               _context.fetchData = (UInt16)(lo | (hi << 8));

               _context.regs.pc += 2;
               return;
            }

         default:
            Console.WriteLine($"Unknown Address mode: {_context.CurrInst.mode}");
            Environment.Exit(-7);
            return;
      }
   }

   public static void Execute()
   {
      Console.WriteLine($"Executing Instruction: {_context.curOpcode:X2}, PC: {_context.regs.pc:X4}");
      Console.WriteLine("Not Executing yet");
   }

   public static bool CPU_Step()
   {
      if (!_context.halted)
      {
         Fetch_Instruction();
         Fetch_Data();
         Execute();
      }

      return true;
   }
}