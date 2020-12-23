// MacAlgo.h

#pragma once

using namespace System;

#include "stdafx.h"
#include "crc_16.h"
#include "d3des.h"

#if defined(DLL_PROJECT)
# define DLLAPI __declspec(dllexport)
#endif

class MacAlgo
{
public : 
	static __declspec(dllexport) unsigned short ComputeCRC(unsigned char Data[], unsigned long DataSize);

	static __declspec(dllexport) void SetDeskey(unsigned char key[], short edf);

	static __declspec(dllexport) void CalcDes(unsigned char inblock[], unsigned char outblock[]);
};


