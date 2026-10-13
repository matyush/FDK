namespace SoftFX.Extended.Errors
{
    using System;
    using System.Runtime.Serialization;
#if NETFRAMEWORK
    using System.Security.Permissions;
#endif // NETFRAMEWORK
    using SoftFX.Extended.Core;

    /// <summary>
    /// This is exception indicates that timeout of a synchronous operation has been reached.
    /// </summary>
    [Serializable]
    public class TimeoutException : RuntimeException
    {
        internal TimeoutException(string message)
            : base(HResults.FX_CODE_ERROR_TIMEOUT, message)
        {
        }

        internal TimeoutException(int waitingIntervalInMilliseconds, string operationId, string message)
            : base(HResults.FX_CODE_ERROR_TIMEOUT, message)
        {
            this.WaitingInterval = waitingIntervalInMilliseconds;
            this.OperationId = operationId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif // NETFRAMEWORK
        protected TimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.WaitingInterval = info.GetInt32("WaitingInterval");
            this.OperationId = info.GetString("OperationId");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif // NETFRAMEWORK
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("WaitingInterval", this.WaitingInterval);
            info.AddValue("OperationId", this.OperationId);
        }

        /// <summary>
        /// Gets used waiting interval in milliseconds.
        /// </summary>
        public int WaitingInterval { get; internal set; }

        /// <summary>
        /// Gets unique id of corresponding synchronous operation; see messages log for detailed information.
        /// </summary>
        public string OperationId { get; internal set; }
    }
}
