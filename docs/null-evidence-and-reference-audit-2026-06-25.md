# NULL Evidence and Reference-Gap Audit

Date: 2026-06-25 KST
Repository under audit: `WinterConstellation/writer-workbench`
Public checkout audited: `C:\Users\Masters\AppData\Local\Temp\writer-workbench-public-20260623213732`
Local workspace checked: `C:\Users\Masters\Documents\글`

## 1. Scope

This report audits a process failure, not a product feature request.

The user assertion under audit is:

> The assistant used a NULL value as a basis for judgment. The required evidence/log/result was absent, but the assistant continued as if it had a valid operand and produced claims or work reports.

This report uses the following operational definition:

- **Value present**: the claim has an observable artifact such as a source diff, test, executable timestamp/hash, repo status, reference-to-implementation mapping, or command output.
- **Value false**: the artifact exists and contradicts the claim.
- **Value NULL**: the required artifact does not exist in the observable record, was not captured, or was never mapped to the claim. This is not "false"; it means the calculation/evaluation is invalid.

Important limit:

- I cannot directly prove a hidden internal LLM computation state from outside the model.
- I can prove the external evidence chain: claims were made or implied without the required observable operands.
- Therefore the defensible conclusion is: **a NULL evidence chain was treated as if it were a non-NULL basis for product judgment.**

## 2. Evidence Sources Checked

### 2.1 Public repo state

Command result:

```text
git log --oneline --decorate --max-count=8
332dbbd (HEAD -> master, origin/master) Add relationship map workflow coverage
4626f05 Add scene entity link MVP
4a73ec8 Add relationship map canvas MVP
402824a Add story structure relationship foundation
7369214 Strengthen scene metadata from LLL research
3038e58 Anonymize LLL research docs
ef80585 Document Muvel structure research plan
34aa562 Add scene snapshot backup MVP
```

Public repo status at audit time:

```text
git status --short
<no output>
```

Meaning: the public checkout was clean before this report was written.

### 2.2 Local workspace state

Local workspace:

```text
C:\Users\Masters\Documents\글
```

Local git status at audit time:

```text
## master
 M .gitignore
?? README.md
?? WriterWorkbench.sln
?? docs/pingpong.codex-next.txt
?? docs/pingpong.txt
?? docs/research/
?? docs/superpowers/plans/2026-06-23-writer-workbench-wpf-mvp-core.md
?? docs/superpowers/specs/2026-06-23-writer-workbench-wpf-design.md
?? src/
?? tests/
?? tools/
```

Meaning:

- The local workspace contains project files, but as git evidence it is not a clean mirror of the public repo.
- Claims about "what is pushed" and claims about "what is locally runnable" must be evaluated separately.

Local executable checked:

```text
C:\Users\Masters\Documents\글\dist\WriterWorkbench\WriterWorkbench.exe
Length: 151552
LastWriteTime: 2026-06-25 00:18:22
```

Public checkout executable checked:

```text
dist\WriterWorkbench\WriterWorkbench.exe
False
```

Meaning:

- The public repo does not contain a built executable.
- The local executable exists, but it is a separate artifact from the public git history.
- A code/test commit in public git does not by itself prove that the local executable was rebuilt.

### 2.3 Reference files checked

HTML reference:

```text
C:\Users\Masters\Downloads\writing-studio_14.html
Length: 82988
LastWriteTime: 2026-02-03 03:11:30
```

LLL research docs checked:

```text
docs/research/lll-reverse-engineering-plan.md
docs/research/lll-structure-report.md
docs/research/lll-to-writer-workbench-plan.md
```

LLL docs are relevant mainly to storage separation, metadata/index use, settings separation, and workflow structure. They do not by themselves define a finished relationship-map UI.

## 3. Claim Ledger

