#include "stdafx.h"
#include "PEParser.h"
#include <vector>

SymbolCallbackDelegate BywayCallback;
SymbolCallbackDelegate SymbolCallback;
extern std::vector<std::string> exportList;

PEParser* g_PEParser = nullptr;

extern "C" __declspec(dllexport) void GetImports(SymbolCallbackDelegate bywayCallBack, SymbolCallbackDelegate symbolCallBack)
{
	BywayCallback = bywayCallBack;
	SymbolCallback = symbolCallBack;
	g_PEParser->Print();
}

extern "C" __declspec(dllexport) void GetExports(SymbolCallbackDelegate exportCallBack)
{
	g_PEParser->PrintExport();
	for (size_t i = 0; i < exportList.size(); i++)
	{
		exportCallBack(exportList[i].c_str());
	}

	//char arr[260] = {};
	//sprintf_s(arr, "%d\n", GetLastError());
	//OutputDebugStringA(arr);
}

extern "C"  __declspec(dllexport) bool OpenEPFile(LPSTR path)
{
	g_PEParser = new PEParser();
	return  g_PEParser->Open(path);
}



int _tmain(int argc, _TCHAR* argv[])
{
	PEParser* peParser = new PEParser();
	peParser->Open("RenderWrapper.dll");
	peParser->Print();
	peParser->PrintExport();

	//g_PEParser->Write("Win32Projec.exe");
	return 0;
}



