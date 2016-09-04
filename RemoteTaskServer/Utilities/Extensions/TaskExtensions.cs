#region

using System;
using System.Threading.Tasks;

#endregion

namespace UlteriusServer.Utilities.Extensions
{
    public static class TaskExtensions
    {
        // Attempts to dispose of a Task, but will not propagate the exception.  
        // Returns false instead if the Task could not be disposed.
        public static bool TryDispose(this Task source, bool shouldMarkExceptionsHandled = true)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            try
            {
                // no sense attempting to dispose unless we are completed, otherwise we know we'll throw
                // and why add the overhead.
                if (source.IsCompleted)
                {
                    if (shouldMarkExceptionsHandled)
                    {
                        // handle all parts of aggregate exception (true == handled)
                        source.Exception?.Flatten().Handle(x => true);
                    }

                    source.Dispose();
                    return true;
                }
            }

            catch (Exception)
            {
                // consume any other possible exception on dispose so dispose is as safe as possible
            }

            // return false if any exception occurred or because task has not yet completed.
            return false;
        }
    }
}