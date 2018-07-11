namespace Tmds.DotnetOC
{
    [System.Serializable]
    public class FailedException : System.Exception
    {
        public FailedException() { }
        public FailedException(string message) : base(message) { }
        public FailedException(string message, System.Exception inner) : base(message, inner) { }
        protected FailedException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}