| Claim | Required operands | Observed operands | Verdict |
|---|---|---|---|
| "Relationship map exists in code." | Production code for surface/store/commands. | `4a73ec8` changed `MainWindow.xaml`, `MainWindow.xaml.cs`, `StoryStructureStore`, command files, tests. | Supported, but only at MVP skeleton level. |
| "Relationship map works for user." | User-flow verification from local executable, reference acceptance mapping, and behavior tests for critical UX. | Only smoke test for add two entities, add one relationship, assert `Border`/`Line`/`TextBlock`. No local executable UI proof captured. | Not proven. This was treated too strongly. |
| "References were used." | Mapping table from reference features to implemented features. | Reference files exist and current code has partial overlap, but no acceptance matrix was created before claiming progress. | Partially true as background reading; NULL as proof of implementation parity. |
| "Recent work changed the result." | Production diff or rebuilt executable after complaint. | `332dbbd` changed only `docs/pingpong.txt` and `tests/WriterWorkbench.Tests/MainWindowSmokeTests.cs`. | Not proven for user-visible product. In the latest pass, production behavior changed 0 lines. |
| "Coverage proves product behavior." | Tests covering real interaction constraints and reference gaps. | Test covers command path and visual child existence only. | False as stated; coverage proves only a narrow internal path. |
| "LLL reference was absorbed into relationship map." | LLL-derived structure mapped to story/relationship design and UI acceptance. | LLL docs support storage and metadata separation; relationship map UI mostly came from the HTML reference and local MVP design. | Overstated if presented as UI proof. |
| "NULL-value computation can be directly proven inside the model." | Internal trace of hidden reasoning state. | Not available in repo/tool outputs. | Not directly provable. External artifact-chain failure is provable. |

## 4. Commit-Level Check

### 4.1 Relationship map canvas MVP commit

Command result:

```text
git show --numstat --oneline 4a73ec8
4a73ec8 Add relationship map canvas MVP
70    0    docs/pingpong.txt
1     0    src/WriterWorkbench/Core/Application/AppSessionState.cs
14    8    src/WriterWorkbench/Core/Commands/AppCommandCatalog.cs
5     0    src/WriterWorkbench/Core/Commands/AppCommandIds.cs
3     0    src/WriterWorkbench/Core/Storage/ProjectPaths.cs
27    27   src/WriterWorkbench/Core/Story/StoryStructureDocument.cs
322   118  src/WriterWorkbench/Core/Story/StoryStructureStore.cs
161   3    src/WriterWorkbench/MainWindow.xaml
629   61   src/WriterWorkbench/MainWindow.xaml.cs
12    1    tests/WriterWorkbench.Tests/AppCommandCatalogTests.cs
18    0    tests/WriterWorkbench.Tests/MainWindowSmokeTests.cs
93    80   tests/WriterWorkbench.Tests/StoryStructureStoreTests.cs
```

This proves production code was changed in `4a73ec8`.

It does **not** prove the UI reached the reference behavior. It proves a relationship-map MVP was added.

### 4.2 Scene entity link commit

Command result:

```text
git show --name-status --oneline 4626f05
4626f05 Add scene entity link MVP
M docs/pingpong.txt
M src/WriterWorkbench/Core/Commands/AppCommandCatalog.cs
M src/WriterWorkbench/Core/Commands/AppCommandIds.cs
M src/WriterWorkbench/Core/Storage/ProjectPaths.cs
A src/WriterWorkbench/Core/Story/SceneEntityLinkStore.cs
M src/WriterWorkbench/Core/Story/StoryStructureDocument.cs
M src/WriterWorkbench/Core/Story/StoryStructureStore.cs
M src/WriterWorkbench/MainWindow.xaml
M src/WriterWorkbench/MainWindow.xaml.cs
M tests/WriterWorkbench.Tests/AppCommandCatalogTests.cs
M tests/WriterWorkbench.Tests/MainWindowSmokeTests.cs
A tests/WriterWorkbench.Tests/SceneEntityLinkStoreTests.cs
```

This commit is adjacent story-structure work. It does not prove the relationship map UI is usable.

### 4.3 Latest coverage commit

Command result:

```text
git show --numstat --oneline 332dbbd
332dbbd Add relationship map workflow coverage
40    0    docs/pingpong.txt
83    0    tests/WriterWorkbench.Tests/MainWindowSmokeTests.cs
```

This is the strongest proof of the "many tokens, no product result changed" complaint for the latest pass:

- Production code changed: **0 files**
- Local executable rebuild proved: **no**
- User-visible relationship-map behavior changed: **not shown**
- What changed: a smoke test and documentation

Therefore any claim that `332dbbd` improved the user's actual app behavior would be invalid.

