using Microsoft;
using System;

namespace Tools
{
    public static class Preconditions
    {
        public static T CheckNotNull<T>([ValidatedNotNull] T value, string paramName) where T : class
        {
            if (value is null)
            {
                throw new ArgumentNullException(paramName);
            }

            return value;
        }
    }
}
