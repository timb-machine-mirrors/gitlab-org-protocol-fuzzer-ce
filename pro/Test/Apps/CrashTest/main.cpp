#include <cstdlib>
#include <string>
#include <iostream>
#include <fstream>

#ifdef WIN32
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#define sleep Sleep
#define SLEEP_FACTOR 1000
#else
#include <unistd.h>
#define SLEEP_FACTOR 1
#endif

int main(int /*argc*/, char** argv)
{
	std::string cmd = argv[1];
	if (cmd == "exit") {
		return std::atoi(argv[2]);
	}
	else if (cmd == "timeout") {
		sleep(std::atoi(argv[2]) * SLEEP_FACTOR);
	}
	else if (cmd == "regex") {
		std::cout << argv[2] << std::endl;
		std::cerr << argv[3] << std::endl;
	}
	else if (cmd == "when") {
		std::fstream fout(argv[2]);
		fout << argv[3];
		fout.close();
	}
	return 0;
}
