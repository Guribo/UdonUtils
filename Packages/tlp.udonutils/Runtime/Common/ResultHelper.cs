using VRC.SDK3.Data;

namespace TLP.UdonUtils.Runtime.Common
{
    /// <summary>
    /// Provides utility methods for handling and creating standardized result DataDictionaries
    /// containing success or error states and corresponding values or errors.
    /// </summary>
    public static class ResultHelper
    {
        private const string SuccessKey = "success";
        private const string ValueKey = "value";
        private const string ErrorKey = "error";

        /// <summary>
        /// Converts the given value DataToken into a successful result DataDictionary.
        /// </summary>
        /// <param name="value">The DataToken representing the value to be included in the successful result DataDictionary.</param>
        /// <returns>A DataDictionary where the operation is marked as successful with the specified value.</returns>
        public static DataDictionary Ok(DataToken value = default) {
            var result = new DataDictionary
            {
                    [SuccessKey] = true,
                    [ValueKey] = value
            };
            return result;
        }

        /// <summary>
        /// Converts the given error DataToken into a failure result DataDictionary.
        /// </summary>
        /// <param name="error">The DataToken representing the error to be included in the failure result DataDictionary.</param>
        /// <returns>A DataDictionary where the operation is marked as unsuccessful with the specified error.</returns>
        public static DataDictionary Err(DataToken error) {
            var result = new DataDictionary
            {
                    [SuccessKey] = false,
                    [ErrorKey] = error
            };
            return result;
        }

        /// <summary>
        /// Converts the current DataToken to an error result DataDictionary.
        /// </summary>
        /// <param name="error">The DataToken representing the error to be converted into a result DataDictionary.</param>
        /// <returns>A DataDictionary where the operation is marked as unsuccessful with the specified error.</returns>
        public static DataDictionary ToErr(this DataToken error) {
            return Err(error);
        }

        /// <summary>
        /// Determines whether a result DataDictionary represents a successful operation.
        /// </summary>
        /// <param name="result">The result DataDictionary to evaluate.</param>
        /// <returns>True if the result represents a successful operation; otherwise, false.</returns>
        public static bool IsOk(this DataDictionary result) {
            return result != null && result.TryGetValue(SuccessKey, out var success) && success.Boolean;
        }

        /// <summary>
        /// Determines whether a result DataDictionary represents a failed operation.
        /// </summary>
        /// <param name="result">The result DataDictionary to evaluate.</param>
        /// <returns>True if the result represents a failed operation; otherwise, false.</returns>
        public static bool IsErr(this DataDictionary result) {
            return result == null || !result.TryGetValue(SuccessKey, out var success) || !success.Boolean;
        }

        /// <summary>
        /// Extracts the value from a result DataDictionary if it represents a successful operation.
        /// </summary>
        /// <param name="result">The result DataDictionary containing the value to unwrap if the operation is successful.</param>
        /// <returns>The extracted value if the result represents a successful operation; otherwise, a default DataToken representing a missing key.</returns>
        public static DataToken Unwrap(this DataDictionary result) {
            return result != null && result.TryGetValue(ValueKey, out var value)
                    ? value
                    : new DataToken(DataError.KeyDoesNotExist);
        }

        /// <summary>
        /// Extracts the error value from a result DataDictionary if it represents a failed operation.
        /// </summary>
        /// <param name="result">The result DataDictionary potentially containing the error value to unwrap.</param>
        /// <returns>The extracted error value if the operation is unsuccessful; otherwise, a default DataToken representing a missing key.</returns>
        public static DataToken UnwrapErr(this DataDictionary result) {
            return result != null && result.TryGetValue(ErrorKey, out var error)
                    ? error
                    : new DataToken(DataError.KeyDoesNotExist);
        }

        /// <summary>
        /// Attempts to extract the value from a result DataDictionary if it represents a successful operation.
        /// </summary>
        /// <param name="result">The result DataDictionary potentially containing the value to unwrap.</param>
        /// <param name="value">The extracted value if the operation is successful; otherwise, it contains a default value.</param>
        /// <returns>True if the result represents a successful operation and contains a value; otherwise, false.</returns>
        public static bool TryUnwrap(this DataDictionary result, out DataToken value) {
            if (result.IsOk() && result.TryGetValue(ValueKey, out value)) {
                return true;
            }

            value = new DataToken(DataError.KeyDoesNotExist);
            return false;
        }

        /// <summary>
        /// Attempts to extract the error value from a result DataDictionary if it represents a failed operation.
        /// </summary>
        /// <param name="result">The result DataDictionary potentially containing the error to unwrap.</param>
        /// <param name="error">The extracted error value if the operation is a failure; otherwise, it contains a default value.</param>
        /// <returns>True if the result represents a failed operation and contains an error value; otherwise, false.</returns>
        public static bool TryUnwrapErr(this DataDictionary result, out DataToken error) {
            if (result != null && result.IsErr() && result.TryGetValue(ErrorKey, out error)) {
                return true;
            }

            error = new DataToken(DataError.KeyDoesNotExist);
            return false;
        }


        /// <summary>
        /// Extracts a reference of type T from a result DataDictionary if it represents a successful operation
        /// and contains a reference value of the specified type.
        /// </summary>
        /// <param name="result">The result DataDictionary containing the reference to unwrap if the operation is successful.</param>
        /// <param name="reference">The out parameter that will hold the extracted reference if successful, or null otherwise.</param>
        /// <typeparam name="T">The expected type of the reference stored in the DataDictionary.</typeparam>
        /// <returns>True if the result represents a successful operation and contains a reference of the specified type; otherwise, false.</returns>
        public static bool UnwrapRef<T>(this DataDictionary result, out T reference) where T : class {
            var value = result.Unwrap();
            if (value.TokenType != TokenType.Reference) {
                reference = null;
                return false;
            }

            reference = value.Reference as T;
            return true;
        }

        /// <summary>
        /// Extracts the error value as a reference type from a result DataDictionary if it represents a failed operation.
        /// </summary>
        /// <param name="result">The result DataDictionary potentially containing the error reference to unwrap.</param>
        /// <param name="reference">When the method returns, contains the unwrapped error reference cast to the specified type, or null if the cast fails or the result does not contain an error reference.</param>
        /// <typeparam name="T">The type to which the error reference should be cast.</typeparam>
        /// <returns>True if the error value is successfully unwrapped and cast to the specified type; otherwise, false.</returns>
        public static bool UnwrapErrRef<T>(this DataDictionary result, out T reference) where T : class {
            var value = result.UnwrapErr();
            if (value.TokenType != TokenType.Reference) {
                reference = null;
                return false;
            }

            reference = value.Reference as T;
            return true;
        }
    }
}