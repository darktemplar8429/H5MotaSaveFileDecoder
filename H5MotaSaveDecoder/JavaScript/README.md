# Embedded LZString

Source: https://github.com/pieroxy/lz-string/blob/1.5.0/libs/lz-string.js
Version: 1.5.0 (upstream source header still says 1.4.5).
Upstream SHA-256: 550034b821027008776cb9bcd1700bb5d31e8b3de224cb130fc966a75dab0e5e

The source is embedded as a .NET resource and executed by Jint. It is never
loaded from a selected save or from the current working directory.

Local changes only add invalid/truncated input checks and the existing
64 * 1024 * 1024 UTF-16 code-unit decompression limit before appending output.
Compression and valid-stream decoding algorithms are unchanged.
The C# adapter checks Base64 characters and removes optional trailing padding.
Each operation uses its own engine, with a two-minute execution timeout.

The 1.5.0 package declares MIT; the historical source header mentions WTFPL.
Both upstream notices are retained in THIRD_PARTY_NOTICES.md.
