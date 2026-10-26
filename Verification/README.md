# Verification

Run on Windows with .NET 9 SDK:

`dotnet run --project Verification/Verification.csproj -c Release`

The 61 checks cover LZString compatibility, JSON round trips, naming, source-relative
output folders, reuse of existing folders, invalid-input overwrite protection,
UTF-8 BOM handling, integer precision and WinForms construction.

fixtures.json contains 12 synthetic string vectors and 6 synthetic save-shaped
JSON vectors. Compressed values come from JavaScript LZString, not from the C#
implementation under test. No player saves, account identifiers or gameplay
recordings are included. See THIRD_PARTY_NOTICES.md for the algorithm attribution.

Window preview and temporary files are generated under bin/.../test-output,
which is excluded from version control.
