#include "stdafx.h"
#include <windows.h>
#include "Detours/detours.h"

#include "PEParser.h"

#include <vector>
#include  <string>
using namespace std;

extern  SymbolCallbackDelegate SymbolCallback;
extern  SymbolCallbackDelegate BywayCallback;

std::vector<std::string> exportList;

PEParser::PEParser()
{
}


PEParser::~PEParser()
{
}

bool PEParser::Open(std::string path)
{
	this->pePath = path.c_str();

	LPCSTR szOrg = this->pePath.c_str();

	hOld = CreateFileA(szOrg,
		GENERIC_READ,
		FILE_SHARE_READ,
		NULL,
		OPEN_EXISTING,
		FILE_ATTRIBUTE_NORMAL,
		NULL);

	if (hOld == INVALID_HANDLE_VALUE) {
		printf("Couldn't open input file: %s, error: %d\n",
			szOrg, GetLastError());

		CloseHandle(hOld);
		return false;
	}

	if ((pBinary = DetourBinaryOpen(hOld)) == NULL) {
		printf("DetourBinaryOpen failed: %d\n", GetLastError());
		return false;
	}

	return  true;
}

string currentFile;

static BOOL CALLBACK ListBywayCallback(PVOID pContext,
	PCHAR pszFile,
	PCHAR *ppszOutFile)
{
	(void)pContext;

	*ppszOutFile = pszFile;
	if (pszFile)
	{
		printf("    %s\n", pszFile);
	}
	return TRUE;
}



static BOOL CALLBACK BinarySymbolCallback(
	PVOID pContext,
	ULONG nOrigOrdinal,
	ULONG nOrdinal,
	ULONG *pnOutOrdinal,
	PCHAR pszOrigSymbol,
	PCHAR pszSymbol,
	PCHAR *ppszOutSymbol
	)
{
	if (pszOrigSymbol)
	{
		//printf(" %s %s\n", currentFile.c_str(), pszOrigSymbol);
		if (SymbolCallback != nullptr)
		{
			SymbolCallback(pszOrigSymbol);
		}
	}


	return TRUE;
}

static BOOL CALLBACK ListFileCallback(PVOID pContext,
	PCHAR pszOrigFile,
	PCHAR pszFile,
	PCHAR *ppszOutFile)
{
	(void)pContext;

	*ppszOutFile = pszFile;
	currentFile = pszFile;
	if (BywayCallback != nullptr)
	{
		BywayCallback(pszFile);
	}

	printf(" %s -> %s\n", pszOrigFile, pszFile);
	return TRUE;
}

bool PEParser::Print()
{
	if (!DetourBinaryEditImports(pBinary, NULL,
		ListBywayCallback, ListFileCallback,
		BinarySymbolCallback, NULL))
	{

		printf("DetourBinaryEditImports failed: %d\n", GetLastError());
	}

	return false;
}

static BOOL CALLBACK AddBywayCallback(PVOID pContext,
	PCHAR pszFile,
	PCHAR *ppszOutFile)
{
	PBOOL pbAddedDll = (PBOOL)pContext;

	if (!pszFile && !*pbAddedDll) {                     // Add new byway.
		*pbAddedDll = TRUE;
		*ppszOutFile = "my.dll";
	}
	return TRUE;
}


static BOOL CALLBACK EnumerateExportCallback(PVOID pContext,
	ULONG nOrdinal,
	PCHAR pszSymbol,
	PVOID pbTarget)
{
	//char arr[260] = {};
	//sprintf_s(arr, "%7d  %p %-30s\n", (ULONG)nOrdinal, pbTarget, pszSymbol ? pszSymbol : "[NONAME]");
	//OutputDebugStringA(arr);

	//if (ExportsCallBack != nullptr)
	//{
	//ExportsCallBack("[NONAME]");
	//}

	exportList.push_back(pszSymbol ? pszSymbol : "[NONAME]");
	return TRUE;
}


bool PEParser::PrintExport()
{
	HINSTANCE hInst = LoadLibrary(this->pePath.c_str());
	if (hInst == NULL) {
		return FALSE;
	}

	PVOID pbEntry = DetourGetEntryPoint(hInst);
	printf("  EntryPoint: %p\n", pbEntry);

	printf("    Ordinal      RVA     Name\n");
	DetourEnumerateExports(hInst, NULL, EnumerateExportCallback);


	FreeLibrary(hInst);

	return true;
}

bool PEParser::Write(std::string path)
{
	LPCSTR szNew = path.c_str();
	hNew = CreateFileA(szNew,

		GENERIC_WRITE | GENERIC_READ, 0, NULL, CREATE_ALWAYS,
		FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
	if (hNew == INVALID_HANDLE_VALUE)
	{
		printf("Couldn't open output file: %s, error: %d\n",
			szNew, GetLastError());
		return false;
	}

	if (!DetourBinaryWrite(pBinary, hNew)) {
		printf("DetourBinaryWrite failed: %d\n", GetLastError());
		return false;
	}

	DetourBinaryClose(pBinary);

	CloseHandle(hNew);

	return  true;
}



bool PEParser::AddFile()
{
	BOOL bAddedDll = FALSE;

	DetourBinaryResetImports(pBinary);

	if (!DetourBinaryEditImports(pBinary,
		&bAddedDll,
		AddBywayCallback, NULL, NULL, NULL)) {
		printf("DetourBinaryEditImports failed: %d\n", GetLastError());
	}

	return false;
}