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
