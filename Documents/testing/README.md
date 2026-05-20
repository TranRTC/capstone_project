# Testing

All test execution documents for this project are in **`Documents/testing/`**.  
The formal test strategy is **[007_TestingPlan.md](../007_TestingPlan.md)** (capstone document 007).

| Document | Description |
|----------|-------------|
| [007_TestingPlan.md](../007_TestingPlan.md) | Full testing strategy and test cases |
| [MANUAL_TEST_CHECKLIST.md](MANUAL_TEST_CHECKLIST.md) | Hands-on checklist: UI, SignalR, MQTT, demo |
| [AUTOMATED_TEST_RESULTS.md](AUTOMATED_TEST_RESULTS.md) | Latest automated API test run |
| [Run-ApiTests.ps1](Run-ApiTests.ps1) | PowerShell script to re-run API tests |

## Run automated API tests

```powershell
# Start the API first, then from repo root:
powershell -ExecutionPolicy Bypass -File "Documents/testing/Run-ApiTests.ps1"
```

Default: `http://localhost:5000`, login `admin` / `Admin@123`

After a run, update [AUTOMATED_TEST_RESULTS.md](AUTOMATED_TEST_RESULTS.md) with the date and pass/fail summary.
