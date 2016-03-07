#include <string>

#ifdef WIN32
#define strncasecmp _strnicmp
#define XFMT "0x%16.16Ix"
#define IFMT "%Iu"
#else
#include <strings.h>
#define XFMT "0x%16.16zx"
#define IFMT "%zu"
#endif

#define UNUSED_ARG(x) x;

#include <stdint.h>
#include <errno.h>

void DebugWrite(const char* msg);

uint64_t GetProcessTicks(int pid); 

std::string GetFullFileName(const std::string& fileName);

