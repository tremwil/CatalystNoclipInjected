#pragma once

#include "stdafx.h"

// The wait time between two loop iterations
#define WAIT_TIME_MS 10

DWORD WINAPI NoclipThread(HMODULE module);
PVOID GetDynamicAddress(INT64 baseAddr, INT32 offsets[], INT32 nOffsets);

struct Vec3
{
	float x;
	float y;
	float z;

	Vec3() : x(0), y(0), z(0) {}
	Vec3(float x, float y, float z) : x(x), y(y), z(z) {}

	Vec3& operator=(const Vec3& a)
	{
		x = a.x;
		y = a.y;
		z = a.z;
		return *this;
	}

	Vec3 operator+(const Vec3& a)
	{
		return Vec3(x + a.x, y + a.y, z + a.z);
	}

	Vec3 operator-()
	{
		return Vec3(-x, -y, -z);
	}

	Vec3 operator-(const Vec3& a)
	{
		return Vec3(x - a.x, y - a.y, z - a.z);
	}

	Vec3 operator*(const float& a)
	{
		return Vec3(x * a, y * a, z * a);
	}

	float operator*(const Vec3& a)
	{
		return x * a.x + y * a.y + z * a.z;
	}

	Vec3 operator/(const float& a) {
		float c = 1 / a;
		return Vec3(x * c, y * c, z * c);
	}
};

struct DeepPointer
{
	INT64 baseAddr;
	INT32 offsets[10]; // Max 10 offsets
	INT32 nOffsets;

	DeepPointer(INT64 b, INT32 o[], INT32 n)
	{
		baseAddr = b;
		nOffsets = n;
		for (int i = 0; i < n; i++)
		{
			offsets[i] = o[i];
		}
	}

	template<typename T> T Read()
	{
		T *ptr = (T*)GetDynamicAddress(baseAddr, offsets, nOffsets);

		if (ptr)
		{
			return *ptr;
		}

		return default(T);
	}

	template<typename T> BOOL Write(T value)
	{
		T *ptr = (T*)GetDynamicAddress(baseAddr, offsets, nOffsets);

		if (ptr)
		{
			*ptr = value;
			return TRUE;
		}

		return FALSE;
	}

	template<typename T> T* GetPtrToValue()
	{
		return (T*)GetDynamicAddress(baseAddr, offsets, nOffsets);
	}
};

struct SharedInfo
{
	INT32 injectorPID;
	BOOL noclipState;
	BOOL noStumbleState;
	Vec3 dpos;
};