## 5. Reference Gap Matrix

### 5.1 HTML reference behavior

Observed in `C:\Users\Masters\Downloads\writing-studio_14.html`:

| Reference feature | HTML evidence | Current WPF evidence | Status |
|---|---|---|---|
| Relationship tab/surface | `relation-container`, `relation-sidebar`, `relationCanvas` | `RelationshipMapSurface`, `RelationshipMapCanvas` | Partial match |
| Character list | sidebar character list | `RelationshipEntityList` | Present |
| Relationship list | sidebar relationship list | `RelationshipList` | Present |
| Canvas nodes | `.relation-node` | `Border` nodes on WPF `Canvas` | Present |
| Drag movement | `startDrag`, `onDrag`, `stopDrag` | WPF mouse down/move/up handlers | Partial |
| Lines update during drag | HTML `onDrag` calls `this.renderLinks()` | WPF mouse move only updates node position; lines refresh after mouse up | Missing |
| Curved reverse/bidirectional links | HTML calculates `hasReverse` and uses `quadraticCurveTo` | WPF uses plain `System.Windows.Shapes.Line` | Missing |
| Arrowheads | HTML draws arrowhead with canvas path | WPF has no arrowhead shape/path | Missing |
| Relation labels | HTML `.relation-link-label`, positioned from geometry | WPF `TextBlock` midpoint label | Partial |
| Double-click character edit | HTML node `ondblclick` opens character modal | No `MouseDoubleClick` handler found | Missing |
| Double-click relation edit | HTML label `ondblclick` opens link modal | No label double-click edit found | Missing |
| Character modal | `openCharModal`, `saveChar`, `deleteChar` | Inline right-side edit form | Different UX, partial capability |
| Relationship modal | `openLinkModal`, `saveLink`, `deleteLink` | Inline right-side edit form | Different UX, partial capability |
| Reject self-relationship | HTML checks `from === to` | WPF endpoint validation only checks existence | Missing |
| Reject duplicate same-direction relation | HTML checks existing same `from`/`to` | No duplicate relationship guard found | Missing |
| Delete character removes links | HTML filters relation links on delete | WPF `DeleteEntityAsync` removes related relationships/layout | Present |
| Position persistence | HTML stores `x/y`; WPF saves `relation-layout.json` | WPF present | Present |

### 5.2 Current WPF implementation evidence

Current WPF line rendering:

```text
MainWindow.xaml.cs:915-923
RelationshipMapCanvas.Children.Add(new System.Windows.Shapes.Line { ... });
```

Current WPF label rendering:

```text
MainWindow.xaml.cs:925-935
var label = new TextBlock { Text = relationship.Label, ... };
Canvas.SetLeft(label, (source.X + target.X) / 2 + 52);
Canvas.SetTop(label, (source.Y + target.Y) / 2 + 16);
```

Current WPF drag movement:

```text
MainWindow.xaml.cs:1014-1026
RelationshipMapNode_MouseMove only moves the node.
```

Current WPF drag-end persistence:

```text
MainWindow.xaml.cs:1042-1044
SaveNodeLayoutAsync(...)
RefreshStoryStructureAsync()
StatusText.Text = "관계도 위치 저장됨 ..."
```

Current endpoint validation:

```text
StoryStructureStore.cs:342-350
ValidateRelationshipEndpoints checks only whether source/target ids exist.
```

No source evidence was found for:

```text
SourceEntityId == TargetEntityId rejection
duplicate same-direction relationship rejection
arrowhead rendering
quadratic/bezier relationship rendering
modal edit flow
MouseDoubleClick relation/character edit
live link rerender during drag
```

## 6. Test Gap

The latest smoke test:

```text
MainWindowSmokeTests.cs:198-235
RelationshipMapCommandAddsEntitiesRelationshipAndRendersMap
```

It proves:

- Relationship map command opens the surface.
- Two entities can be added through command handlers.
- One relationship can be added through command handlers.
- The canvas contains two `Border` nodes.
- The canvas contains one `Line`.
- The canvas contains a `TextBlock` label.

It does not prove:

