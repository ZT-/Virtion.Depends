#pragma once

#include  <windows.h>
#include <string>
#include <thread>
#include "Detours/detours.h"


typedef void(_stdcall* SymbolCallbackDelegate)(LPCSTR);

class PEParser
{
public:
	PEParser();
	~PEParser();

	bool Open(std::string path);

	bool Print();
	bool PrintExport();

	bool AddFile();

	bool AddSymble();

	bool Write(std::string path);

	std::string  pePath;
	HANDLE hNew;
	HANDLE hOld;
	PDETOUR_BINARY pBinary = NULL;

};

