log4stash - Breaking changes
=====================

* __Upgrading to 2.0.4__: BasicAuthUsername and BasicAuthPassword moved under AuthenticationMethod, see [config example][config-example] for more information.

* __Upgrading to 2.0.0__: The namespace has been changed from _log4net.ElasticSearch_ to _log4stash_ 

* __Upgrading to 1.0.0__: The definition of IElasticAppenderFilter has been changed, PrepareEvent has only one parameter and PrepareConfiguration's parameter type has changed to IElasticsearchClient.


see also [Version notes](http://urielha.github.io/log4stash/notes.html)
