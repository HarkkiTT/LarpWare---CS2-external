using System.Runtime.InteropServices;
using Swed64;

namespace TuffTool.Core;

public sealed class Memory : IDisposable
{
    private Swed? _swed;
    private bool _disposed;
    private uint _cachedPid;

    public IntPtr ClientBase { get; private set; }
    public IntPtr Engine2Base { get; private set; }
    public bool IsAttached => _swed != null && _swed.GetProcess() != null;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public bool IsGameFocused()
    {
        if (!IsAttached) return false;
        if (_cachedPid == 0) _cachedPid = (uint)_swed!.GetProcess().Id;

        IntPtr foregroundHwnd = GetForegroundWindow();
        if (foregroundHwnd == IntPtr.Zero) return false;

        GetWindowThreadProcessId(foregroundHwnd, out uint foregroundPid);
        return foregroundPid == _cachedPid;
    }

    public bool Attach()
    {
        try
        {
            _swed = new Swed("cs2");
            if (_swed.GetProcess() == null)
                return false;

            ClientBase = _swed.GetModuleBase("client.dll");
            Engine2Base = _swed.GetModuleBase("engine2.dll");

            return ClientBase != IntPtr.Zero && Engine2Base != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    public T Read<T>(IntPtr address) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = _swed!.ReadBytes(address, size);
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);  
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public T[] ReadArray<T>(IntPtr address, int count) where T : struct
    {
        int typeSize = Marshal.SizeOf<T>();
        int size = typeSize * count;
        byte[] buffer = _swed!.ReadBytes(address, size);
        T[] array = new T[count];
        
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            for (int i = 0; i < count; i++)
            {
                array[i] = Marshal.PtrToStructure<T>(ptr + (i * typeSize));
            }
            return array;
        }
        finally
        {
            handle.Free();
        }
    }

    public void Write<T>(IntPtr address, T value) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = new byte[size];
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            _swed!.WriteBytes(address, buffer);
        }
        finally
        {
            handle.Free();
        }
    }

    public string ReadString(IntPtr address, int maxLength = 128)
    {
        byte[] buffer = _swed!.ReadBytes(address, maxLength);
        int nullIndex = Array.IndexOf(buffer, (byte)0);
        if (nullIndex >= 0)
            return System.Text.Encoding.UTF8.GetString(buffer, 0, nullIndex);
        return System.Text.Encoding.UTF8.GetString(buffer);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _swed = null;
    }
}
