// Guids.cs
// MUST match guids.h

using System;

namespace EclipseKey
{
    static class GuidList
    {
        public const string guidEclipseKeyPkgString = "58b3fb67-11d2-4750-84c3-125ce2f76d6b";
        public const string guidEclipseKeyCmdSetString = "72c9448a-6bc9-4cf7-9558-22edba81548c";
        public static readonly Guid guidEclipseKeyCmdSet = new Guid(guidEclipseKeyCmdSetString);
    };
}