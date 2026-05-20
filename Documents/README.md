# Project Documentation

All project documentation lives under **`Documents/`**: formal specs (`001`–`010`), testing artifacts, and presentation files.

**Status (May 2026):** Documents `001`–`010` are aligned with the **as-built** codebase (ASP.NET Core 8 API on port 5000, React/MUI frontend, 11-table SQL Server schema, JWT roles, SignalR, MQTT).

## Folder layout

```
Documents/
├── README.md                 ← this index
├── 001_Overview.md … 010_UserManual.md
├── testing/                  ← test execution (checklists, results, script)
│   ├── README.md
│   ├── MANUAL_TEST_CHECKLIST.md
│   ├── AUTOMATED_TEST_RESULTS.md
│   └── Run-ApiTests.ps1
└── Presentation/             ← capstone slides and tables
    ├── Capstone Project Presentation.pptx
    ├── Capstone_System_Architecture_Diagrams.pptx
    └── Table For Powerpoint.xlsx
```

## Formal documents (read in order)

| # | Document |
|---|----------|
| 1 | [001_Overview](001_Overview.md) |
| 2 | [002_Requirements](002_Requirements.md) |
| 3 | [003_DatabaseDesign](003_DatabaseDesign.md) |
| 4 | [004_ApplicationDesign](004_ApplicationDesign.md) |
| 5 | [005_FrontendDesign](005_FrontendDesign.md) |
| 6 | [006_APIDocumentation](006_APIDocumentation.md) |
| 7 | [007_TestingPlan](007_TestingPlan.md) |
| 8 | [008_DeploymentGuide](008_DeploymentGuide.md) |
| 9 | [009_ImplementationGuide](009_ImplementationGuide.md) |
| 10 | [010_UserManual](010_UserManual.md) |

## Testing (`Documents/testing/`)

| Document | Purpose |
|----------|---------|
| [testing/README.md](testing/README.md) | Testing folder index |
| [007_TestingPlan.md](007_TestingPlan.md) | Formal test strategy and test cases (doc 007) |
| [testing/MANUAL_TEST_CHECKLIST.md](testing/MANUAL_TEST_CHECKLIST.md) | Manual test list (UI, SignalR, MQTT, demo) |
| [testing/AUTOMATED_TEST_RESULTS.md](testing/AUTOMATED_TEST_RESULTS.md) | Latest automated API test run |
| [testing/Run-ApiTests.ps1](testing/Run-ApiTests.ps1) | Re-run API tests |

```powershell
powershell -ExecutionPolicy Bypass -File "Documents/testing/Run-ApiTests.ps1"
```

## Presentation (`Documents/Presentation/`)

| File | Purpose |
|------|---------|
| [Capstone Project Presentation.pptx](Presentation/Capstone%20Project%20Presentation.pptx) | Main milestone / capstone slides |
| [Capstone_System_Architecture_Diagrams.pptx](Presentation/Capstone_System_Architecture_Diagrams.pptx) | Architecture diagrams |
| [Table For Powerpoint.xlsx](Presentation/Table%20For%20Powerpoint.xlsx) | Slide data tables |
