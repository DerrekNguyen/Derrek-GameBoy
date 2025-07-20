using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;

public unsafe class CPUContext
{
   public CPU_Registers regs;

   // Current fetch...
   public UInt16 fetchedData;
   public UInt16 memDest;
   public byte curOpcode;

   public Instruction CurrInst;

   public bool destIsMem;
   public bool halted;
   public bool stepping;

   public bool intMasterEnabled;
}

public static class CPU
{
   public static CPUContext _context = new();

   public static bool CPU_FLAG_Z = Common.BIT(_context.regs.f, 7);

   public static bool CPU_FLAG_C = Common.BIT(_context.regs.f, 4);

   public static void CPU_Init()
   {
      _context.regs.pc = 0x100;
      _context.regs.a = 0x01;
   }

   public static void Fetch_Instruction()
   {
      _context.curOpcode = Bus.BusRead(_context.regs.pc++);
      _context.CurrInst = Instructions.Instruction_By_Opcode(_context.curOpcode);
   }

   public static void Fetch_Data()
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

         case AddrMode.AM_R_D8:
            _context.fetchedData = Bus.BusRead(_context.regs.pc);
            Emulator.EmuCycle(1);
            _context.regs.pc++;
            return;

         case AddrMode.AM_D16:
            {
               UInt16 lo = Bus.BusRead(_context.regs.pc);
               Emulator.EmuCycle(1);

               UInt16 hi = Bus.BusRead((UInt16)(_context.regs.pc + 1));
               Emulator.EmuCycle(1);

               _context.fetchedData = (UInt16)(lo | (hi << 8));

               _context.regs.pc += 2;
               return;
            }

         default:
            Console.WriteLine($"Unknown Address mode: {_context.CurrInst.mode} ({_context.curOpcode:X2})");
            Environment.Exit(-7);
            return;
      }
   }

   public static void Execute()
   {
      IN_PROC proc = CPUProc.InstGetProcessor(_context.CurrInst.type);

      if ( proc == null )
      {
         Common.NO_IMPL();
      }

      proc(_context);
   }

   public static bool CPU_Step()
   {
      if (!_context.halted)
      {
         UInt16 pc = _context.regs.pc;

         Fetch_Instruction();
         Fetch_Data();

         Console.WriteLine($"{pc:X4}: {InstLookUp.InstName(_context.CurrInst.type), 7} " +
            $"({_context.curOpcode:X2} {Bus.BusRead((ushort)(pc + 1)):X2} {Bus.BusRead((ushort)(pc + 2)):X2}) " +
            $"A: {_context.regs.a:X2} B: {_context.regs.b:X2} C: {_context.regs.c:X2}");

         if (_context.CurrInst == null)
         {
            Console.WriteLine($"Unknown Instruction: {_context.curOpcode:X2}");
            Environment.Exit(-7);
            return false;
         }

         Execute();
      }

      return true;
   }
}