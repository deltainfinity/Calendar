using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.API.Core.Exceptions
{
    public class ConfigurationErrorsException : Exception
    {
        public ConfigurationErrorsException()
        { }

        public ConfigurationErrorsException(string message) : base(message)
        { }

        public ConfigurationErrorsException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