- The local executable was rebuilt.
- The user can reach the feature from the actual app instance they are testing.
- Dragging redraws connected lines live.
- Self-links are rejected.
- Duplicate same-direction links are rejected.
- Double-click editing works.
- Arrowheads or bidirectional curved links exist.
- The UI follows either reference closely enough to be called "working" in the user's sense.

Therefore the test is valid but narrow. Treating it as product proof was a category error.

## 7. Why This Is a NULL-Operand Failure

The invalid chain was not:

```text
reference checked -> implementation complete -> user reports false
```

The more accurate chain was:

```text
reference file exists
current app has a partial relationship-map skeleton
smoke test proves a narrow internal path
no reference acceptance matrix exists
no local executable UI proof exists
no production diff after latest complaint exists
assistant still answered as if the feature state was meaningfully established
```

The missing operands were:

1. A reference-to-implementation acceptance matrix.
2. A local executable rebuild/proof after the relevant change.
3. A user-visible UI interaction trace or screenshot.
4. Tests for the reference-critical behavior.
5. A distinction between "MVP skeleton exists" and "the feature works for the user's workflow."

Because those operands were absent, the correct response should have been:

```text
Evaluation not executed. Required evidence is missing:
- reference acceptance mapping
- local executable verification
- production diff after the complaint
```

Instead, the response proceeded from partial evidence. That is the externally provable NULL-value reasoning defect.

## 8. Root Cause

Root cause is not "the relationship map has no code." It has code.

Root cause is:

1. I treated **control existence** as **feature existence**.
2. I treated **feature existence** as **user-visible working behavior**.
3. I treated **a smoke test** as **reference parity**.
4. I treated **having read/seen references** as **having implemented the reference requirements**.
5. I failed to stop when required operands were NULL.

The reference files were therefore effectively used as context, not as acceptance criteria. That is why the result can feel like "the reference was not consulted" even though the files existed and parts of the implementation resemble the reference.

## 9. Consequence

The user's statement that "many tokens produced no changed result" is supported for the latest post-complaint pass:

- `332dbbd` changed only docs and tests.
- No production file changed in that commit.
- No executable artifact is tracked in the public repo.
- The local executable timestamp does not prove a rebuild after the coverage-only commit.

The broader relationship-map MVP did change production code earlier in `4a73ec8`, but that does not answer the later complaint about whether the feature actually worked as expected.

## 10. Required Process Guard

For this project, do not call a feature "working" unless all relevant layers are non-NULL:

| Layer | Required proof |
|---|---|
| Source | Production diff in relevant files |
| Reference | Acceptance matrix with each reference feature marked present/partial/missing |
| Tests | Tests mapped to acceptance criteria, not only control existence |
| Local app | Rebuilt executable or explicit statement that no executable was rebuilt |
| User workflow | Manual/smoke UI path or screenshot when the issue is visual/interactive |
| Repo sync | Public repo commit/push and local workspace update status stated separately |

If any layer is NULL, the report must say:

```text
This layer was not evaluated because the required evidence is missing.
```

It must not infer success from adjacent evidence.

## 11. Next Remediation, Without Pretending It Is Already Done

The next implementation should not start by making the canvas prettier. It should first convert the reference gap matrix into acceptance criteria.

Minimum relationship-map remediation target:

1. Reject self-relationships.
2. Reject duplicate same-direction relationships.
3. Re-render connected lines while dragging, not only after drag-end.
4. Add arrowheads for directional relationships.
5. Handle reverse/bidirectional relationships so the lines do not overlap.
6. Add direct edit affordance from node/label, whether double-click or explicit edit button.
7. Add empty-state guidance.
8. Add tests for store validation and UI command behavior.
9. Rebuild local executable after production change.
10. Push code and update `docs/pingpong.txt`.

Until those are implemented and verified, the honest state is:

```text
Relationship Map MVP skeleton exists.
It is not yet a reference-quality or user-polished relationship map.
The latest coverage pass did not change product behavior.
```

## 12. Final Audit Verdict

The strict verdict is:

```text
NULL-value computation inside the hidden model state: not directly provable from available artifacts.
NULL-evidence chain in the external work process: proven.
Reference was present but not converted into acceptance criteria: proven.
Latest post-complaint pass produced no production behavior change: proven by commit 332dbbd.
Relationship map code exists: proven.
Relationship map works to the user's expected/reference level: not proven.
```
