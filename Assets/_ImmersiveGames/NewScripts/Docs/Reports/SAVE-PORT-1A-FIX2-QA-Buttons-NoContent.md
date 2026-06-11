# SAVE-PORT-1A-FIX2 — QA buttons for no-content activity

Status: `APPLIED / PENDING COMPILE + SMOKE`.

## Context

After `SAVE-PORT-1A-FIX1`, the modular save/load/restore path produced the expected observability for `activity_01`, but the QA snapshot buttons were disabled when the current activity was `activity_02`.

`activity_02` is an explicit no-content activity. The QA buttons must remain clickable in `ActivityRunning` so the canonical pipeline can classify the operation as `SkippedNoContent`, instead of hiding the evidence behind a UI guard.

## Change

Updated `SessionActivityDebugPanel`:

- Snapshot QA buttons are now enabled when:
  - host state is available;
  - no pending operation is active;
  - current identity is valid;
  - current stage is `ActivityRunning`.
- `HasGameplayContent` is no longer a button guard.
- The GUI label now exposes guard reasons:
  - `captureGuard='ready_with_content'`
  - `captureGuard='ready_no_content_expected_skip'`
  - `saveGuard='...'`
- `Capture + Save Snapshot Envelope (QA)` continues to save when capture returns false on no-content, so `RouteActivitySaveQaSave` can emit `SkippedNoContent`.
- `RouteActivitySaveQaSubmitted` now reports `outcomeKind='Skipped'` when the operational pipeline returns a `qa_save_skipped_*` reason.

## Expected smoke evidence

For `activity_02`:

```text
Snapshot QA: canCapture='True' captureGuard='ready_no_content_expected_skip' canSave='True' saveGuard='ready_no_content_expected_skip' contentMode='None' hasGameplayContent='False' stage='ActivityRunning'
QaCapabilitySnapshotEnvelopeCapture checkpointStatus='SkippedNoContent'
RouteActivitySaveQaSave checkpointStatus='SkippedNoContent'
RouteActivitySaveQaSubmitted outcomeKind='Skipped'
```

For `activity_01`, the existing positive path must remain:

```text
RouteActivitySaveSnapshotLoad checkpointStatus='Passed'
ActivityEntrySnapshotRestoreReady loadedSnapshotPayload='true'
ActivityObjectSnapshotRestore checkpointStatus='Passed'
```
