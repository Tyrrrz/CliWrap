### v1.7.5 (21-Jan-2018)

- Added `EncodingSettings` to customize stream encoding.
- Added some ReSharper annotations to improve warnings and suggestions.

### v1.7.4 (10-Jan-2018)

- Execution start and exit times are now calculated separately, without accessing `Process` instance.
- Execution start and exit times are now `DateTimeOffset` instead of `DateTime`.
- Fixed an issue that prevented CliWrap from properly working on Linux.