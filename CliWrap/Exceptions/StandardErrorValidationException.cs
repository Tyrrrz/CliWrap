﻿using System;
using CliWrap.Internal;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Thrown if underlying process reported an error.
    /// </summary>
    public class StandardErrorValidationException : Exception
    {
        /// <summary>
        /// Standard error data produced by underlying process.
        /// </summary>
        public string StandardError { get; }

        /// <summary>
        /// Initializes <see cref="StandardErrorValidationException"/> with given standard error data.
        /// </summary>
        public StandardErrorValidationException(string standardError)
            : base($"Underlying process reported an error:{Environment.NewLine}{standardError}")
        {
            StandardError = standardError.GuardNotNull(nameof(standardError));
        }
    }
}