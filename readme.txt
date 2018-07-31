log4stash - breaking changes
============================

Important!!
Breaking changes on 2.3.0 version:
	**IndexAsync** was True and now False by default


Old:

Breaking changes on 2.0.4 version:
	BasicAuthUsername and BasicAuthPassword moved under AuthenticationMethod, see config example for more information.

Breaking changes on 2.0.0 version:
	"log4net.ElasticSearch" --> "log4stash"

	The namespace "log4net.ElasticSearch" has been changed to "log4stash", 
	Please make sure to change it in your log4net configuration file and any other references.

For full details please check log4stash github page: https://github.com/urielha/log4stash/blob/master/docs/breaking.md