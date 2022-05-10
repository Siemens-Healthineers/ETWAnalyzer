//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest
{
    /// <summary>
    /// Helper class to check if a method throws an expected exception or and exception with a specific message substring.
    /// </summary>
    public static class ExceptionAssert
    {
        /// <summary>
        /// Assert if passed delegate does not throw an exception of passed type T
        /// </summary>
        /// <typeparam name="T">Exception type which must be thrown by delegate acc</typeparam>
        /// <param name="acc">Actual method which is called.</param>
        public static void Throws<T>(this Action acc) where T : Exception
        {
            bool exThrown = false;
            try
            {
                acc();
            }
            catch(Exception ex)
            {
                exThrown = true;
                if( !(ex is T))
                {
                    Assert.True(false, $"Expected exception of type {typeof(T).Name} but got {ex.GetType().Name} with message: {ex.Message}");
                }
            }

            if( !exThrown)
            {
                throw new InvalidOperationException("No exception was thrown!");
            }
        }

        /// <summary>
        /// Assert if wrong exception type or the message substring is not found in the thrown exception. 
        /// </summary>
        /// <typeparam name="T">Exception type to test for</typeparam>
        /// <param name="acc">Delegate which will be called for the test</param>
        /// <param name="expectedMessageSubstring">Case insensitive substring which must occur in thrown exception by acc.</param>
        public static void Throws<T>(this Action acc, string expectedMessageSubstring) where T:Exception
        {
            try
            {
                acc();
            }
            catch (Exception ex)
            {
                if (!(ex is T))
                {
                    Assert.True(false, $"Expected exception of type {typeof(T).Name} but got {ex.GetType().Name} with message: {ex.Message}");
                }

                if( ex.Message.IndexOf(expectedMessageSubstring, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    throw new InvalidOperationException($"Expected error message: {expectedMessageSubstring} but got from type {ex.GetType().Name} message {ex.Message}");
                }
            }

        }
    }
}
