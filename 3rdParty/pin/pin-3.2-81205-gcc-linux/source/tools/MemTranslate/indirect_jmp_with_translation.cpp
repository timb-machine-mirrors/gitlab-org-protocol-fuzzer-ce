/*BEGIN_LEGAL 
Intel Open Source License 

Copyright (c) 2002-2016 Intel Corporation. All rights reserved.
 
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.  Redistributions
in binary form must reproduce the above copyright notice, this list of
conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.  Neither the name of
the Intel Corporation nor the names of its contributors may be used to
endorse or promote products derived from this software without
specific prior written permission.
 
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE INTEL OR
ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
END_LEGAL */
#include <iostream>
#include <fstream>
#include <set>
#include "pin.H"

ofstream OutFile;
ADDRINT newBranchTarget = 0xbeef;
REG rewrite_reg;
std::set<ADDRINT> ipointsCallbacks;
KNOB<string> KnobOutputFile(KNOB_MODE_WRITEONCE, "pintool",
    "o", "indirect_jmp_translation.out", "specify output file name");

IPOINT allIpoints[] = {IPOINT_TAKEN_BRANCH, IPOINT_BEFORE};

VOID PIN_FAST_ANALYSIS_CALL IndirectJumpOrCall(ADDRINT ipoint, ADDRINT pc, ADDRINT isMemoryIndirect, ADDRINT translatedAddr)
{
    if (isMemoryIndirect && translatedAddr == newBranchTarget)
    {
        std::pair<std::set<ADDRINT>::iterator,bool> res = ipointsCallbacks.insert(ipoint);
        if (!res.second)
        {
            // We expect all indirect branches through memory to go to the address at 'newBranchTarget'
            ASSERT(translatedAddr == newBranchTarget, "At PC=" + hexstr(pc) +
                "Too many indirect branches at IPOINT " + decstr(ipoint) +
                " was reported to jump to " + hexstr(newBranchTarget) + "\n");
        }
    }
}


/* Translate memory address 0xfed to the address of newBranchTarget */
ADDRINT PIN_FAST_ANALYSIS_CALL memoryCallback(PIN_MEM_TRANS_INFO* memTransInfo, VOID *v) 
{
    if (memTransInfo->memOpType == PIN_MEMOP_LOAD && memTransInfo->addr == 0xfed)
    {
        return (ADDRINT)&newBranchTarget;
    }

    return (memTransInfo->addr);
}

static ADDRINT TranslateJmpMemRef(ADDRINT ea, ADDRINT stack_ptr)
{
    if (ea == 0xfed)
    {
        // This is where we translate the bad address of
        // "jmp *(0xfed)" to "jmp *(rsp/esp)"
        return stack_ptr;
    }

    return ea;
}

// Pin calls this function every time a new instruction is encountered
VOID Instruction(INS ins, VOID *v)
{
    if (INS_IsIndirectBranchOrCall(ins))
    {
        OutFile << "Instrumenting at " << hex << INS_Address(ins) << " " << INS_Disassemble(ins).c_str() << std::endl;
        BOOL explicitMemRef = INS_HasExplicitMemoryReference(ins);
        if (explicitMemRef)
        {
            INS_InsertCall(ins, IPOINT_BEFORE, (AFUNPTR)TranslateJmpMemRef,
                IARG_MEMORYOP_EA, 0,
                IARG_REG_VALUE, REG_STACK_PTR,
                IARG_RETURN_REGS, rewrite_reg,
                IARG_END);
            INS_RewriteMemoryOperand(ins, 0, rewrite_reg);
        }

        for (size_t i = 0; i < sizeof(allIpoints)/sizeof(allIpoints[0]); i++)
        {
            INS_InsertCall(ins,
                    allIpoints[i],
                    (AFUNPTR)IndirectJumpOrCall,
                    IARG_FAST_ANALYSIS_CALL,
                    IARG_ADDRINT, (ADDRINT)allIpoints[i],
                    IARG_INST_PTR,
                    IARG_ADDRINT, (ADDRINT)explicitMemRef,
                    IARG_BRANCH_TARGET_ADDR,
                    IARG_END);
        }
    }
}


// This function is called when the application exits
VOID Fini(INT32 code, VOID *v)
{
    for (size_t i = 0; i < sizeof(allIpoints)/sizeof(allIpoints[0]); i++)
    {
        ASSERT(ipointsCallbacks.count(allIpoints[i]) == 1, "Encoutered no translated branches for IPOINT " + decstr((int)allIpoints[i]));
    }
    // Write to a file since cout and cerr maybe closed by the application
    OutFile.setf(ios::showbase);
    OutFile << "Done!" << endl;
    OutFile.close();
}

/* ===================================================================== */
/* Print Help Message                                                    */
/* ===================================================================== */

INT32 Usage()
{
    cerr << "This tool tests memory address translation for indirect call" << endl;
    cerr << endl << KNOB_BASE::StringKnobSummary() << endl;
    return -1;
}

/* ===================================================================== */
/* Main                                                                  */
/* ===================================================================== */

int main(int argc, char * argv[])
{
    // Initialize pin
    if (PIN_Init(argc, argv)) return Usage();

    OutFile.open(KnobOutputFile.Value().c_str());

    rewrite_reg = PIN_ClaimToolRegister();

    // Register Instruction to be called to instrument instructions
    INS_AddInstrumentFunction(Instruction, 0);

    // Register memory callback
    PIN_AddMemoryAddressTransFunction(memoryCallback, NULL);

    // Register Fini to be called when the application exits
    PIN_AddFiniFunction(Fini, 0);

    // Start the program, never returns
    PIN_StartProgram();
}
