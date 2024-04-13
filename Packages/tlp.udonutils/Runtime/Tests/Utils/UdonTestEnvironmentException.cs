using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace TLP.UdonUtils.Tests.Utils
{
    public class UdonTestEnvironmentException : Exception
    {
        public UdonTestEnvironmentException() {
        }

        protected UdonTestEnvironmentException([NotNull] SerializationInfo info, StreamingContext context) : base(
                info,
                context) {
        }

        public UdonTestEnvironmentException(string message) : base(message) {
        }

        public UdonTestEnvironmentException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}