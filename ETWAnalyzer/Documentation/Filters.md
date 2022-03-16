# Filters

ETWAnalyzer supports for all input arguments which work as a filter a common filtering semantic.
A filter string can define multiple in- and exclusion filters.


- Filters can be combined with ; as separator e.g. *a;b*
- Exclusion are filters are supported and start with ! e.g. *!a*
- Filters support wildcards
    - The character * matches 0 or all characters. E.g. \*chrome\*
    - ? matches 0 or one character

A typical Process query which selects all chrome processes but no chromers would be

> -ProcessName \*chrome\*;!\*chromers\*

An exception filter which filters all TimeoutExceptions

> -Type \*TimeOutException\*
