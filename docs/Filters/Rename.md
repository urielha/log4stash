Filters - Rename
=====================

Rename a given tag __name__ to new one.

Configuration example:

```xml
<ElasticFilters>
    <Add>
        <Key>TagName</Key>
        <RenameTo>TagValue</RenameTo>
        
        <!-- optional -->
        <Overwrite>True</Overwrite>
    </Add>
</ElasticFilters>
```
### Properties:

#### Key
The name of the tag.<br/>
You can use [smart formatting][smart-formatting] here.

#### RenameTo
The new tag name.<br/>
You can use [smart formatting][smart-formatting] here.

#### Overwrite
Whether to overite or append if this new tag name already exists.

See Add Filter [Overwrite Property](https://github.com/urielha/log4stash/blob/master/docs/Filters/Add.md#overwrite) for more info.


