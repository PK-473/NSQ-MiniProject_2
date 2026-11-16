# Taneem - Event Validations, BPFs, Plugins and Flow

Project: NSQ Event Management System (Microsoft Dynamics 365 / Dataverse)
Owner: Taneem Sajjan (M1) | Branch: `feature/Taneem` | Publisher prefix: `trpj_`
Table owned: **Event** (`trpj_event`) plus both Business Process Flows.

---

## 1. Summary of what I built

I own 12 validation items from the FSDS for the Event table. They are implemented with four different techniques, chosen for what each rule needs.

| Technique | Count | Items |
| --- | --- | --- |
| JavaScript (Event form script) | 6 | E1, E2, E7, E11, E12, S4 |
| Business rule | 1 | E3 |
| Plugins (C#, one shared assembly) | 4 | E13, E9, E4, E6 |
| Power Automate flow | 1 | E5 |

Other work: Event table (columns, form, views), the Number of Sessions rollup, the two Business Process Flows, the plugin project skeleton, the shared connection reference, and the managed solution export.

### Why each technique was used

| Technique | Used when | Why |
| --- | --- | --- |
| JavaScript | The user needs instant feedback on the form (lock a field, show a message, clear a value) | Runs in the browser, no save needed |
| Business rule | A simple "make a field required when..." on one table | No code needed (the trainer allowed 6 business rules in the whole project) |
| Plugin | The rule must always hold (imports, API, grid edits) or needs a database lookup | Runs on the server inside the save and can block it |
| Power Automate | Something must happen when time passes, with no user saving anything | A scheduled flow is the only way to react to the clock |

---

## 2. Validation map (my 12 items)

| ID | Validation (from the FSDS) | Technique | Where |
| --- | --- | --- | --- |
| E1 | End Date read-only until Start Date is chosen; End <= Start shows an error and clears End Date | JavaScript | `Event.js` |
| E2 | When status is Completed or Cancelled, all fields are read-only | JavaScript | `Event.js` |
| E3 | Description is mandatory for Workshop or Conference | Business rule | `NSQ - Event - Description Required` |
| E4 | No overlapping events at the same location and time | Plugin | `EventOverlapValidationPlugin` |
| E5 | Only Scheduled events auto-transition to Completed | Power Automate | `NSQ - Event - Auto Complete Scheduled Events` |
| E6 | When an event is Completed, Registered registrations become No Show | Plugin | `EventCompletedNoShowPlugin` |
| E7 | Start Date cannot be in the past when creating an event | JavaScript | `Event.js` |
| E9 | An event cannot be Scheduled unless at least one session exists | Plugin | `EventScheduleRequiresSessionPlugin` |
| E11 | Event Type cannot be changed once a session exists | JavaScript | `Event.js` |
| E12 | Switch automatically between the two BPFs by Event Type | JavaScript | `Event.js` |
| E13 | Event Code in the format EN-DDMMYY-000 | Plugin | `EventCodeGeneratorPlugin` |
| S4 | Warning when no session exists and the event starts in under 24 hours | JavaScript | `Event.js` |

---

## 3. JavaScript: `JavaScript/Event/Event.js`

Namespace `NSQ.Event`. One function per validation, each with a comment naming its ID. Column names use the `trpj_` prefix and choice values are held in constants at the top of the file.

Handlers on **Event Main Form**: On Load, On Save, and On change of Start Date, End Date, Event Type and Event Status (all with "Pass execution context as first parameter" ticked).

| ID | Function(s) | How it works |
| --- | --- | --- |
| E1 | `onDateChange` | Disables End Date while Start Date is empty. If End <= Start, clears End Date and shows an alert dialog |
| E2 | `lockFormIfClosed`, `unlockFormAfterClose`, `onStatusChange` | When the status is Completed or Cancelled, disables every control. The status field stays editable until the record is saved, so a mistake can be undone; only the controls E2 itself locked are unlocked again, so read-only fields such as Event Code stay read-only |
| E7 | `onSave` | On the create form only, cancels the save with `preventDefault()` if Start Date is earlier than now |
| E11 | `lockEventTypeIfSessionsExist` | Queries the Session table through `Xrm.WebApi` for sessions of this event and disables Event Type if one exists. It does not use the Number of Sessions rollup, because the rollup updates on a schedule |
| E12 | `switchProcessByEventType`, `onEventTypeChange`, `onPostSave`, `descriptionRequiredFor` | Finds the BPF by name in the `workflow` table (no GUIDs) and calls `setActiveProcess`. See section 5 |
| S4 | `warnIfNoSessionsSoon` | If the event has no session and starts within the next 24 hours, shows a WARNING bar at the top of the form |

`onLoad` calls the checks in order, with E2 last so that its lock wins over the `setDisabled` calls in the others.

### Notes
- Dates are compared as full date-and-time values, not only dates.
- Scripts only run on the form. Imports, grid edits and API calls skip them, so the important rules that must always hold are plugins.

---

## 4. Business rule (E3)

`NSQ - Event - Description Required`, scope Entity.
- Condition: Event Type = Conference OR Event Type = Workshop.
- If true: Description is Business Required. Else: Description is not required (so the field is released when the user switches to Webinar or Corporate).

---

## 5. Business Process Flows and E12

| BPF | Used for | Stage 1 | Stage 2 |
| --- | --- | --- | --- |
| `Public Event Process` | Conference, Webinar, Workshop | Event Planning: Event Name, Event Type, Start Date, End Date, Event Owner | Session Setup: Location, Number of Sessions, Event Status |
| `Corporate Event Process` | Corporate | Event Proposal: Event Name, Event Type, Description, Event Owner | Planning & Execution: Start Date, End Date, Location, Event Status |

No stage step is marked Required, because the FSDS does not ask for it.

### E12: how the switch works
1. The script reads Event Type and picks the process name: `Corporate Event Process` for Corporate, otherwise `Public Event Process`.
2. It looks the process up by name (category 4, activated) in the `workflow` table, so no GUID is hardcoded.
3. If the form is already on that process it stops; otherwise it calls `setActiveProcess`.

### Why a new event is saved automatically
A BPF instance has to point at an existing record. Switching the process on an unsaved event failed with an error about an empty Event ID (`Guid.Empty`). So:
- On a **new** event, once Event Name and Event Type are filled, the script saves the record. The form then reloads as a saved record, `onLoad` runs, and the switch succeeds.
- If the chosen type is Conference or Workshop and Description is empty, the business rule would block that save, so the script waits for a valid save. `onPostSave` runs after every successful save and performs the switch then.
- On a **saved** event, changing Event Type switches the process directly.

Side effect: a new event can be saved before the user clicks Save.

---

## 6. Plugins (C#, assembly `NSQ.EventManagement.Plugins`)

Classes are in `Plugins/Event/`. All are in the same assembly as the other members' classes and inherit the shared `PluginBase`. They use constants at the top of each class for column names and choice values.

| Class | ID | What it does |
| --- | --- | --- |
| `EventCodeGeneratorPlugin` | E13 | Builds `EN-` + today's date (ddMMyy, UTC) + `-` + a three-digit running number. It finds the highest code starting with today's prefix and adds 1, so the first event of the day is `001` |
| `EventScheduleRequiresSessionPlugin` | E9 | On create or update, when Event Status is set to Scheduled, counts sessions for the event. If none, blocks the save with "An event cannot be Scheduled until at least one session has been added." |
| `EventOverlapValidationPlugin` | E4 | Finds other events whose time range overlaps (existing start < new end AND existing end > new start), excluding itself and Cancelled events, then compares Location (trimmed, ignoring case) in code. On Update it reads the saved record for the values that were not changed. Blocks with a message naming the other event |
| `EventCompletedNoShowPlugin` | E6 | When Event Status becomes Completed, finds registrations for that event with status Registered and sets each to No Show |

### Step registration (all Synchronous, Execution Order 1, solution `NSQEventManagementSystem`)

| Class | Message | Entity | Stage | Filtering attributes | Step description |
| --- | --- | --- | --- | --- | --- |
| `EventCodeGeneratorPlugin` | Create | `trpj_event` | PreOperation | none | NSQ Event Code Generator Create |
| `EventScheduleRequiresSessionPlugin` | Update | `trpj_event` | PreValidation | `trpj_eventstatus` | NSQ Event Schedule Requires Session Update |
| `EventScheduleRequiresSessionPlugin` | Create | `trpj_event` | PreValidation | none | NSQ Event Schedule Requires Session Create |
| `EventOverlapValidationPlugin` | Create | `trpj_event` | PreValidation | none | NSQ Event Overlap Check Create |
| `EventOverlapValidationPlugin` | Update | `trpj_event` | PreValidation | `trpj_startdate, trpj_enddate, trpj_location` | NSQ Event Overlap Check Update |
| `EventCompletedNoShowPlugin` | Update | `trpj_event` | PostOperation | `trpj_eventstatus` | NSQ Event Completed No Show Update |

No pre-image is needed: E4 reads the saved record when it needs the old values.

### Things worth knowing
- **Choice values come from the real columns.** Registration Status uses `738000000` (Registered) and `738000003` (No Show), not the `100000000` series used by the Event columns. E6 stayed silent until these constants were corrected.
- **E6 is Synchronous** so the result is visible straight after saving and a failure shows on screen. It could be Asynchronous (PostOperation allows it) if registrations were very numerous.
- **Date text in E13 uses UTC**, so between 00:00 and 05:30 IST the date part is the previous day.

---

## 7. Power Automate flow (E5)

**`NSQ - Event - Auto Complete Scheduled Events`**

| Item | Value |
| --- | --- |
| Trigger | Recurrence, every 15 minutes (time zone India) |
| Connection | Connection reference `NSQ Dataverse Connection` (inside the solution) |
| `Try` scope | `List_Scheduled_Events_Ended` > `Apply_to_each_Ended_Event` > `Update_Event_Status_To_Completed` |
| `Catch` scope | Runs if `Try` fails or times out: `Capture_Error` (`result('Try')`) then Terminate (Failed) |

List rows filter: Event Status = Scheduled **and** End Date earlier than (or equal to) the current UTC time:
`trpj_eventstatus eq 100000001 and trpj_enddate le @{utcNow()}`

Because only Scheduled events are fetched, Draft and Cancelled events never change (that is the "only Scheduled" part of E5). Each update fires the E6 plugin, so Registered registrations become No Show, and E2 locks the form on the next open.

Why a flow: nothing is saved when an End Date simply passes, so a plugin would never run. A scheduled flow is the only way to act on time passing.

Limits: an event can stay Scheduled for up to 15 minutes after its End Date; the flow runs as the connection owner.

---

## 8. Event table, form and views

- Columns: Event Name, Start Date, End Date, Location, Description, Event Type, Event Code, Event Status, Number of Sessions (rollup, count of sessions), Owner.
- Event Code is read-only (filled by the plugin); Number of Sessions is read-only (rollup, refresh icon).
- Form `Event Main Form` with the process bar, tabs General Information, Sessions, Event Registrations, Related, and a Timeline. Views `Active Events` and `All Events` with all columns.
- Table settings: auditing on, activities and notes on, business process flows on.

---

## 9. Testing done (Env 1)

| Item | Test | Result |
| --- | --- | --- |
| E1 | End Date locked until Start; End earlier than or equal to Start clears End | Passed |
| E2 | Completed or Cancelled locks the form; switching back to Draft before saving unlocks it | Passed |
| E3 | Conference or Workshop with empty Description cannot be saved | Passed |
| E7 | Past Start Date blocked on create, allowed on edit | Passed |
| E11 | Event Type locked once a session exists | Passed |
| E12 | Process bar switches by type, including new events | Passed |
| S4 | Warning when no session and starting within 24 hours | Passed |
| E13 | Codes like `EN-DDMMYY-001`, then 002 | Passed |
| E9 | Scheduled blocked without a session, allowed with one | Passed |
| E4 | Same location and overlapping time blocked | Passed |
| E6 | Registered registration became No Show on completing the event | Passed after the choice values were corrected |
| E5 | Scheduled event with a past End Date became Completed | Passed (run after the End Date had passed; a run one second before the End Date correctly returned no rows) |

---

## 10. Known limitations

- JavaScript checks (E1, E2, E7, E11, E12, S4) and the business rule apply on the form only. Tools that bypass the form (God mode, imports, grid edits, API calls) can skip them. E11 has an optional server-side plugin idea (`EventTypeLockPlugin`) that is not built.
- E9 has both an Update and a Create step, so an event cannot be created directly as Scheduled (a brand-new event has no sessions). It does not re-check when the last session of an already Scheduled event is deleted later.
- E4 does not re-check when a Cancelled event is changed back to an active status.
- E13 has no database lock, so two events created at the same instant could receive the same code.
- E12 needs a saved record, hence the automatic save on new events.

---

## 11. Git workflow used

- Work on `feature/Taneem`; one commit per validation or class with a message such as `event-plugin: block Scheduled status when the event has no session (E9)`.
- Pull request into `dev`, merged with a merge commit so each commit stays visible; `dev` goes to `main` at milestones.
- The plugin assembly is built from `dev` so it contains every member's classes; the `.csproj` must list every class file.

## 12. Deployment

Managed solution exported from Env 1 as version `1.0.0.1` and imported into Env 2 (see the team guide, section 14). The two Microsoft system solutions it depends on (App Framework Infra Extensions and App Module Assets) are standard in Dataverse environments.
