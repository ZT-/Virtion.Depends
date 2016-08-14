#include "stdafx.h"
#include "PEParser.h"

BywayCallbackDelegate BywayCallback;
SymbolCallbackDelegate SymbolCallback;
ExportCallBackDelegate ExportCallBack;
PEParser* g_PEParser = nullptr;

extern "C" __declspec(dllexport) void GetImports(BywayCallbackDelegate bywayCallBack, SymbolCallbackDelegate symbolCallBack)
{
	BywayCallback = bywayCallBack;
	SymbolCallback = symbolCallBack;
	g_PEParser->Print();
}

extern "C" __declspec(dllexport) void GetExports(ExportCallBackDelegate exportCallBack)
{
	ExportCallBack = exportCallBack;
	g_PEParser->PrintExport();
}

extern "C"  __declspec(dllexport) bool OpenEPFile(LPSTR path)
{
	g_PEParser = new PEParser();
	return  g_PEParser->Open(path);
}



int _tmain(int argc, _TCHAR* argv[])
{
	PEParser* peParser = new PEParser();
	peParser->Open("PEImportEditer.exe");
	peParser->Print();
	peParser->PrintExport();

	//g_PEParser->Write("Win32Projec.exe");
	return 0;
}



