using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
        //
        // The RegisterNameMap maps register names to logical group names. The architecture of 
        // the platform is described with all its varients. Any particular target may only contains a subset 
        // of the available registers.
        public class RegisterNameMap
        {
            private Entry[] m_map;
            private struct Entry
            {
                public readonly string Name;
                public readonly bool IsRegex;
                public readonly string Group;
                public Entry(string name, bool isRegex, string group)
                {
                    Name = name;
                    IsRegex = isRegex;
                    Group = group;
                }
            };

            private static readonly Entry[] Arm32Registers = new Entry[]
            {
                new Entry( "sp", false, "CPU"),
                new Entry( "lr", false, "CPU"),
                new Entry( "pc", false, "CPU"),
                new Entry( "cpsr", false, "CPU"),
                new Entry( "r[0-9]+", true, "CPU"),
                new Entry( "fpscr", false, "FPU"),
                new Entry( "f[0-9]+", true, "FPU"),
                new Entry( "s[0-9]+", true, "IEEE Single"),
                new Entry( "d[0-9]+", true, "IEEE Double"),
                new Entry( "q[0-9]+", true, "Vector"),
            };

            private static readonly Entry[] X86Registers = new Entry[]
            {
                new Entry( "eax", false, "CPU" ),
                new Entry( "ecx", false, "CPU" ),
                new Entry( "edx", false, "CPU" ),
                new Entry( "ebx", false, "CPU" ),
                new Entry( "esp", false, "CPU" ),
                new Entry( "ebp", false, "CPU" ),
                new Entry( "esi", false, "CPU" ),
                new Entry( "edi", false, "CPU" ),
                new Entry( "eip", false, "CPU" ),
                new Entry( "eflags", false, "CPU" ),
                new Entry( "cs", false, "CPU" ),
                new Entry( "ss", false, "CPU" ),
                new Entry( "ds", false, "CPU" ),
                new Entry( "es", false, "CPU" ),
                new Entry( "fs", false, "CPU" ),
                new Entry( "gs", false, "CPU" ),
                new Entry( "st", true, "CPU" ),
                new Entry( "fctrl", false, "CPU" ),
                new Entry( "fstat", false, "CPU" ),
                new Entry( "ftag", false, "CPU" ),
                new Entry( "fiseg", false, "CPU" ),
                new Entry( "fioff", false, "CPU" ),
                new Entry( "foseg", false, "CPU" ),
                new Entry( "fooff", false, "CPU" ),
                new Entry( "fop", false, "CPU" ),
                new Entry( "mxcsr", false, "CPU" ),
                new Entry( "orig_eax", false, "CPU" ),
                new Entry( "al", false, "CPU" ),
                new Entry( "cl", false, "CPU" ),
                new Entry( "dl", false, "CPU" ),
                new Entry( "bl", false, "CPU" ),
                new Entry( "ah", false, "CPU" ),
                new Entry( "ch", false, "CPU" ),
                new Entry( "dh", false, "CPU" ),
                new Entry( "bh", false, "CPU" ),
                new Entry( "ax", false, "CPU" ),
                new Entry( "cx", false, "CPU" ),
                new Entry( "dx", false, "CPU" ),
                new Entry( "bx", false, "CPU" ),
                new Entry( "bp", false, "CPU" ),
                new Entry( "si", false, "CPU" ),
                new Entry( "di", false, "CPU" ),
                new Entry( "mm[0-7]", true, "MMX" ),
                new Entry( "xmm[0-7]ih", true, "SSE2" ),
                new Entry( "xmm[0-7]il", true, "SSE2" ),
                new Entry( "xmm[0-7]dh", true, "SSE2" ),
                new Entry( "xmm[0-7]dl", true, "SSE2" ),
                new Entry( "xmm[0-7][0-7]", true, "SSE" ),
                new Entry( "ymm.+", true, "AVX" ),
                new Entry( "mm[0-7][0-7]", true, "AMD3DNow" ),
            };

            private static readonly Entry[] AllRegisters = new Entry[]
            {
                    new Entry( ".+", true, "CPU"),
            };

            public static RegisterNameMap Create(string[] registerNames)
            {
                // TODO: more robust mechanism for determining processor architecture
                RegisterNameMap map = new RegisterNameMap();
                if (registerNames[0][0] == 'r') // registers are prefixed with 'r', assume ARM and initialize its register sets
                {
                    map.m_map = Arm32Registers;
                }
                else if (registerNames[0][0] == 'e') // x86 register set
                {
                    map.m_map = X86Registers;
                }
                else
                {
                    // report one global register set
                    map.m_map = AllRegisters;
                }
                return map;
            }

            public string GetGroupName(string regName)
            {
                foreach (var e in m_map)
                {
                    if (e.IsRegex)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(regName, e.Name))
                        {
                            return e.Group;
                        }
                    }
                    else if (e.Name == regName)
                    {
                        return e.Group;
                    }
                }
                return "Other Registers"; 
            }
        };
}
