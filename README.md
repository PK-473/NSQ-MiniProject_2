# NSQ Event Management System

A Microsoft Dynamics 365 / Dataverse solution for planning events, sessions and speakers, registering attendees, and collecting feedback. It was built by a team of four as a mini project, with 59 business validations, two Business Process Flows, a model-driven app and a managed deployment.

**Current release: version 1.0.0.2, deployed as a Managed solution.**

---

## 1. Quick facts

| Item | Value |
| --- | --- |
| Solution | NSQ Event Management System (`NSQEventManagementSystem`) |
| Publisher / prefix | NSQ Event Management / `trpj_` |
| Release | **1.0.0.2 (Managed)**. Earlier: 1.0.0.1 (first managed export) |
| App | NSQ Event Management (model-driven), visible only to System Administrator and System Customizer |
| Environments | Env 1 = development (unmanaged), Env 2 = deployment and demo (managed) |
| Tables | Event, Session, Speaker, Attendee, Event Registration, Feedback |
| Automation | 4 Power Automate flows, 6 business rules, 2 Business Process Flows |
| Code | 6 JavaScript files, 1 plugin assembly (about 22 classes) |
| Repository branches | `feature/<member>` > `dev` > `main` |

---

## 2. How the system works (the story)

1. An organiser creates an **Event** (Conference, Webinar, Workshop or Corporate). The system gives it a code like `EN-141126-001` and shows the matching process bar.
2. The organiser adds **Sessions** to the event and assigns a **Speaker** to each. The speaker gets a welcome email when a session becomes Planned.
3. Once the event has a session, it can be moved to **Scheduled**.
4. **Attendees** are registered for the event. Each registration gets a confirmation number and a confirmation email.
5. Registrations are marked **Attended** manually.
6. After the session is Completed, attendees who attended can give **Feedback** (rating 1 to 5, comments for low ratings).
7. When the event's End Date passes, a flow marks it **Completed**, and registrations that were never marked Attended become **No Show**.

---

## 3. Data model

| Table | Purpose | Key columns |
| --- | --- | --- |
| Event | A corporate or public event | Event Name, Start Date, End Date, Location, Description, Event Type, Event Code, Event Status, Number of Sessions (rollup) |
| Session | A part of an event | Session Title, Event, Start Time, End Time, Session Type, Session Description, Speaker, Session Status |
| Speaker | A person who runs a session | Full Name, Email, Phone Number, Organization, Biography, LinkedIn Profile |
| Attendee | A person who attends | First Name, Last Name, Email, Phone, Company, Job Title, Preferred Contact Method |
| Event Registration | Links an attendee to an event | Confirmation Number, Event, Attendee, Registration Date, Event Start/End Date, Registration Status |
| Feedback | An attendee's opinion of a session | Feedback Number, Session, Attendee, Rating, Comments, Submitted On |

Relationships: Event 1:N Session, Speaker 1:N Session, Event 1:N Event Registration, Attendee 1:N Event Registration, Session 1:N Feedback, Attendee 1:N Feedback.

---

## 4. The four techniques we used

