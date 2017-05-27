Filters - Remove
=====================

Removes tag from the log event.

Configuration example:

```xml
<ElasticFilters>
    <Remove>
        <Key>TagName</Key>
    </Remove>
</ElasticFilters>
```
### Properties:

#### Key
The name of the tag to be removed.<br/>
You can use [smart formatting][smart-formatting] here.

[smart-formatting]:https://github.com/urielha/log4stash/blob/master/docs/SmartFormatting.md
