// This is the main DLL file.
#include "stdafx.h"
#include "crc_16.h"
#include "d3des.h"
#include "MacAlgo.h"


/*----------------------------------------------------------------------------*\
||                                                                            ||
|| Function :  ComputeCRC                                                     ||
|| --------                                                                   ||
||                                                                            ||
||                                                                            ||
\*----------------------------------------------------------------------------*/
unsigned short MacAlgo::ComputeCRC(unsigned char Data[], unsigned long DataSize)
{
	return CRC_compute( Data, DataSize);
}

/*----------------------------------------------------------------------------*\
||                                                                            ||
|| Function :  SetDeskey                                                      ||
|| --------                                                                   ||
||                                                                            ||
||                                                                            ||
\*----------------------------------------------------------------------------*/
void MacAlgo::SetDeskey(unsigned char key[], short edf)
{
	deskey(key, edf);
}

/*----------------------------------------------------------------------------*\
||                                                                            ||
|| Function :  CalcDes														  ||
|| --------                                                                   ||
||                                                                            ||
||                                                                            ||
\*----------------------------------------------------------------------------*/
void MacAlgo::CalcDes(unsigned char inblock[], unsigned char outblock[])
{
	des(inblock, outblock);
}