Smart formatting
=====================

Some filters supports "smart formatting".

Smart formatting is a feature similar to logstash in which you can put the content of another tag inside the key/value of new one.

### Using:
Lets say you have a tag named `Tag1` with the vlaue `Val1`<br/>
And you want to add new tag: `Tag1_ex` with the value: `Tag1 value is: '<tag1 value>'`.

By using the `Add` filter which supports smart formatting you can do:
```xml
<ElasticFilters>
    <Add>
        <Key>Tag1_ex</Key>
        <Value>Tag1 value is: '%{Tag1}'</Value>
    </Add>
</ElasticFilters>
```

The result will be:
```javascript
{
    // ...
    Tag1: 'Val1',
    Tag1_ex: 'Tag1 value is: \'Val1\''
}
```

Pay attention to this special syntax: `%{tag_name}`

This applies to the key as well: you can put `%{Tag1}` in the key and the value of Tag1 will be the name of the brand new key.

You can also use more than one tag reference:

```xml
<ElasticFilters>
    <Add>
        <!-- the value of 'my_tag' will be the key name -->
        <Key>%{my_tag}</Key>
        <Value>This is an example, Tag1:%{Tag1} and Tag2:%{Tag2}</Value>
    </Add>
</ElasticFilters>
```

### Date format

As you can see in the [configuration example][config-example], IndexName have the value of `log_test_%{+yyyy-MM-dd}`.<br />
This is a feature of smart formatters, you can put the DateTime.Now by writing a DateTime format after a '+'.

So `%{+yyyy}` will be replaced by the current year.

This applies to all filters which supports smart formatters, not just the IndexName.


[config-example]:https://github.com/urielha/log4stash#almost-full-configuration

