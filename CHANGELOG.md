## 0.1.2 (01.10.2018):

### Bugfixes

* Time-based rolling strategy now won't create empty files for periods when there were no log records.


## 0.1.1 (10.09.2018):

### Bugfixes

* FileLog now creates all the directories on the way to log file (https://github.com/vostok/logging.file/issues/5)

### Enhancements

* RollingUpdateCooldown was renamed to FileSettingsUpdateCooldown to indicate that it's not exclusively purposed for rolling settings.


## 0.1.0 (06-09-2018): 

Initial prerelease.