| Technique | When it is used | Count |
| --- | --- | --- |
| **JavaScript** (form scripts) | Instant feedback on the form: lock a field, show a message, clear a value | 21 validations |
| **Plugins** (C#) | Rules that must always hold (even for imports and API calls) or need a database lookup | 27 validations, about 22 classes |
| **Power Automate** | Work done after a save, on a schedule, or sending email | 4 flows |
| **Business rules** | Simple "required when..." conditions on one form (the trainer allowed 6 in total) | 6 |
| Column setting | Speaker Full Name maximum length 100 | 1 |

Total: 59 validations (21 + 6 + 27 + 4 + 1).

---

## 5. Who built what

| Member | Tables | Also |
| --- | --- | --- |
| **Taneem (M1)** | Event, both Business Process Flows | Plugin project skeleton, shared connection reference, managed export |
| **Rutul (M2)** | Session, Speaker | Imports and sets up the managed solution in Env 2 |
| **Prathamesh (M3)** | Event Registration, Attendee | GitHub owner, plugin folder structure, second user in the demo |
| **Jay (M4)** | Feedback | Model-driven app, site map, auditing, test and documentation lead |

Details of Taneem's work: see `README-M1-Event.md`.

---

## 6. All 59 validations

### Event (Taneem)

| ID | Validation | Technique |
| --- | --- | --- |
| E1 | End Date read-only until Start Date; End <= Start shows an error and clears End Date | JavaScript |
| E2 | Completed or Cancelled makes all fields read-only | JavaScript |
| E3 | Description mandatory for Workshop or Conference | Business rule |
| E4 | No overlapping events at the same location and time | Plugin |
| E5 | Only Scheduled events auto-move to Completed | Power Automate |
| E6 | When an event is Completed, Registered registrations become No Show | Plugin |
| E7 | Start Date cannot be in the past when creating | JavaScript |
| E8 | No new registrations once the event is Cancelled | Plugin (registration class) |
| E9 | Cannot be Scheduled without at least one session | Plugin |
| E10 | No creating or changing sessions when the event is Completed or Cancelled | Plugin (session class) |
| E11 | Event Type locked once a session exists | JavaScript |
| E12 | Business Process Flow switches by Event Type | JavaScript |
| E13 | Event Code in the format EN-DDMMYY-000 | Plugin |

### Session and Speaker (Rutul)

| ID | Validation | Technique |
| --- | --- | --- |
| S1 | Session times must be inside the event's dates | JavaScript |
| S2 | End read-only until Start; End <= Start shows an error and clears End | JavaScript |
| S3 | Draft to Planned sends a welcome email to the speaker | Power Automate |
| S4 | Warning when no session exists and the event starts within 24 hours | JavaScript (Event form) |
| S5 | A speaker is required before Planned or Completed | Plugin |
| S6 | No session time overlap inside the same event | Plugin |
| S7 | Block deleting a session that has feedback | Plugin (shared with F3) |
| S8 | The same speaker cannot be in overlapping sessions | Plugin |
| S9 | A session cannot be Completed before its End Time | JavaScript |
| S10 | Description required for Workshop sessions | Business rule |
| S11 | No session under a Cancelled event | Plugin (shared with E10) |
| S12 | Speaker read-only once the session is Completed | JavaScript |
| SP1 | Block duplicate Email or Phone | Plugin (shared with SP5) |
| SP2 | Email format; phone starts with `+` and is numeric | JavaScript |
| SP3 | LinkedIn must be a valid URL if given | JavaScript |
| SP4 | At least one of Email or Phone | JavaScript |
| SP5 | LinkedIn Profile must be unique | Plugin |
| SP6 | Full Name maximum 100 characters | Column setting |
| SP7 | Organization required when LinkedIn is filled | Business rule |

### Event Registration and Attendee (Prathamesh)

| ID | Validation | Technique |
| --- | --- | --- |
| R1 | Fill or clear the event's Start and End Date when the event is chosen | JavaScript |
| R2 | Confirmation email when a registration is created | Power Automate |
| R3 | Block registration for a Cancelled or Completed event | Plugin (with E8 and R6) |
| R4 | No duplicate attendee for the same event | Plugin |
| R5 | Block registration if the attendee's email or phone is missing | Plugin |
| R6 | No registration after the event's Start Date | Plugin |
| R7 | A Cancelled registration cannot go back to Registered | Plugin |
| R8 | A registration cannot be deleted once Attended | Plugin |
| R9 | A Cancelled event cancels all its active registrations | Power Automate (built by Jay) |
| R10 | Confirmation Number in the format AD-EN-DDMM-0001 | Plugin |
| A1 | Block duplicate Email (case-insensitive) or Phone with a friendly message | Plugin |
| A2 | Required fields depend on Preferred Contact Method | JavaScript |
| A3 | Block saving when both Email and Phone are missing | JavaScript |
| A4 | First and Last Name must not contain digits | JavaScript |
| A5 | Company required when Job Title is entered | Business rule |
| A6 | Preferred Contact Method cannot be blank | Business rule |
| A7 | Email format; phone starts with `+` and is numeric | JavaScript |

### Feedback (Jay)

| ID | Validation | Technique |
| --- | --- | --- |
| F1 | Comments visible and editable only when Rating is below 3 | JavaScript |
| F2 | Submitted On filled automatically with the current time | Plugin |
| F3 | Block deleting a session that has feedback | Plugin (shared with S7) |
| F4 | Profanity check on comments | Plugin |
| F5 | Only attendees who Attended can submit feedback | Plugin |
| F6 | Rating between 1 and 5 | JavaScript plus column limits |
| F7 | Submitted On read-only after creation | JavaScript |
| F8 | Feedback only after the session is Completed | Plugin |
| F9 | Comments required when Rating is 1 or 2 | Business rule |
| F10 | One feedback per attendee and session, with a friendly message | Plugin |

---

## 7. Power Automate flows (4)

| Flow | What it does | Owner |
| --- | --- | --- |
| `NSQ - Event - Auto Complete Scheduled Events` | Every 15 minutes, finds Scheduled events whose End Date has passed and sets them to Completed | Taneem |
| `NSQ - Session - Send Welcome Email to Speaker` | When a session becomes Planned, creates and sends an Email record to the speaker with the title, date and time | Rutul |
| `NSQ - Registration - Send Confirmation Email` | When a registration is created, sends the attendee a confirmation email | Prathamesh |
| `NSQ - Event - Cancel Active Registrations` | When an event is Cancelled, sets its Registered registrations to Cancelled | Jay |

All flows live inside the solution, use the single connection reference `NSQ Dataverse Connection`, and have a Try and Catch scope for errors. Emails are created as Email records in Dataverse (not through an email connector), so they appear in the record's timeline.

---

## 8. Business rules (6)

| Rule | Item |
| --- | --- |
| `NSQ - Event - Description Required` | E3 |
| `NSQ - Session - Description Required` | S10 |
| `NSQ - Speaker - Organization Required` | SP7 |
| `NSQ - Attendee - Company Required` | A5 |
| `NSQ - Attendee - Contact Method Required` | A6 |
| `NSQ - Feedback - Comments Required` | F9 |

---

## 9. Business Process Flows (2)

| Process | Used for |
| --- | --- |
| Public Event Process | Conference, Webinar, Workshop (stages: Event Planning, Session Setup) |
| Corporate Event Process | Corporate (stages: Event Proposal, Planning & Execution) |

The form switches between them automatically when the Event Type changes (E12). A process can only be attached to a saved record, so a new event is saved automatically once its name and type are filled.

---

## 10. Code in this repository

```
NSQ-MiniProject_2/
  README.md                 this file
  README-M1-Event.md        details of the Event work
  JavaScript/               one script per table (Event, Session, Speaker, Attendee, EventRegistration, Feedback)
  Plugins/                  Visual Studio project, one assembly, classes grouped by table
    Common/                 PluginBase and shared names
    Event/  Session/  Speaker/  EventRegistration/  Attendee/  Feedback/
  docs/
    flows/                  one note per flow
    version-history/        JavaScript, plugin and Power Automate change logs
```

- **JavaScript:** one file per table, a namespace per file (for example `NSQ.Event`), functions named after the validation they implement.
- **Plugins:** all classes in a single assembly (`NSQ.EventManagement.Plugins`, .NET Framework 4.6.2, signed). Each class is registered as a Synchronous step in the solution. Most are PreValidation (they block a save before it happens); the confirmation number, event code and submitted-on classes are PreOperation (they fill a value just before saving); the no-show update is PostOperation.

---

## 11. App, navigation and settings

- **App:** NSQ Event Management (model-driven), visible only to System Administrator and System Customizer.
- **Navigation:** Event Planning (Events, Sessions, Speakers), Attendee Management (Attendees, Event Registrations), Engagement (Feedback).
- **Auditing:** on for Event, Session, Event Registration and Feedback.
- **Environment variables:** `NSQ Sender Email` (the user the flows send email as), and the forbidden-words list if used by the profanity check.
- **Connection reference:** `NSQ Dataverse Connection`, shared by all flows.

---

## 12. Deployment

Released as a **managed solution, version 1.0.0.2**, exported from Env 1 and imported into Env 2.

**What the import needs**
- Dataverse in the target environment, with the two Microsoft system solutions App Framework Infra Extensions and App Module Assets (standard in every environment).
- A connection for `NSQ Dataverse Connection` and a value for `NSQ Sender Email`.

**After the import**
1. Turn on the four flows.
2. Check the connection reference shows connected.
3. Approve the sender mailbox (Settings, Email, Mailboxes) and turn on auditing for the environment.
4. Confirm only System Administrator and System Customizer can see the app.

**Fixing something later**: change it in Env 1 only, raise the version, export managed, and import with **Upgrade**. Never edit components in Env 2.

| Version | Notes |
| --- | --- |
| 1.0.0.0 | Development version in Env 1 |
| 1.0.0.1 | First managed export |
| 1.0.0.2 | Current managed release |

---

## 13. Git workflow

- Each member works on their own branch: `feature/Taneem`, `feature/rutul`, `feature/Prathamesh`, `feature/jay`.
- Pull request into `dev`, reviewed by a teammate, merged with a merge commit so each member's commits stay visible. `dev` is merged into `main` at milestones.
- One commit per validation or class, with messages like `event-plugin: block Scheduled status when the event has no session (E9)`.
- Because the plugin assembly is shared, it is always built from `dev` after merging, so it contains every member's classes.

---

## 14. Demo script (about 10 steps)

1. Create an Event (Conference). The code appears, the Public Event Process bar shows, and Description is required. Switch to Corporate and the bar changes.
2. Try to set it to Scheduled: blocked, no session yet.
3. Create a Speaker (try a duplicate email: blocked).
4. Create a Session inside the event dates and assign the Speaker. Set it to Planned: a welcome email appears in the timeline.
5. Set the event to Scheduled: allowed.
6. Create an Attendee and register them. The confirmation number appears and the confirmation email is sent. Registering again is blocked.
7. Mark the registration Attended.
8. After the session is Completed, add Feedback. A rating of 2 requires comments; a rating of 4 hides the comments.
9. Edit the event's dates into the past. The scheduled flow completes it, and any Registered registration becomes No Show.
10. Cancel another event: its registrations are cancelled and new registrations are blocked.

---

## 15. Key decisions

- Status fields are custom Choice columns (Event Status, Session Status, Registration Status), not the system status.
- Date parts in the Event Code and Confirmation Number use UTC.
- The choice numbers differ between columns (Registration Status uses the 738000000 series), so always read them from the column and never assume.
- Validations use JavaScript, plugins, Power Automate or business rules only, as the FSDS allows.
- Emails are Dataverse Email records so they appear in timelines.
- No GUIDs or environment URLs are hardcoded: the BPFs are found by name, and settings live in environment variables.

## 16. Known limitations

- JavaScript checks and business rules work on the form only. Imports, grid edits and API calls can skip them, so the rules that must always hold are plugins.
- The scheduled flow runs every 15 minutes, so an event can stay Scheduled for up to 15 minutes after its End Date.
- A Business Process Flow cannot attach to an unsaved record, hence the automatic save on new events.
- The Number of Sessions rollup updates on a schedule; use the refresh icon.

## 17. Team

Taneem Sajjan, Rutul, Prathamesh and Jay, as trainees at NSQUARE XPERTS, following the Functional Specification and Design Specification Document (FSDS) for the Event Management System, version 3.0.
