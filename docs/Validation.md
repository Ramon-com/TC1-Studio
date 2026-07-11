# validation

the `ValidationService` checks a BinaryObjectFile for bad data before you save. results show up in the status bar and you can expand for details.

## what it checks

| check | severity | what it means |
|---|---|---|
| null field data | error | a field has a null value reference (shouldn't happen but) |
| nan float | warning | a 4-byte field treated as float contains NaN |
| infinity float | warning | a 4-byte field treated as float contains ±Infinity |
| nan float64 | warning | an 8-byte field treated as float64 contains NaN |
| circular reference | error | a node references itself through children (detected by tracking parent refs) |
| large child count | warning | a node has >500 children, might be corruption |

## implementation

```csharp
var validation = new ValidationService();
var results = validation.Validate(binaryObjectFile);

foreach (var r in results)
{
    Console.WriteLine($"[{r.Severity}] {r.Message}");
}
```

results are exposed on `MainWindowViewModel.ValidationResults` and shown as a warning count in the status bar.

## adding checks

edit `ValidationService.ValidateField()` or `ValidateNode()`. each check returns a `ValidationResult` with severity, message, optional field hash, and node path.
