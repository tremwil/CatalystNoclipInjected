// NoclipInjectedDll.cpp : Defines the DLL functions.
//

#include "stdafx.h"
#include "NoclipInjectedDll.h"
#include <limits>

using namespace std;

/* function to get a dynamic address from a deep pointer */
PVOID GetDynamicAddress(INT64 baseAddr, INT32 offsets[], INT32 nOffsets)
{
	INT64 *addr = (INT64*)baseAddr;

	for (int i = 0; i < nOffsets; i++)
	{
		addr = (INT64*)(*addr + offsets[i]);
		if (addr == NULL) return NULL;
	}

	return addr;
}

/* Main noclip thread, started in DLLMain */
DWORD WINAPI NoclipThread(HMODULE module)
{
	SharedInfo *sInfo; // A pointer to shared memory between the DLL and our app
	char *name = "NoclipDllFileMapping"; // The memory mapped file name

	// Try to open the file map that allows communication between the injector and the thread
	HANDLE hMapFile = OpenFileMapping(FILE_MAP_READ, FALSE, name);
	if (hMapFile == NULL) // Could not open the file map :c
	{
		MessageBox(NULL, "Failure acquiring shared memory - OpenFileMapping", "Injected Noclip DLL", MB_OK);
		FreeLibraryAndExitThread(module, 0);
	}

	// Try to get a pointer to the file map's data
	sInfo = (SharedInfo*)MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, sizeof(SharedInfo));
	if (sInfo == NULL) // Could not get pointer :c
	{
		MessageBox(NULL, "Failure acquiring shared memory - MapViewOfFile", "Injected Noclip DLL", MB_OK);
		FreeLibraryAndExitThread(module, 0);
	}

	/* Get things ready for the main loop */

	// Get a handle to the injector to query its exit code
	HANDLE hInjector = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, sInfo->injectorPID);
	DWORD exitCode;

	// Get the 1st position pointer
	int pos1Offsets[] = { 0x70, 0x98, 0x238, 0x18, 0x22d0 };
	DeepPointer positionDP1 = DeepPointer(0x142578A68, pos1Offsets, 5);
	Vec3 *positionPtr1 = NULL;

	// Get the 2nd position pointer
	int pos2Offsets[] = { 0x70, 0x98, 0x238, 0x20, 0x22d0 };
	DeepPointer positionDP2 = DeepPointer(0x142578A68, pos2Offsets, 5);
	Vec3 *positionPtr2 = NULL;

	// Get the pointer to the last ground position
	int gposOffsets[] = { 0x20, 0x20, 0x40, 0x20, 0x00 };
	DeepPointer groundPosDP = DeepPointer(0x1423DA028, gposOffsets, 5);
	Vec3 *groundPosPtr = NULL;

	// Get the movement pointer, which is static
	int *movementStatePtr = (int*)0x142576FDC;

	BOOL lastNoclipState = FALSE;
	Vec3 tgtPosition;

	// Clock related stuff
	LARGE_INTEGER clockFreq, ctime, ltime;
	float dt;

	QueryPerformanceFrequency(&clockFreq); // Get clock frequency
	QueryPerformanceCounter(&ltime); // Initialize ltime

	/* Main noclip loop, runs until injector is closed */

	do
	{
		// Mesure the time between two iterations
		QueryPerformanceCounter(&ctime);
		dt = float(ctime.QuadPart - ltime.QuadPart) / clockFreq.QuadPart;
		ltime = ctime;

		// Check if the movement pointer is not -1, which means the game is not loading
		if (*movementStatePtr != -1)
		{
			// Update the dynamic addresses
			positionPtr1 = positionDP1.GetPtrToValue<Vec3>();
			positionPtr2 = positionDP2.GetPtrToValue<Vec3>();
			groundPosPtr = groundPosDP.GetPtrToValue<Vec3>();

			if (!lastNoclipState && sInfo->noclipState) // Noclip was just turned on
			{
				tgtPosition = *positionPtr1;
			}

			if (sInfo->noclipState) // Noclip is on
			{
				// Prevents the player from dying when going down super fast
				groundPosPtr->y = -FLT_MAX;

				// Adjust the target position
				tgtPosition = tgtPosition + sInfo->dpos * dt;

				// Set both position pointers to the target position
				*positionPtr1 = tgtPosition;
				*positionPtr2 = tgtPosition;
			}
			else if (sInfo->noStumbleState)
			{   // NoStumble is on, so set our groud pos to our current height
				groundPosPtr->y = positionPtr1->y;
			}
		}

		lastNoclipState = sInfo->noclipState; // Update the last noclip state

		Sleep(WAIT_TIME_MS); // Sleep to take less CPU
		GetExitCodeProcess(hInjector, &exitCode); // Update the exit code of the noclip app

	} while (exitCode == STILL_ACTIVE);

	/* Process exited, so release everything, free our DLL and exit */

	// Release the mapped file
	UnmapViewOfFile(sInfo);
	CloseHandle(hMapFile);

	// Unmap the DLL and exit the thread
	FreeLibraryAndExitThread(module, 0);
}