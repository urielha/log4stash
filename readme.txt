log4stash
=====================

Important Release Note! (2.0.0)
"log4net.ElasticSearch" --> "log4stash"

The namespace "log4net.ElasticSearch" has been changed to "log4stash", 
Please make sure to change it in your log4net configuration file and any other references.


Templates:
-----------
To get to know the ElasticSearch templates read:
https://www.elastic.co/guide/en/elasticsearch/reference/current/indices-templates.html

Sample template could be found in: https://github.com/urielha/log4stash/blob/master/scripts/log-index-spec.json
More complex template with dynamic mappings can be found in the tests template: https://github.com/urielha/log4stash/blob/master/src/log4stash.Tests/template.json

You can read more about dynamic mappings here:
https://www.elastic.co/guide/en/elasticsearch/reference/current/default-mapping.html
