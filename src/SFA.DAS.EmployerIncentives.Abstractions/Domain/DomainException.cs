using System;
using System.Runtime.Serialization;

namespace SFA.DAS.EmployerIncentives.Abstractions.Domain
{
    [Serializable]
    public abstract class DomainException : Exception
    {
        protected DomainException() { }
        protected DomainException(string message) : base(message) { }
        protected DomainException(string message, Exception innerException) : base(message, innerException) { }
        protected DomainException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
