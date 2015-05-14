// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Globalization;
using MICore;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.MIDebugEngine
{
    public static class EngineUtils
    {
        internal static string AsAddr(ulong addr)
        {
            string addrFormat = DebuggedProcess.g_Process.Is64BitArch ? "x16" : "x8";
            return "0x" + addr.ToString(addrFormat, CultureInfo.InvariantCulture);
        }

        internal static string GetAddressDescription(DebuggedProcess proc, ulong ip)
        {
            string description = null;
            proc.WorkerThread.RunOperation(async () =>
            {
                description = await EngineUtils.GetAddressDescriptionAsync(proc, ip);
            }
            );

            return description;
        }

        internal static async Task<string> GetAddressDescriptionAsync(DebuggedProcess proc, ulong ip)
        {
            string location = null;
            IEnumerable<DisasmInstruction> instructions = await proc.Disassembly.FetchInstructions(ip, 1);
            if (instructions != null)
            {
                foreach (DisasmInstruction instruction in instructions)
                {
                    if (location == null && !String.IsNullOrEmpty(instruction.Symbol))
                    {
                        location = instruction.Symbol;
                        break;
                    }
                }
            }

            if (location == null)
            {
                string addrFormat = DebuggedProcess.g_Process.Is64BitArch ? "x16" : "x8";
                location = ip.ToString(addrFormat, CultureInfo.InvariantCulture);
            }

            return location;
        }


        public static void CheckOk(int hr)
        {
            if (hr != 0)
            {
                throw new MIException(hr);
            }
        }

        public static void RequireOk(int hr)
        {
            if (hr != 0)
            {
                throw new InvalidOperationException();
            }
        }

        public static AD_PROCESS_ID GetProcessId(IDebugProcess2 process)
        {
            AD_PROCESS_ID[] pid = new AD_PROCESS_ID[1];
            EngineUtils.RequireOk(process.GetPhysicalProcessId(pid));
            return pid[0];
        }

        public static AD_PROCESS_ID GetProcessId(IDebugProgram2 program)
        {
            IDebugProcess2 process;
            RequireOk(program.GetProcess(out process));

            return GetProcessId(process);
        }

        public static int UnexpectedException(Exception e)
        {
            Debug.Fail("Unexpected exception during Attach");
            return Constants.RPC_E_SERVERFAULT;
        }

        internal static bool IsFlagSet(uint value, int flagValue)
        {
            return (value & flagValue) != 0;
        }

        internal static bool ProcIdEquals(AD_PROCESS_ID pid1, AD_PROCESS_ID pid2)
        {
            if (pid1.ProcessIdType != pid2.ProcessIdType)
            {
                return false;
            }
            else if (pid1.ProcessIdType == (int)enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM)
            {
                return pid1.dwProcessId == pid2.dwProcessId;
            }
            else
            {
                return pid1.guidProcessId == pid2.guidProcessId;
            }
        }

        internal static string GetExceptionDescription(Exception exception)
        {
            if (!IsCorruptingException(exception))
            {
                return exception.Message;
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_CorruptingException, exception.GetType().FullName, exception.StackTrace);
            }
        }

        private static bool IsCorruptingException(Exception exception)
        {
            if (exception is SystemException)
            {
                if (exception is NullReferenceException)
                    return true;
                if (exception is AccessViolationException)
                    return true;
                if (exception is ArgumentNullException)
                    return true;
                if (exception is ArithmeticException)
                    return true;
                if (exception is ArrayTypeMismatchException)
                    return true;
                if (exception is DivideByZeroException)
                    return true;
                if (exception is IndexOutOfRangeException)
                    return true;
                if (exception is InvalidCastException)
                    return true;
                if (exception is StackOverflowException)
                    return true;
                if (exception is SEHException)
                    return true;
            }

            return false;
        }

        internal class SignalMap : Dictionary<string, uint>
        {
            private static SignalMap s_instance;
            private SignalMap()
            {
                this["SIGHUP"] = 1;
                this["SIGINT"] = 2;
                this["SIGQUIT"] = 3;
                this["SIGILL"] = 4;
                this["SIGTRAP"] = 5;
                this["SIGABRT"] = 6;
                this["SIGIOT"] = 6;
                this["SIGBUS"] = 7;
                this["SIGFPE"] = 8;
                this["SIGKILL"] = 9;
                this["SIGUSR1"] = 10;
                this["SIGSEGV"] = 11;
                this["SIGUSR2"] = 12;
                this["SIGPIPE"] = 13;
                this["SIGALRM"] = 14;
                this["SIGTERM"] = 15;
                this["SIGSTKFLT"] = 16;
                this["SIGCHLD"] = 17;
                this["SIGCONT"] = 18;
                this["SIGSTOP"] = 19;
                this["SIGTSTP"] = 20;
                this["SIGTTIN"] = 21;
                this["SIGTTOU"] = 22;
                this["SIGURG"] = 23;
                this["SIGXCPU"] = 24;
                this["SIGXFSZ"] = 25;
                this["SIGVTALRM"] = 26;
                this["SIGPROF"] = 27;
                this["SIGWINCH"] = 28;
                this["SIGIO"] = 29;
                this["SIGPOLL"] = 29;
                this["SIGPWR"] = 30;
                this["SIGSYS"] = 31;
                this["SIGUNUSED"] = 31;
            }
            public static SignalMap Instance
            {
                get
                {
                    if (s_instance == null)
                    {
                        s_instance = new SignalMap();
                    }
                    return s_instance;
                }
            }
        }
    }
}
