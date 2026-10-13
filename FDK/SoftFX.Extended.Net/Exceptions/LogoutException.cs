namespace SoftFX.Extended.Errors
{
    using System;
    using System.Runtime.Serialization;
#if NETFRAMEWORK
    using System.Security.Permissions;
#endif // NETFRAMEWORK
    using SoftFX.Extended.Core;

    /// <summary>
    /// This exception indicates that a synchronous call has been interrupted by logout event.
    /// </summary>
    [Serializable]
    public class LogoutException : RuntimeException
    {
        internal LogoutException(string message)
            : base(HResults.FX_CODE_ERROR_LOGOUT, message)
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
        protected LogoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
