namespace Lure
{
    public abstract class Configuration : ILockable
    {
        private volatile bool _isLocked;


        /// <summary>
        /// Gets lock status.
        /// </summary>
        public bool IsLocked => _isLocked;


        /// <summary>
        /// Validates and locks configuration and prevents any further changes to the configuration.
        /// </summary>
        /// <exception cref="ConfigurationException">Thrown when configuration could not be validated.</exception>
        public void Lock()
        {
            if (_isLocked)
            {
                return;
            }

            try
            {
                OnLock();
                _isLocked = true;
            }
            catch (ConfigurationException e)
            {
                throw new ConfigurationException("Could not lock invalid configuration.", e);
            }
        }

        protected abstract void OnLock();

        /// <summary>
        /// Sets field value.
        /// </summary>
        /// <exception cref="ConfigurationException">Thrown when configuration is locked.</exception>
        protected void Set<T>(ref T field, T value)
        {
            if (_isLocked)
            {
                throw new ConfigurationException("Could not modify configuration after it has been locked.");
            }

            if (!Equals(field, value))
            {
                field = value;
            }
        }
    }
}
