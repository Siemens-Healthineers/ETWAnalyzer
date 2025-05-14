# -LoadSymbol
If you collect data offline or behind a firewall and you do not want to send around large ETL files you can also extract CPU data
without symbol server support. You will get then from a a multi GB .etl file a small just a few MB sized json7z file which can be shared
even per EMail. 

At the remote site you can still look up method names by rewriting the Json file with a remote symbol server. 

```
C>EtwAnalyzer -LoadSymbol -fd 15_12_24.13LongTrace.json7z -symserver ms 
Processing file 1/1 15_12_24.13LongTrace.json7z
Resolved 412 pdbs. Still missing: 14
Resolved 2465 methods.
```

## Limitations
Currently only CPU data is supported. 
Late PDB resolution is currently not supported for 

- Exception
- Handle 

data.

