/*BEGIN_LEGAL 
Intel Open Source License 

Copyright (c) 2002-2015 Intel Corporation. All rights reserved.
 
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
class APP_THREAD_STATISTICS;
class BUFFER_LIST_STATISTICS;
class OVERALL_STATISTICS
{
  public:
      OVERALL_STATISTICS(){};
      VOID Init()
      {
          _numElementsProcessed = 0;
          _numBuffersFilled = 0;
          _numBuffersProcessedInAppThread = 0;
          _numTimesWaitedForFull = 0;
          _numTimesWaitedForFree = 0;
          _cyclesProcessingBuffer = 0;
		  _cyclesWaitingForFreeBuffer = 0;
          _cyclesWaitingForFullBuffer = 0;
		  _totalCycles = 0;
          if (KnobStatistics)
          {
              _startProgramAtCycle = ReadProcessorCycleCounter();
          }
      }
      VOID AccumulateAppThreadStatistics (APP_THREAD_STATISTICS *statistics, BOOL accumulateFreeStats);
      VOID IncorporateBufferStatistics (BUFFER_LIST_STATISTICS *statistics, BOOL isFull);


      VOID DumpNumBuffersFilled()
      {
          if (!KnobLiteStatistics)
          {
              return;
          }
          _totalCycles = ReadProcessorCycleCounter() - _startProgramAtCycle;
		  printf ("\n\nOVERALL STATISTICS\n");
          printf ("  numElementsProcessed               %14u\n", _numElementsProcessed);
          printf ("  numBuffersFilled                   %14u\n", _numBuffersFilled);
          printf ("  numBuffersProcessedInAppThread     %14u\n", _numBuffersProcessedInAppThread);		  
		  if (KnobStatistics)
		  {
	          _fp = fopen ((KnobStatisticsOutputFile.Value()).c_str(), "a");
			  fprintf (_fp, "\n\nOVERALL STATISTICS\n");
              fprintf (_fp, "  totalElementsProcessed               %14u\n", _numElementsProcessed);
			  fprintf (_fp, "  totalBuffersFilled                   %14u\n", _numBuffersFilled);
              fprintf (_fp, "  totalBuffersProcessedInAppThread     %14u\n", _numBuffersProcessedInAppThread);
		  }
      }

      VOID Dump()
      {

          printf ("  numTimesWaitedForFull              %14s\n", decstr(_numTimesWaitedForFull).c_str());
          printf ("  numTimesWaitedForFree              %14s\n", decstr(_numTimesWaitedForFree).c_str());
          printf ("totalThreadCycles            %14s\n", decstr(_totalCycles).c_str());
          printf ("  cyclesProcessingBuffer     %14s  %%of total: %05.2f\n", 
                decstr(_cyclesProcessingBuffer).c_str(),
               (static_cast<float>(_cyclesProcessingBuffer)*100.0)/
                static_cast<float>(_totalCycles));
          printf ("  cyclesWaitingForFreeBuffer %14s  %%of total: %05.2f\n", 
                  decstr(_cyclesWaitingForFreeBuffer).c_str(),
                 (static_cast<float>(_cyclesWaitingForFreeBuffer)*100.0)/
                  static_cast<float>(_totalCycles));
          printf ("  cyclesWaitingForFullBuffer %14s  %%of total: %05.2f\n", 
                  decstr(_cyclesWaitingForFullBuffer).c_str(),
                 (static_cast<float>(_cyclesWaitingForFullBuffer)*100.0)/
                  static_cast<float>(_totalCycles));

          
          fprintf (_fp, "  numTimesWaitedForFull              %14s\n", decstr(_numTimesWaitedForFull).c_str());
          fprintf (_fp, "  numTimesWaitedForFree              %14s\n", decstr(_numTimesWaitedForFree).c_str());
		  fprintf (_fp, "totalThreadCycles            %14s\n", decstr(_totalCycles).c_str());
          fprintf (_fp, "  cyclesProcessingBuffer     %14s  %%of total: %05.2f\n", decstr(_cyclesProcessingBuffer).c_str(),
               (static_cast<float>(_cyclesProcessingBuffer)*100.0)/
                static_cast<float>(_totalCycles));
          fprintf (_fp, "  cyclesWaitingForFreeBuffer %14s  %%of total: %05.2f\n", 
                  decstr(_cyclesWaitingForFreeBuffer).c_str(),
                 (static_cast<float>(_cyclesWaitingForFreeBuffer)*100.0)/
                  static_cast<float>(_totalCycles));
          fprintf (_fp, "  cyclesWaitingForFullBuffer %14s  %%of total: %05.2f\n", 
                  decstr(_cyclesWaitingForFullBuffer).c_str(),
                 (static_cast<float>(_cyclesWaitingForFullBuffer)*100.0)/
                  static_cast<float>(_totalCycles));
      }
  private:
    UINT64 _cyclesProcessingBuffer;
    UINT64 _cyclesWaitingForFreeBuffer;
    UINT64 _cyclesWaitingForFullBuffer;
    UINT64 _startProgramAtCycle;
    UINT64 _totalCycles;
    UINT64 _numElementsProcessed;
    UINT32 _numBuffersFilled;
    UINT32 _numBuffersProcessedInAppThread;
    UINT32 _numTimesWaitedForFull;
    UINT32 _numTimesWaitedForFree;
    FILE * _fp;
} overallStatistics;

class BUFFER_LIST_STATISTICS
{
  public:
      BUFFER_LIST_STATISTICS() : _numTimesWaited(0), _cyclesWaitingForBuffer(0) 
      {
      }
      VOID UpdateCyclesWaitingForBuffer()
      {
          _cyclesWaitingForBuffer += ReadProcessorCycleCounter() - _startToWaitForBufferAtCycle;
      }
      VOID StartCyclesWaitingForBuffer()
      {
          _startToWaitForBufferAtCycle = ReadProcessorCycleCounter();
      }
      UINT64 CyclesWaitingForBuffer() {return _cyclesWaitingForBuffer;}

      VOID IncrementNumTimesWaited() {_numTimesWaited++;}
      UINT32 NumTimesWaitied() {return (_numTimesWaited);}

  private:
    UINT32 _numTimesWaited;
    UINT64 _startToWaitForBufferAtCycle;
    UINT64 _cyclesWaitingForBuffer;
};

class APP_THREAD_STATISTICS
{
  public:
      APP_THREAD_STATISTICS()
      {
          _numBuffersFilled = 0;
          _numBuffersProcessedInAppThread = 0;
          _numElementsProcessed = 0;
          _cyclesProcessingBuffer = 0;
          _cyclesWaitingForFreeBuffer = 0;
          _totalCycles = 0;
          _startAtCycle = ReadProcessorCycleCounter();
      }

      VOID DumpNumBuffersFilled()
      {
          if (!KnobLiteStatistics)
          {
              return;
          }
          _totalCycles = ReadProcessorCycleCounter() - _startAtCycle;
          printf ("\n\nTHREAD STATISTICS %14u\n", 0);
          printf ("  numElementsProcessed               %14s\n", decstr(_numElementsProcessed).c_str());
          printf ("  numBuffersFilled                   %14s\n", decstr(_numBuffersFilled).c_str());
          printf ("  numBuffersProcessedInAppThread     %14s\n", decstr(_numBuffersProcessedInAppThread).c_str());

          if (KnobStatistics)
          {
              _fp = fopen ((KnobStatisticsOutputFile.Value()).c_str(), "a");
              fprintf (_fp, "\n\nTHREAD STATISTICS\n");
              fprintf (_fp, "  numElementsProcessed               %14s\n", decstr(_numElementsProcessed).c_str());
              fprintf (_fp, "  numBuffersFilled                   %14s\n", decstr(_numBuffersFilled).c_str());
              fprintf (_fp, "  numBuffersProcessedInAppThread     %14s\n", decstr(_numBuffersProcessedInAppThread).c_str());

          }
      }

      VOID Dump()
      {
          printf ("  numTimesWaitedForFree              %14s\n", decstr(_numTimesWaitedForFree).c_str());
          printf ("totalThreadCycles            %14s\n", decstr(_totalCycles).c_str());
          printf ("  cyclesProcessingBuffer     %14s  %%of total: %05.2f\n", 
                decstr(_cyclesProcessingBuffer).c_str(),
               (static_cast<float>(_cyclesProcessingBuffer)*100.0)/
                static_cast<float>(_totalCycles));
          printf ("  cyclesWaitingForFreeBuffer %14s  %%of total: %05.2f\n", 
                  decstr(_cyclesWaitingForFreeBuffer).c_str(),
                 (static_cast<float>(_cyclesWaitingForFreeBuffer)*100.0)/
                  static_cast<float>(_totalCycles));

          
          fprintf (_fp, "  numTimesWaitedForFree              %14s\n", decstr(_numTimesWaitedForFree).c_str());
          fprintf (_fp, "totalThreadCycles            %14s\n", decstr(_totalCycles).c_str());
          fprintf (_fp, "  cyclesProcessingBuffer     %14s  %%of total: %05.2f\n", decstr(_cyclesProcessingBuffer).c_str(),
               (static_cast<float>(_cyclesProcessingBuffer)*100.0)/
                static_cast<float>(_totalCycles));
          fprintf (_fp, "  cyclesWaitingForFreeBuffer %14s  %%of total: %05.2f\n", 
                  decstr(_cyclesWaitingForFreeBuffer).c_str(),
                 (static_cast<float>(_cyclesWaitingForFreeBuffer)*100.0)/
                  static_cast<float>(_totalCycles));
          fclose (_fp);

      }
      
      VOID IncorporateBufferStatistics (BUFFER_LIST_STATISTICS * myFreeBufferListStats)
      {
          _numTimesWaitedForFree = myFreeBufferListStats->NumTimesWaitied();
          _cyclesWaitingForFreeBuffer = myFreeBufferListStats->CyclesWaitingForBuffer();
      }
      
      VOID UpdateCyclesProcessingBuffer()
      {
          _cyclesProcessingBuffer += ReadProcessorCycleCounter() - _startToProcessBufAtCycle;
      }
      VOID StartCyclesProcessingBuffer()
      {
          _startToProcessBufAtCycle = ReadProcessorCycleCounter();
      }
      
      VOID AddNumElementsProcessed(UINT32 numElementsProcessed) {_numElementsProcessed+=numElementsProcessed;}
      VOID IncrementNumBuffersProcessedInAppThread() {_numBuffersProcessedInAppThread++;}
      VOID IncrementNumBuffersFilled() {_numBuffersFilled++;}
      UINT32 NumBuffersProcessedInAppThread() {return _numBuffersProcessedInAppThread;}
      UINT64 NumBuffersElementsProcessed() {return _numElementsProcessed;}
      UINT32 NumBuffersFilled() {return _numBuffersFilled;}
      UINT32 NumTimesWaitedForFree() {return _numTimesWaitedForFree;}
      UINT64 CyclesProcessingBuffer() {return _cyclesProcessingBuffer;}
      UINT64 CyclesWaitingForFreeBuffer() {return _cyclesWaitingForFreeBuffer;}
      UINT64 TotalCycles() {return _totalCycles;}

  private:
    UINT64 _startToProcessBufAtCycle;
    UINT64 _cyclesProcessingBuffer;
    UINT64 _startAtCycle;
    UINT64 _totalCycles;
    UINT64 _numElementsProcessed;
    FILE * _fp;
    UINT32 _numBuffersProcessedInAppThread;
    UINT32 _numBuffersFilled;

    UINT32 _numTimesWaitedForFree;
    UINT64 _cyclesWaitingForFreeBuffer;
};

VOID OVERALL_STATISTICS::AccumulateAppThreadStatistics (APP_THREAD_STATISTICS *statistics, BOOL accumulateFreeStats)
{
    _numElementsProcessed += statistics->NumBuffersElementsProcessed();
    _numBuffersFilled += statistics->NumBuffersFilled();
    _numBuffersProcessedInAppThread += statistics->NumBuffersProcessedInAppThread();
    if (accumulateFreeStats)
    {
        _numTimesWaitedForFree += statistics->NumTimesWaitedForFree();
        _cyclesWaitingForFreeBuffer += statistics->CyclesWaitingForFreeBuffer();
    }
    _cyclesProcessingBuffer += statistics->CyclesProcessingBuffer();
}

VOID OVERALL_STATISTICS::IncorporateBufferStatistics (BUFFER_LIST_STATISTICS *statistics, BOOL isFull)
{
    if (isFull)
    {
        _numTimesWaitedForFull = statistics->NumTimesWaitied();
        _cyclesWaitingForFullBuffer = statistics->CyclesWaitingForBuffer();
    }
    else
    {
         _numTimesWaitedForFree = statistics->NumTimesWaitied();
        _cyclesWaitingForFreeBuffer = statistics->CyclesWaitingForBuffer();
    }
}