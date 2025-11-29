# Documenting some stuff I find

Filters have a property named "howCreated", and it very much influences how they are seen in PowerBI, and how they are saved in the Layout file.

0 => Can be seen in the PowerBI report. The filter was already there, but I added a condition to it.
1 => ? Can be seen in the PowerBI report

4 => Can be seen in powerBI, in the Json these don't have an "expression" property. instead, they have a "filterExpressionMetadata", and there can be many expressions : filterExpressionMetadata.expressions[].
5 => Invisible in PowerBI. Don't know why they are there.

https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.2.0/schema.json

https://github.com/microsoft/json-schemas/tree/main/fabric/item/report/definition

