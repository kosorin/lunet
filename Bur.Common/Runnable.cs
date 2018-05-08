namespace Bur.Common
{
    public abstract class Runnable : IRunnable
    {
        private volatile bool isRunning;

        public bool IsRunning
        {
            get => isRunning;
            protected set => isRunning = value;
        }

        public abstract void Start();

        public abstract void Stop();
    }
}
