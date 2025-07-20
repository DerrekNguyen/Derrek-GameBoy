using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public enum AddrMode
{
   AM_IMP,
   AM_R_D16,
   AM_R_R,
   AM_MR_R,
   AM_R,
   AM_R_D8,
   AM_R_MR,
   AM_R_HLI,
   AM_R_HLD,
   AM_HLI_R,
   AM_HLD_R,
   AM_R_A8,
   AM_A8_R,
   AM_HL_SPR,
   AM_D16,
   AM_D8,
   AM_D16_R,
   AM_MR_D8,
   AM_MR,
   AM_A16_R,
   AM_R_A16,
}

public enum RegType
{
   RT_NONE,
   RT_A,
   RT_F,
   RT_B,
   RT_C,
   RT_D,
   RT_E,
   RT_H,
   RT_L,
   RT_AF,
   RT_BC,
   RT_DE,
   RT_HL,
   RT_SP,
   RT_PC,
}

public enum InType
{
   IN_NONE,
   IN_NOP,
   IN_LD,
   IN_INC,
   IN_DEC,
   IN_RLCA,
   IN_ADD,
   IN_RRCA,
   IN_STOP,
   IN_RLA,
   IN_JR,
   IN_RRA,
   IN_DAA,
   IN_CPL,
   IN_SCF,
   IN_CCF,
   IN_HALT,
   IN_ADC,
   IN_SUB,
   IN_SBC,
   IN_AND,
   IN_XOR,
   IN_OR,
   IN_CP,
   IN_POP,
   IN_JP,
   IN_PUSH,
   IN_RET,
   IN_CB,
   IN_CALL,
   IN_RETI,
   IN_LDH,
   IN_JPHL,
   IN_DI,
   IN_EI,
   IN_RST,
   IN_ERR,

   //CB instructions...
   IN_RLC,
   IN_RRC,
   IN_RL,
   IN_RR,
   IN_SLA,
   IN_SRA,
   IN_SWAP,
   IN_SRL,
   IN_BIT,
   IN_RES,
   IN_SET,
}

public struct CPU_Registers
{
   public byte a;
   public byte f;
   public byte b;
   public byte c;
   public byte d;
   public byte e;
   public byte h;
   public byte l;
   public UInt16 pc;
   public UInt16 sp;
}

public enum CondType
{
   CT_NONE, CT_NZ, CT_Z, CT_NC, CT_C
}

public static class InstLookUp
{
   public static readonly string[] table = new string[]
   {
      "<NONE>",
      "NOP",
      "LD",
      "INC",
      "DEC",
      "RLCA",
      "ADD",
      "RRCA",
      "STOP",
      "RLA",
      "JR",
      "RRA",
      "DAA",
      "CPL",
      "SCF",
      "CCF",
      "HALT",
      "ADC",
      "SUB",
      "SBC",
      "AND",
      "XOR",
      "OR",
      "CP",
      "POP",
      "JP",
      "PUSH",
      "RET",
      "CB",
      "CALL",
      "RETI",
      "LDH",
      "JPHL",
      "DI",
      "EI",
      "RST",
      "IN_ERR",
      "IN_RLC",
      "IN_RRC",
      "IN_RL",
      "IN_RR",
      "IN_SLA",
      "IN_SRA",
      "IN_SWAP",
      "IN_SRL",
      "IN_BIT",
      "IN_RES",
      "IN_SET"
   };

   public static string InstName(InType t)
   {
      int index = (int)t;

      if (index < 0 || index > table.Length)
      {
         return "<INVALID>";
      }

      return table[index];
   }
}

public class Instruction
{
   public InType type;
   public AddrMode mode;
   public RegType reg1;
   public RegType reg2;
   public CondType cond;
   public byte param;

   public Instruction(InType inType = InType.IN_NONE, AddrMode addrMode = AddrMode.AM_IMP, RegType regType = RegType.RT_NONE)
   {
      type = inType;
      mode = addrMode;
      reg1 = regType;
   }
}

public static class Instructions
{
   public static Instruction[] _instructions = new Instruction[0x100];

   static Instructions()
   {
      _instructions[0x00] = new Instruction(InType.IN_NOP, AddrMode.AM_IMP);
      _instructions[0x05] = new Instruction(InType.IN_DEC, AddrMode.AM_R, RegType.RT_B);
      _instructions[0x0E] = new Instruction(InType.IN_LD, AddrMode.AM_R_D8, RegType.RT_C);
      _instructions[0xAF] = new Instruction(InType.IN_XOR, AddrMode.AM_R, RegType.RT_A);
      _instructions[0xC3] = new Instruction(InType.IN_JP, AddrMode.AM_D16);
      _instructions[0xF3] = new Instruction(InType.IN_DI);
   }

   public static Instruction Instruction_By_Opcode(byte opcode)
   {
      // no need for pointer since returning a class in C# returns by reference.
      // only need it for structs
      if (_instructions[opcode] is not null)
      {
         return _instructions[opcode];
      }
      return new Instruction();
   }
}