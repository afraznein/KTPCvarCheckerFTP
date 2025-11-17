using System;
using System.Threading.Tasks;

namespace DoDCvarCheckerFTP.Utils
{
    /// <summary>
    /// Helper class for retrying operations with exponential backoff
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Execute an async action with retry logic
        /// </summary>
        /// <typeparam name="T">Return type of the action</typeparam>
        /// <param name="action">The action to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
        /// <param name="initialDelayMs">Initial delay in milliseconds (default: 1000)</param>
        /// <param name="useExponentialBackoff">Whether to use exponential backoff (default: true)</param>
        /// <returns>Result of the action</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> action,
            int maxRetries = 3,
            int initialDelayMs = 1000,
            bool useExponentialBackoff = true)
        {
            int delayMs = initialDelayMs;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        Console.WriteLine($"Final attempt {attempt}/{maxRetries} failed: {ex.Message}");
                        throw; // Re-throw on final attempt
                    }

                    Console.WriteLine($"Attempt {attempt}/{maxRetries} failed: {ex.Message}");
                    Console.WriteLine($"Retrying in {delayMs}ms...");

                    await Task.Delay(delayMs);

                    // Exponential backoff: double the delay each time
                    if (useExponentialBackoff)
                    {
                        delayMs *= 2;
                    }
                }
            }

            // Should never reach here
            throw new InvalidOperationException("Retry logic failed unexpectedly");
        }

        /// <summary>
        /// Execute a synchronous action with retry logic
        /// </summary>
        public static T ExecuteWithRetry<T>(
            Func<T> action,
            int maxRetries = 3,
            int initialDelayMs = 1000,
            bool useExponentialBackoff = true)
        {
            int delayMs = initialDelayMs;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        Console.WriteLine($"Final attempt {attempt}/{maxRetries} failed: {ex.Message}");
                        throw; // Re-throw on final attempt
                    }

                    Console.WriteLine($"Attempt {attempt}/{maxRetries} failed: {ex.Message}");
                    Console.WriteLine($"Retrying in {delayMs}ms...");

                    System.Threading.Thread.Sleep(delayMs);

                    // Exponential backoff: double the delay each time
                    if (useExponentialBackoff)
                    {
                        delayMs *= 2;
                    }
                }
            }

            // Should never reach here
            throw new InvalidOperationException("Retry logic failed unexpectedly");
        }

        /// <summary>
        /// Execute an async action without return value with retry logic
        /// </summary>
        public static async Task ExecuteWithRetryAsync(
            Func<Task> action,
            int maxRetries = 3,
            int initialDelayMs = 1000,
            bool useExponentialBackoff = true)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await action();
                return true; // Dummy return value
            }, maxRetries, initialDelayMs, useExponentialBackoff);
        }
    }
}
