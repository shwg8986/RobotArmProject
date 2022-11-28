
using UnityEngine;
using System;

namespace Teleoperation {
    
/// <summary>
/// https://github.com/hecomi/uOSC/blob/master/Assets/uOSC/Runtime/Core/DotNet/Thread.cs
/// uOSCを真似して，Threadの扱い方を知る
/// </summary>
public class Thread {
    private System.Threading.Thread _thread;
    private bool _isRunning = false;
    private Action _loopFunc = null;
    protected const int IntervalMillisec = 1;
    
    
    public void Start(Action loopFunc) {
        if (_isRunning || loopFunc is null) return;

        _isRunning = true;
        _loopFunc = loopFunc;
        _thread = new System.Threading.Thread(ThreadLoop);
        _thread.Start();
    }

    void ThreadLoop() {
        while (_isRunning) {
            try {
                _loopFunc();
                System.Threading.Thread.Sleep(IntervalMillisec);
            } catch (Exception e) {
                Debug.LogError(e.Message);
                Debug.LogError(e.StackTrace);
            }
        }
    }

    public void Stop(int timeoutMilliseconds = 3000) {
        if (!_isRunning) return;

        _isRunning = false;
        if (_thread.IsAlive) {
            _thread.Join(timeoutMilliseconds);
            if (_thread.IsAlive) {
                _thread.Abort();
            }
        }
    }
}
}
