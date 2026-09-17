using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Services
{
    public class TransactionResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; } = string.Empty;

        private TransactionResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }
        public static TransactionResult Success()
        {
            return new TransactionResult(true, string.Empty);
        }
        public static TransactionResult Failure(string errorMessage)
        {
            return new TransactionResult(false, errorMessage);
        }
    }
}
