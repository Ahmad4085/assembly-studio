# Beginner Usability Session Kit (TASK-105)

| Field | Value |
|---|---|
| Document | QA test kit: moderator script and recording sheet |
| Status | Ready for use (prepared by the agent; the session itself is run by the user) |
| Version | 1.0 |
| Last updated | 2026-11-04 |
| Verifies | AC-062, NFR-023: at least 80% of first-time participants (at least 5) open a sample, build, run and step within 5 minutes without outside help |
| Task | TASK-105 (user-run). Results go into §5 and are recorded by `/assembly-qa` |

## 1. Who takes part
- **Number:** at least **5** people. With 5 people, 4 must succeed (80%).
- **First-time participants:** people who have **never used Assembly Studio**. They may know some programming or assembly, but
  they must not have seen this IDE, its help or a demo of it.
- **One at a time,** each on their own run. Don't let a participant watch someone else's session.

## 2. Before each session (moderator)
1. Use the PC with the current Assembly Studio installed or built (record which, in §5).
2. **Reset the application** so it looks new: close it, then delete `%APPDATA%\AssemblyStudio\settings.json` (settings and
   window layout) and the folder `%LOCALAPPDATA%\AssemblyStudio\recovery` (unsaved edits). Leave the logs.
3. Start Assembly Studio and leave it on its first screen. Close any open documents.
4. Have a timer (phone stopwatch) ready. Don't open the help or the Learn sidebar for them.

## 3. What to say (read it word for word)
> "Thank you for helping. This is a program for learning assembly language. I'm testing the program, not you: if something is
> hard, that's the program's fault. Please think aloud: say what you're looking for and what you expect.
>
> Your task: **open one of the example programs that come with it, build it, run it, and then step through it one instruction
> at a time.** Any example is fine. Tell me when you think you're done. I can't help you during the task. You have up to 5
> minutes. Ready? Start."

Start the timer when you finish reading.

## 4. During the session (moderator)
- **Don't help.** No hints, no pointing, no nodding. If they ask a question, answer only: "What would you try?"
- **Outside help** means anything from you or another person. Using the application's own help, Learn sidebar, menus or tooltips
  is allowed. That's the product doing its job.
- Write down the time when each step is first done (§5): **Opened** (a sample is open in the editor), **Built** (a build
  succeeded), **Ran** (the program ran or is running), **Stepped** (at least one Step Into or Step Over executed an instruction).
- **Stop at 5:00** or when all four steps are done, whichever comes first.
- **If you had to help** (for example, the application froze), stop the timer: the session counts as **not successful**, and
  you write what happened.

**Success** for one participant: all four steps were done within 5:00, without outside help.

## 5. Recording sheet (one row per participant)
Build or installer used: ______________________ Date: ____________ Moderator: ______________________

| # | Programming background (none / some / assembly) | Opened (m:ss) | Built (m:ss) | Ran (m:ss) | Stepped (m:ss) | All four within 5:00 without help? (yes / no) | Where they hesitated or got stuck (their words if possible) |
|---|---|---|---|---|---|---|---|
| 1 | | | | | | | |
| 2 | | | | | | | |
| 3 | | | | | | | |
| 4 | | | | | | | |
| 5 | | | | | | | |

**Result:** ___ of ___ succeeded = ___ %. **AC-062 passes when at least 5 took part and the result is at least 80%.**

## 6. After the sessions
- Give the filled sheet (a photo or the typed table) to the agent with "record the usability session". The agent copies it into a
  dated QA record, sets AC-062's status (pass or fail, with the numbers), and turns every point where people got stuck into a
  defect or a proposal.
- Don't round up or fill gaps from memory: a time that wasn't noted stays blank, and a blank "Stepped" time counts as not done.
  The record has to show what actually happened.

## 7. Reference for the moderator (don't show it to participants)
What a participant has to find, so you can tell which step they're on:
- **Samples:** Learn sidebar (View menu or Ctrl+Alt+L) → a sample, for example "Hello, World", "Loops" or "Keyboard input".
- **Build:** Build menu or Ctrl+Shift+B. **Run:** F5. **Step Into:** F11. **Step Over:** F10.
- **Stepping has happened** when the current-line marker moves in the editor and the Registers panel marks changed values.

## Change log
- 2026-11-04 v1.0: Created (TASK-105, agent part). Session not yet run.
