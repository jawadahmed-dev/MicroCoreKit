using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MicroCoreKit.Helpers
{
    public class Result<T>
    {
        public HttpStatusCode StatusCode { get; private set; }
        public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode <= 299;
        public T Value { get; private set; }
        public List<string> Messages { get; private set; }

        private Result(HttpStatusCode statusCode, T value, IEnumerable<string> messages)
        {
            StatusCode = statusCode;
            Value = value;
            Messages = messages?.ToList() ?? new List<string>();
        }

        // Private method for generating default status code messages
        private static string GetDefaultMessage(HttpStatusCode statusCode, string context = null)
        {
            string baseMessage = statusCode switch
            {
                HttpStatusCode.OK => "Operation completed successfully.",
                HttpStatusCode.Created => "Resource created successfully.",
                HttpStatusCode.BadRequest => "Invalid request data provided.",
                HttpStatusCode.Unauthorized => "Authentication is required to access this resource.",
                HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
                HttpStatusCode.NotFound => "The requested resource was not found.",
                HttpStatusCode.Conflict => "A conflict occurred with the current state of the resource.",
                HttpStatusCode.InternalServerError => "An unexpected error occurred on the server.",
                _ => $"An operation resulted in status code {(int)statusCode}."
            };

            return string.IsNullOrWhiteSpace(context) ? baseMessage : $"{baseMessage} {context}";
        }

        // Factory method for success with a value and optional custom message
        public static Result<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK, string message = null, string context = null)
        {
            if ((int)statusCode < 200 || (int)statusCode > 299)
                throw new ArgumentException("Success status codes must be in the 200-299 range.", nameof(statusCode));

            var messages = string.IsNullOrWhiteSpace(message)
                ? new List<string> { GetDefaultMessage(statusCode, context) }
                : new List<string> { message };
            return new Result<T>(statusCode, value, messages);
        }

        // Factory method for failure with custom messages
        public static Result<T> Failure(HttpStatusCode statusCode, IEnumerable<string> messages)
        {
            if ((int)statusCode >= 200 && (int)statusCode <= 299)
                throw new ArgumentException("Failure status codes must be outside the 200-299 range.", nameof(statusCode));
            if (messages == null || !messages.Any())
                throw new ArgumentException("At least one message is required for a failure result.", nameof(messages));

            return new Result<T>(statusCode, default, messages);
        }

        // Factory method for failure with a single default message and optional context
        public static Result<T> Failure(HttpStatusCode statusCode, string context = null)
        {
            if ((int)statusCode >= 200 && (int)statusCode <= 299)
                throw new ArgumentException("Failure status codes must be outside the 200-299 range.", nameof(statusCode));

            return new Result<T>(statusCode, default, new List<string> { GetDefaultMessage(statusCode, context) });
        }

        // Factory method for failure with a single custom message (renamed to avoid conflict)
        public static Result<T> FailureWithMessage(HttpStatusCode statusCode, string message)
        {
            if ((int)statusCode >= 200 && (int)statusCode <= 299)
                throw new ArgumentException("Failure status codes must be outside the 200-299 range.", nameof(statusCode));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty.", nameof(message));

            return new Result<T>(statusCode, default, new List<string> { message });
        }

        // Add a message after creation
        public Result<T> WithMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Messages.Add(message);
            return this;
        }
    }
}