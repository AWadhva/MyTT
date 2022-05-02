// TstV4Callbacks.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include <windows.h>
using namespace std;
// actually it is version 1.8. not 1.08
#include "..\\..\\..\\ThirdPartyLib\\ThalesCscApi-1.08\\Include\\ThalesCscApi.h"


HANDLE r1MediaProduced, r2MediaProduced, r1MediaRemoved, r2MediaRemoved;
CSC_HANDLE h1 = -1, h2 = -1;
StatusCSC status1, status2;

void MediaProduced (CSC_HANDLE h, StatusCSC *status)
{
	if (h == h1)
	{
		::memcpy(&status1, status, sizeof(StatusCSC));
		::SetEvent(r1MediaProduced);
	}
	else if (h == h2)
	{
		::memcpy(&status2, status, sizeof(StatusCSC));
		::SetEvent(r2MediaProduced);
	}
}

void MediaRemoved (CSC_HANDLE h, StatusCSC *status)
{
	if (h == h1)
	{
		::memcpy(&status1, status, sizeof(StatusCSC));
		::SetEvent(r1MediaRemoved);
	}
	else if (h == h2)
	{
		::memcpy(&status2, status, sizeof(StatusCSC));
		::SetEvent(r2MediaRemoved);
	}
}

DWORD WINAPI RdrPoller(
    LPVOID lpThreadParameter
    )
{
	HANDLE evts[4] = {r1MediaProduced, r2MediaProduced, r1MediaRemoved, r2MediaRemoved};
	while(true)
	{
		DWORD signalledId = ::WaitForMultipleObjects(4, evts, FALSE, -1);

		switch(signalledId)
		{
		case WAIT_OBJECT_0:
			cout << "\nMedia produced at 1";
			::sSmartStartDetectRemovalEx(h1, SCENARIO_1, &MediaRemoved);
			break;
		case WAIT_OBJECT_0 + 1:
			cout << "\nMedia produced at 2";
			::sSmartStartDetectRemovalEx(h2, SCENARIO_1, &MediaRemoved);
			break;
		case WAIT_OBJECT_0 + 2:
			cout << "\nMedia removed from 1";
			::sSmartStartPollingEx(h1, SCENARIO_1, eTypACType::AC_WITHOUT_COLLISION, DETECTION_WITH_EVENT, &MediaProduced);
			break;
		case WAIT_OBJECT_0 + 3:
			cout << "\nMedia removed from 2";
			::sSmartStartPollingEx(h2, SCENARIO_1, eTypACType::AC_WITHOUT_COLLISION, DETECTION_WITH_EVENT, &MediaProduced);
			break;
		default:
			{
				cout << "Unexpected";
				break;
			}
		}
	}
	return 0;
}

void CreateEvents()
{
	r1MediaProduced = ::CreateEvent(NULL, FALSE, FALSE, NULL);
	r2MediaProduced = ::CreateEvent(NULL, FALSE, FALSE, NULL);
	r1MediaRemoved = ::CreateEvent(NULL, FALSE, FALSE, NULL);
	r2MediaRemoved = ::CreateEvent(NULL, FALSE, FALSE, NULL);
}

int _tmain(int argc, _TCHAR* argv[])
{
	CreateEvents();
	short result;

	result = ::sCSCReaderStartEx("COM3:", 115200, &h1);
	result = ::sCSCReaderStartEx("COM4:", 115200, &h2);

	StatusCSC status;
	result = ::sSmartStatusEx(h1, &status);
	if (status1.ucStatCSC != ST_VIRGIN)
	{
		result = ::sCscRebootEx(h1);
	}
	result = ::sSmartStatusEx(h2, &status);
	if (status1.ucStatCSC != ST_VIRGIN)
	{
		result = ::sCscRebootEx(h2);
	}

	InstallCard install;
	install.xCardType = CARD_MIFARE1;
	install.iCardParam.xMifParam.sSize = 0;
	
	result = ::sSmartInstCardEx(h1, eDestReaderType::DEST_CARD, &install);
	result = ::sSmartInstCardEx(h2, eDestReaderType::DEST_CARD, &install);

	ScenarioPolling scenarios[1];
	scenarios[0].ucAntenna = 1;
	scenarios[0].ucRepeatNumber = 1;
	scenarios[0].xCardType = eTypCardType::CARD_MIFARE1;
	
	result = ::sSmartConfigEx(h1, SCENARIO_1, 1, scenarios);
	result = ::sSmartConfigEx(h2, SCENARIO_1, 1, scenarios);

	DWORD thdId;
	HANDLE thd = ::CreateThread(NULL, 0, &RdrPoller, NULL, 0, &thdId);

	result = ::sSmartStartPollingEx(h1, SCENARIO_1, eTypACType::AC_WITHOUT_COLLISION, DETECTION_WITH_EVENT, &MediaProduced);
	result = ::sSmartStartPollingEx(h2, SCENARIO_1, eTypACType::AC_WITHOUT_COLLISION, DETECTION_WITH_EVENT, &MediaProduced);

	cout << "\nPolling started";

	int x;
	cin >> x;
	return 0;
}