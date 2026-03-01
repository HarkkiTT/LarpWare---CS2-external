using System;
using System.Numerics;
using System.Threading;
using TuffTool.Core;
using TuffTool.SDK;

namespace TuffTool.Features;

public class StandaloneRCS
{
    private readonly Memory _mem;
    private bool _running = true;
    private Thread? _thread;
    
    
    private const nint dwLocalPlayerPawn = 0x2064AE0;
    private const nint dwViewAngles = 0x2314F98;
    private const nint m_iHealth = 0x354;
    private const nint m_iShotsFired = 0x270C;
    private const nint m_aimPunchAngle = 0x16CC;

    private float _oldPunchX = 0f;
    private float _oldPunchY = 0f;
    
    
    private float _smoothDeltaX = 0f;
    private float _smoothDeltaY = 0f;
    private int _lastShots = 0;

    public bool Enabled { get; set; } = false;
    public float RecoilScaleX { get; set; } = 2.0f;
    public float RecoilScaleY { get; set; } = 2.0f;

    public StandaloneRCS(Memory mem)
    {
        _mem = mem;
        Start();
    }

    public void Start()
    {
        if (_thread != null && _thread.IsAlive) return;
        _running = true;
        _thread = new Thread(RcsLoop) { IsBackground = true };
        _thread.Priority = ThreadPriority.Highest;
        _thread.Start();
    }

    private void RcsLoop()
    {
        while (_running)
        {
            try
            {
                IntPtr client = _mem.ClientBase;
                if (client == IntPtr.Zero) { Thread.Sleep(500); continue; }

                IntPtr localPlayer = _mem.Read<IntPtr>(client + dwLocalPlayerPawn);
                if (localPlayer == IntPtr.Zero) 
                { 
                    _oldPunchX = _oldPunchY = 0f; 
                    _lastShots = 0;
                    Thread.Sleep(100); 
                    continue; 
                }

                int shots = _mem.Read<int>(localPlayer + m_iShotsFired);
                float punchX = _mem.Read<float>(localPlayer + m_aimPunchAngle);
                float punchY = _mem.Read<float>(localPlayer + m_aimPunchAngle + 4);
                int health = _mem.Read<int>(localPlayer + m_iHealth);

                
                float rawDeltaX = punchX - _oldPunchX;
                float rawDeltaY = punchY - _oldPunchY;

                if (Enabled && health > 0)
                {
                    
                    
                    if (shots > 0 && _lastShots == 0)
                    {
                        _oldPunchX = punchX;
                        _oldPunchY = punchY;
                        _lastShots = shots;
                        _smoothDeltaX = 0;
                        _smoothDeltaY = 0;
                        continue; 
                    }

                    
                    
                    float smoothing = 0.5f; 
                    _smoothDeltaX = (_smoothDeltaX * (1f - smoothing)) + (rawDeltaX * smoothing);
                    _smoothDeltaY = (_smoothDeltaY * (1f - smoothing)) + (rawDeltaY * smoothing);

                    
                    _smoothDeltaX = Math.Clamp(_smoothDeltaX, -0.5f, 0.5f);
                    _smoothDeltaY = Math.Clamp(_smoothDeltaY, -0.5f, 0.5f);

                    
                    
                    
                    float scaleX = (shots > 0 || (_smoothDeltaX * punchX > 0)) ? RecoilScaleX : 1.0f;
                    float scaleY = (shots > 0 || (_smoothDeltaY * punchY > 0)) ? RecoilScaleY : 1.0f;

                    if (Math.Abs(_smoothDeltaX) > 0.0001f || Math.Abs(_smoothDeltaY) > 0.0001f)
                    {
                        IntPtr viewAddr = client + dwViewAngles;
                        float currX = _mem.Read<float>(viewAddr);
                        float currY = _mem.Read<float>(viewAddr + 4);

                        _mem.Write(viewAddr, currX - (_smoothDeltaX * scaleX));
                        _mem.Write(viewAddr + 4, currY - (_smoothDeltaY * scaleY));
                    }
                }
                else
                {
                    _smoothDeltaX = _smoothDeltaY = 0f;
                    _lastShots = 0;
                }

                
                _oldPunchX = punchX;
                _oldPunchY = punchY;
                _lastShots = shots;
            }
            catch { }

            Thread.Sleep(2); 
        }
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join();
    }
}
