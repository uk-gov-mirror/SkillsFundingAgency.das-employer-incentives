using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace SFA.DAS.EmployerIncentives.Commands.Exceptions
{
    [Serializable]
    public sealed class CommandDispatcherException : Exception
    {
        public CommandDispatcherException()
        {
        }

        public CommandDispatcherException(string message)
            : base(message)
        {
        }

        public CommandDispatcherException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        private CommandDispatcherException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
   
}
