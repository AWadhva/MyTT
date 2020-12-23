#include <cstring>
#include "..\\..\\ThirdPartyLib\\ThalesCscApi-1.08\\Include\\ThalesCSCApi.h"


typedef void (__stdcall *fCallBackEx_StdCall) (
	CSC_HANDLE, StatusCSC*
	);

fCallBackEx_StdCall callbkPollingHost, callbkDetectionHost;

void CallbackAgentPolling(CSC_HANDLE h, StatusCSC *status)
{
	StatusCSC *pCopyStatus = new StatusCSC(*status);	
	std::memcpy(pCopyStatus->ucATR, status->ucATR, MAX_ATR_SIZE);

	(*callbkPollingHost)(
		h, pCopyStatus
		);
}

void CallbackAgentDetection(CSC_HANDLE h, StatusCSC *status)
{
	StatusCSC *pCopyStatus = new StatusCSC(*status);	
	std::memcpy(pCopyStatus->ucATR, status->ucATR, MAX_ATR_SIZE);

	(*callbkDetectionHost)(
		h, pCopyStatus
		);
}


extern "C" __declspec(dllexport) short __stdcall 
  sSmartStartPollingEx_V4
	(CSC_HANDLE apiHandle,
	 UCHAR ucNumScenario,
	 eTypACType xAC,
	 UCHAR ucSpontCall,
	 fCallBackEx_StdCall pvCallBackEx)
{
	callbkPollingHost = pvCallBackEx;
	return sSmartStartPollingEx
	(apiHandle,
	 ucNumScenario,
	 xAC,
	 ucSpontCall,
	 CallbackAgentPolling);
}



extern "C" __declspec(dllexport) short __stdcall 
  sSmartStartDetectRemovalEx_V4
(CSC_HANDLE apiHandle,
	UCHAR ucSpontCall,
	fCallBackEx_StdCall pvCallBackEx)
{
	callbkDetectionHost = pvCallBackEx;
	return sSmartStartDetectRemovalEx
	(apiHandle,
	 ucSpontCall,
	 CallbackAgentDetection);
}

