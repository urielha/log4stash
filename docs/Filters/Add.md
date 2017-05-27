Filters - Add
=====================

Adding new tag to the log event.

Configuration example:

```xml
<ElasticFilters>
    <Add>
        <Key>TagName</Key>
        <Value>TagValue</Value>
        
        <!-- optional -->
        <Overwrite>True</Overwrite>
    </Add>
</ElasticFilters>
```
### Properties:

#### Key
The name of the tag.<br/>
You can use [smart formatting][smart-formatting] here.

#### Value
The value.<br/>
You can use [smart formatting][smart-formatting] here.

#### Overwrite
Whether to overite or append if this tag already exists.

For example if you have tag with the name `Address` and value `first` and you are using the Add filter to add `Address` with value of `second`.<br />
If `Overwrite` is *true* - The original address will be replaced with `second`.<br />
If `Overwrite` is *false* - The tag `Address` will be replace by array with the values `['first', 'second']`.

[smart-formatting]:https://github.com/urielha/log4stash/blob/master/docs/SmartFormatting.md
