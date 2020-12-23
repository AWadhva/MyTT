/******************************************************************************/
/*                                                                            */
/*                            File Inclusion                                  */
/*                                                                            */
/******************************************************************************/
//
#ifndef _CRC_16_H_
#define _CRC_16_H_


/////////////////////////////////////////////////////////////////////////////
// CRC16Bit

unsigned short CRC_computeWithInitValue( void *pZone, unsigned long SizeOfZone, unsigned short CrcInit );
unsigned short CRC_compute( void *pZone, unsigned long SizeOfZone);

#endif
