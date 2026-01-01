using System;
using System.Runtime.InteropServices;

namespace GameProtocol;

public enum PacketType : ushort
{
    Login = 1,
    Logout = 2,
    Move = 3,
    Attack = 4,
    Chat = 5
}


public enum ErrorCode
{
    Success = 0,
    InvalidCredentials = -1,
    ServerFull = -2,
    NotAuthorized = -3
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LoginRequest
{
    [MarshalAs(UnmanagedType.LPStr)]
    public string username; // Offset: 0, Original: string
    [MarshalAs(UnmanagedType.LPStr)]
    public string password; // Offset: 8, Original: string
    public uint clientVersion; // Offset: 16, Original: uint32_t
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LoginResponse
{
    public ErrorCode errorCode; // Offset: 0, Original: ErrorCode
    public ulong sessionId; // Offset: 8, Original: uint64_t
    [MarshalAs(UnmanagedType.LPStr)]
    public string playerName; // Offset: 16, Original: string
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MoveRequest
{
    public float positionX; // Offset: 0, Original: float
    public float positionY; // Offset: 8, Original: float
    public float positionZ; // Offset: 16, Original: float
    public ulong timestamp; // Offset: 24, Original: uint64_t
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ChatMessage
{
    public ulong senderId; // Offset: 0, Original: uint64_t
    [MarshalAs(UnmanagedType.LPStr)]
    public string message; // Offset: 8, Original: string
    public ulong timestamp; // Offset: 16, Original: uint64_t
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InventoryUpdate
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public uint[] items; // Offset: 0, Original: uint32_t
    public ushort itemCount; // Offset: 8, Original: uint16_t
}

