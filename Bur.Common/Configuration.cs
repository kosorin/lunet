namespace Bur.Common
{
    public abstract class Configuration
    {
        private bool _isLocked;

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
                Validate();
                _isLocked = true;
            }
            catch (ConfigurationException e)
            {
                throw new ConfigurationException("Could not lock invalid configuration.", e);
            }
        }

        /// <summary>
        /// Validate configuration.
        /// </summary>
        /// <exception cref="ConfigurationException" />
        public abstract void Validate();

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
