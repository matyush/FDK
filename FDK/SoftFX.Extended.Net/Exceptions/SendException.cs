namespace SoftFX.Extended.Errors
{
    using System;
    using System.Runtime.Serialization;
#if NETFRAMEWORK
    using System.Security.Permissions;
#endif // NETFRAMEWORK
    using SoftFX.Extended.Core;

    /// <summary>
    /// This exception indicates that outgoing request has not been sent.
    /// </summary>
    [Serializable]
    public class SendException : RuntimeException
    {
        internal SendException(string message)
            : base(HResults.FX_CODE_ERROR_SEND, message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif // NETFRAMEWORK
        protected SendException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
