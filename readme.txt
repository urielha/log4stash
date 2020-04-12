log4stash
=====================

Breaking changes on 3.0.0 version:
BasicAuthUsername and BasicAuthPassword no longer exist as parameters and now needs to be configured through an authentication method.
DocumentIdSource no longer exists as a parameter and now needs to be configured through IndexOperationParams
Stopped support of .net 4.0

Breaking changes on 2.0.4 version:
BasicAuthUsername and BasicAuthPassword moved under AuthenticationMethod, see config example for more information.


For full details please check log4stash github page: https://github.com/urielha/log4stash/blob/master/docs/breaking.md