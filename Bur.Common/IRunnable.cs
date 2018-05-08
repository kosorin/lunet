﻿namespace Bur.Common
{
    public interface IRunnable
    {
        bool IsRunning { get; }

        void Start();

        void Stop();
    }
}
