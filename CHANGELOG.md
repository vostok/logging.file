## 1.0.15 (22.09.2021):

Enabled `UseSeparateFileOnConflict` by default.

## 1.0.14 (26.06.2021):

- Fixed memory leak

## 1.0.13 (21.06.2021):

- Added `FileLogSettings` cache to protect from frequent `logSettingsProvider` calls.
- Now it's possible to refresh all settings in all caches through `RefreshSettings` method.

## 1.0.12 (14.12.2020):

- Default `FileShare` mode changed to `Read`
- Added `UseSeparateFileOnConflict` setting


## 1.0.11 (29.09.2020):

Optimized rendering of unstructured log events without actual templating in messages.

## 1.0.9 (29.09.2020):

FileLog internals no longer retain references to LogEvents that have already been written.

## 1.0.8 (22.07.2020):

Customizable separator between base log file path and rolling strategy suffix.

## 1.0.7 (25.06.2020):

Slight performance improvements.

## 1.0.6 (11.04.2020):

Now using `AppDomain.CurrentDomain.BaseDirectory` as the base to resolve relative paths.

## 1.0.5 (06.02.2020):

Implemented https://github.com/vostok/logging.file/issues/11

## 1.0.4 (18.10.2019):

Fixed lowerCamelCase `WellKnownProperties`.

## 1.0.3 (23.08.2019):

Fixed https://github.com/vostok/logging.file/issues/9

## 1.0.2 (09.04.2019):

FileLog.Log now does not write events with disabled log levels.

## 1.0.0 (11.03.2019):

* Breaking change: ForContext() is now hierarchical.
* FileLog no longer allows to specify file paths pointing to directories.
* FileLog's resources (such as file handle) will now eventually be recycled if it's leaked without being disposed of.

## 0.1.3 (16.01.2019)

* FileLog.Dispose() now also performs a Flush() call if anything has been written by this log instance.

## 0.1.2 (01.10.2018):

* Time-based rolling strategy now won't create empty files for periods when there were no log records.

## 0.1.1 (10.09.2018):

* FileLog now creates all the directories on the way to log file (https://github.com/vostok/logging.file/issues/5).
* RollingUpdateCooldown was renamed to FileSettingsUpdateCooldown to indicate that it's not exclusively purposed for rolling settings.

## 0.1.0 (06-09-2018): 

Initial prerelease.
