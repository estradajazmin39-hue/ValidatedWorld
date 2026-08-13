# CLAUDE.md

Read and follow the repository guidance in the root repository agents file, `/AGENTS.md`.

For implementation work, read and update
`/docs/implementation_execution_plan.md`. Implement only its Current task. A
successful handoff records the completed evidence and fully specifies the next
task.

A human starts each run. Complete the task or report why it failed, then stop. Do
not begin the next task in the
same run. Do not perform Git state-changing or remote operations; leave edits in
the supplied working tree for the human.

Before completing changes, run the assignment-specific checks and the full
restore, build, and test commands from `/AGENTS.md`. Do not substitute manual
inspection for automated acceptance. If repair attempts keep cycling through the
same failure, report the evidence to the human and stop.

Use the realistic TechnicalProject scenario required by the plan. From WP3
onward, perform and report an actual black-box agent walkthrough through the
built public CLI against a disposable database in addition to scripted tests.

If Current task is `None`, make no changes. Report that the planned work is
finished, ask what the human wants next, and stop.
