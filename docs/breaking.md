
Breaking changes
=====================
<!-- ---
layout: docs
title: Breaking Changes
description: List of breaking changes
redirect_from: "/breaking/"
--- -->

### __Upgrading to 3.0.0__
* BasicAuthUsername and BasicAuthPassword no longer exists as parameters and now needs to be configured through an authentication method.
* DocumentIdSource no longer exists as a parameter and now needs to be configured through IndexOperationParams
* Stopped support of .net 4.0


### __Upgrading to 2.0.4__ 
BasicAuthUsername and BasicAuthPassword moved under AuthenticationMethod.

Old configuration:
```xml
<BasicAuthUsername>username</BasicAuthUsername>
<BasicAuthPassword>password</BasicAuthPassword>
```

Moved into *AuthenticationMethod*
```xml
<AuthenticationMethod>
    <!--For basic authentication purposes-->
    <Basic>
        <Username>Username</Username>
        <Password>Password</Password>
    </Basic>
    <!--For AWS ElasticSearch service-->
    <Aws>
        <Aws4SignerSecretKey>Secret</Aws4SignerSecretKey>
        <Aws4SignerAccessKey>AccessKey</Aws4SignerAccessKey>
        <Aws4SignerRegion>Region</Aws4SignerRegion>
    </Aws>
</AuthenticationMethod>
```

### __Upgrading to 2.0.0__ 
The namespace has been changed from _log4net.ElasticSearch_ to _log4stash_ 
So you need to change the *type* attribute in the config file:

Old configuration:
```xml
<appender name="ElasticSearchAppender" type="log4net.ElasticSearch.ElasticSearchAppender, log4stash">
 <!-- ... -->
</appender>
```

New:
```xml
<appender name="ElasticSearchAppender" type="log4stash.ElasticSearchAppender, log4stash">
 <!-- ... -->
</appender>
```

### __Upgrading to 1.0.0__ 
The definition of IElasticAppenderFilter has been changed, PrepareEvent has only one parameter and PrepareConfiguration's parameter type has changed to IElasticsearchClient.


## see also 
* [Version notes](https://github.com/urielha/log4stash/blob/master/docs/version_notes.md)

