# 2026-06-28 KST Superpowers Process Impact Report

## Verdict

For Writer Workbench, broad Superpowers usage is currently a net negative.

It does not make the model intrinsically smarter. It adds process rules. In this project, the main failures have been product judgment, UI continuity, reference absorption, and exact user-intent tracking. A process bundle can help with narrow verification, but it can also make the assistant spend attention on procedure instead of the visible app.

So the practical answer is:

- Removing or sharply limiting Superpowers can make the work less stupid in this repo.
- Keeping every Superpowers rule active by default is not recommended.
- Use repo-local handoff, focused tests, and user-visible acceptance checks as the primary workflow.

## Why It Hurt Here

1. The plugin pushes procedure before product context.
   Writer Workbench is a UI-heavy writing cockpit. The critical questions are "what does the user see", "does the editor keep the manuscript whole", "does the remote/binder actually work", and "does this survive large manuscripts". Generic process rules do not answer those.

2. The plugin increases token and attention cost.
   Reading process instructions and satisfying meta-rules competes with reading the actual app, screenshots, reference docs, and current code. That is bad for this project because many regressions were surface-level UI and workflow regressions.

3. The plugin can create false confidence.
   Passing a process step is not the same as a working workbench. A test can prove an anchor exists while the app still feels broken. This repo needs tests plus visual/manual behavior checks.

4. The plugin conflicts with the user's preferred loop when used too broadly.
   The user wants direct repair, real repo changes, pingpong updates, and public sync. Long meta-explanations or rigid ceremony make the interaction worse.

## Where It Still Helps

Keep only narrow, evidence-producing use:

- TDD for a small regression test before a bugfix.
- Verification before saying build/test passed.
- Systematic debugging only when there is a concrete failure.

Do not use it as a general thinking replacement for design, layout, or product direction.

## Working Rule Going Forward

Default workflow for Writer Workbench:

1. Read the newest user instruction.
2. Check current repo state.
3. Pick the next visible broken workflow.
4. Add or update the smallest useful regression test.
5. Fix the behavior.
6. Run build/tests and the large-manuscript guardrail when relevant.
7. Update `docs/pingpong.txt`.
8. Sync the public repo.

Superpowers should not be allowed to replace steps 1 through 3.

## Immediate Next Step Applied

The next step after the binder right-click fix is remote-control policy repair:

- Native topmost remote control is the primary desktop remote.
- HTML in-window remote is hidden to avoid duplicate remotes.
- Entering the HTML workbench now shows the native remote even if it was hidden.

This directly addresses the "remote disappeared" complaint without bringing back duplicate remotes.
