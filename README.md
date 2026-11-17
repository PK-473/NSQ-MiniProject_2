<<<<<<< HEAD
# M4 (Jay) \- Feedback Validations, App Shell, Auditing, Plugins and Flow

**Project:** NSQ Event Management System (Microsoft Dynamics 365 / Dataverse)

**Owner:** Jay Kadam (M4)

**Branch:** feature/jay

**Publisher prefix:** trpj\_

**Primary table owned:** **Feedback** (trpj\_feedback)

**Additional ownership:** Model\-driven app shell, sitemap, auditing, shared plug\-in assembly support, testing, documentation, and R9 Power Automate flow

---

## 1\. Summary of What I Built

M4 owns the Feedback table validations and several shared application\-level responsibilities.

### Validation implementation summary

| Technique | Count | Requirements |
| --- | --- | --- |
| JavaScript | 2 | F1, F7 |
| Business Rule | 1 | F9 |
| Dataverse column configuration | 1 | F6 |
| C\# plug\-ins | 5 | F2, F4, F5, F8, F10 |
| Shared C\# plug\-in | 1 | S7/F3 |
| Power Automate | 1 | R9 |

### Other M4 work

- Feedback table, columns, form and views
- Model\-driven app shell
- Sitemap with Area, Groups and six table Sub\-areas
- Application access for System Administrator and System Customizer
- Auditing for Event, Session, Event Registration and Feedback
- Shared plug\-in assembly registration in Plugin Registration Tool
- Plug\-in tracing and profiling
- Shared Dataverse connection\-reference planning
- Validation tracking, regression testing and documentation

---

## 2\. Why Each Technology Was Used

| Technique | Used when | Reason |
| --- | --- | --- |
| JavaScript | Immediate form behavior is required | Runs in the browser and gives instant feedback without waiting for save |
| Business Rule | Simple conditional requirement on the same table | No\-code implementation and automatically reverses the required state |
| Dataverse column configuration | A standard data\-type or numeric limit is sufficient | Enforces the configured limit without duplicate custom code |
| C\# plug\-in | Validation must apply to form saves, imports, APIs and integrations | Runs server\-side and can block invalid create, update or delete operations |
| Power Automate | A related group of Dataverse records must be updated after a business event | Supports automated multi\-row processing and solution\-aware deployment |
| Environment Variable | Configuration may change between environments | Removes configuration such as forbidden words from the compiled plug\-in |
| Connection Reference | Solution flows need portable authenticated connector mapping | Allows each target environment to map the imported reference to a local connection |

---

## 3\. Validation Map

| ID | Validation | Technology | Implementation |
| --- | --- | --- | --- |
| F1 | Show Comments only when Rating is 1 or 2 | JavaScript | Feedback.js |
| F2 | Automatically populate Submitted On during Feedback creation | C\# plug\-in | SetFeedbackSubmittedOnPlugin |
| F4 | Prevent forbidden whole words in Comments | C\# plug\-in \+ Environment Variable | FeedbackProfanityValidationPlugin |
| F5 | Only attendees with an Attended registration can submit Feedback | C\# plug\-in | ValidateFeedbackAttendancePlugin |
| F6 | Rating must be between 1 and 5 | Dataverse column configuration | Rating minimum/maximum |
| F7 | Submitted On remains read\-only after creation | JavaScript | Feedback.js |
| F8 | Feedback can be submitted only for a Completed Session | C\# plug\-in | ValidateFeedbackSessionStatusPlugin |
| F9 | Comments mandatory when Rating is 1 or 2 | Business Rule | NSQ \- Feedback \- Comments Required |
| F10 | Prevent duplicate Feedback for the same Attendee and Session | C\# plug\-in | PreventDuplicateFeedbackPlugin |
| S7/F3 | Prevent Session deletion when related Feedback exists | Shared C\# plug\-in | PreventSessionDeletionWithFeedbackPlugin |
| R9 | Cancel Registered registrations when Event becomes Cancelled | Power Automate | NSQ \- Event Cancellation \- Cancel Active Registrations |

---

## 4\. Feedback Table

### Table details

| Property | Value |
| --- | --- |
| Display name | Feedback |
| Logical name | trpj\_feedback |
| Ownership | User or Team |
| Primary column | Feedback Number |
| Primary column type | Autonumber |
| Autonumber format | FB\-\{SEQNUM:6\} |

Example Feedback Numbers:

- FB\-000001
- FB\-000002
- FB\-000003

### Custom columns

| Display name | Logical name | Type | Configuration |
| --- | --- | --- | --- |
| Feedback Number | Primary name column | Autonumber | Business required, read\-only |
| Session | trpj\_session | Lookup to Session | Business required |
| Attendee | trpj\_attendee | Lookup to Attendee | Business required |
| Rating | trpj\_rating | Whole Number | Business required, minimum 1, maximum 5 |
| Comments | trpj\_comments | Multiple Lines of Text | Optional at column level |
| Submitted On | trpj\_submittedon | Date and Time | Automatically populated |

Comments is optional at the Dataverse column level because F9 controls the requirement level dynamically.

---

## 5\. Feedback Form and Views

### Main form

**Form name:** Feedback Main Form

### Form layout

**General tab**

**Feedback Details section**

- Feedback Number
- Session
- Attendee
- Rating

**Additional Details section**

- Comments
- Submitted On

### Form behavior

- Feedback Number is read\-only.
- Session and Attendee are Business Required.
- Rating accepts whole numbers from 1 through 5.
- Comments is shown only for Rating 1 or 2.
- Comments becomes required for Rating 1 or 2.
- Comments is cleared when Rating changes to blank, 3, 4 or 5.
- Submitted On becomes read\-only after record creation.
- No unnecessary Timeline or extra tabs are used on the Feedback form.

### Views

#### Active Feedback

Shows active Feedback records with the main business columns.

#### All Feedback

Shows both active and inactive Feedback records for administration and reporting.

---

## 6\. JavaScript: JavaScript/Feedback/Feedback.js

The Feedback JavaScript web resource is registered on the Feedback Main Form.

### Strict mode

The file starts with "use strict";.

Strict mode helps detect:

- Undeclared variables
- Accidental global variables
- Unsafe assignments
- JavaScript mistakes that would otherwise fail silently

This is important because the model\-driven form can load JavaScript libraries created by multiple team members.

### F1: Comments Visibility and Clearing

#### Required behavior

| Rating | Comments behavior |
| --- | --- |
| Blank | Clear and hide |
| 1 | Show and enable |
| 2 | Show and enable |
| 3 | Clear and hide |
| 4 | Clear and hide |
| 5 | Clear and hide |

### Functions

| Function | Event | Purpose |
| --- | --- | --- |
| showHideCommentsOnLoad | Form OnLoad | Shows or hides Comments without clearing saved data during form load |
| showHideCommentsOnChange | Rating OnChange | Shows Comments for Rating 1 or 2; otherwise clears and hides Comments |
| lockSubmittedOn | Form OnLoad | Locks Submitted On on existing Feedback records |

### Why OnLoad and OnChange use separate functions

The OnLoad function must not automatically delete existing data merely because a record is opened.

The OnChange function handles active user changes. When the user changes Rating from 1 or 2 to blank, 3, 4 or 5, it:

1. Clears Comments.
2. Uses setSubmitMode("always") so the cleared value is submitted.
3. Hides Comments.

This fixes the regression where old Comments reappeared after a Rating sequence such as 2 → 3 → 2.

### Event registration

| Form event | Function | Pass execution context |
| --- | --- | --- |
| Form OnLoad | showHideCommentsOnLoad | Yes |
| Form OnLoad | lockSubmittedOn | Yes |
| Rating OnChange | showHideCommentsOnChange | Yes |

---

## 7\. Business Rule: F9

### Rule name

NSQ \- Feedback \- Comments Required

### Scope

Entity/Table scope

### Logic

**IF**

Rating equals 1 OR Rating equals 2

**THEN**

Set Comments as Business Required

**ELSE**

Set Comments as Not Business Required

### Expected behavior

| Rating | Comments requirement |
| --- | --- |
| Blank | Optional |
| 1 | Required |
| 2 | Required |
| 3 | Optional |
| 4 | Optional |
| 5 | Optional |

The ELSE branch provides reverse or flip\-flop behavior. For example, changing Rating from 2 to 4 changes Comments from required to optional.

### Separation of responsibilities

| Behavior | Owner |
| --- | --- |
| Show, hide and clear Comments | F1 JavaScript |
| Make Comments required or optional | F9 Business Rule |

JavaScript does not change the Comments requirement level, preventing conflicts with the Business Rule.

---

## 8\. Dataverse Column Validation: F6

### Requirement

Rating must be between 1 and 5.

### Implementation

Rating is configured as:

- Data type: Whole Number
- Minimum value: 1
- Maximum value: 5
- Requirement level: Business Required

Dataverse displays a standard range\-validation message for values outside 1 through 5.

### Decimal behavior

Decimal values such as 2.5 are not supported because the Rating column is a Whole Number column.

Changing Rating to a Decimal Number would affect:

- F1 comparison logic
- F9 Business Rule conditions
- Rating control behavior
- Existing records
- Forms, views and tests

Therefore, Rating currently supports integer values 1, 2, 3, 4 and 5.

---

## 9\. Plug\-in Project and Shared Assembly

### Visual Studio solution

Plugins/NSQ.EventManagement.Plugins.sln

### Project

NSQ.EventManagement.Plugins

### Framework

.NET Framework 4.6.2

### Shared assembly

NSQ.EventManagement.Plugins.dll

### Signing key

NSQ.EventManagement.snk

### NuGet package

Microsoft.CrmSdk.CoreAssemblies

### Shared base class

All plug\-ins inherit from:

NSQ.EventManagement.Plugins.Common.PluginBase

The base class provides:

- IPlugin implementation
- Execution context
- Organization service
- Tracing service
- Shared exception handling

### One\-assembly rule

The team uses one shared assembly for all Event, Session, Speaker, Attendee, Event Registration and Feedback plug\-in classes.

New classes are added to the existing project. After changes are merged into dev, the complete solution is built in Release mode and the existing registered assembly is updated.

A separate assembly must not be registered for every plug\-in class.

---

## 10\. Feedback Plug\-ins

### F2: Set Submitted On

**Class:** SetFeedbackSubmittedOnPlugin

### Purpose

Automatically populates Submitted On with the current UTC date and time when Feedback is created.

### Registration

| Setting | Value |
| --- | --- |
| Message | Create |
| Primary table | trpj\_feedback |
| Stage | PreOperation |
| Mode | Synchronous |
| Execution order | 1 |
| Images | None |

### Why PreOperation

PreOperation allows the plug\-in to add Submitted On directly to the incoming Target before Dataverse saves the row. No additional update request is needed.

---

### F4: Forbidden Words Validation

**Class:** FeedbackProfanityValidationPlugin

### Purpose

Prevents Feedback Comments containing configured forbidden whole words.

### Rules

- Case\-insensitive matching
- Whole words only
- Blank Comments allowed
- Runs on Create and Update
- Does not trace the complete Comments value
- Does not hardcode the word list in C\#

### Environment Variable

| Property | Value |
| --- | --- |
| Display name | NSQ Forbidden Words |
| Schema name | trpj\_ForbiddenWords |
| Data type | Text |
| Value format | Comma, semicolon or new\-line separated words |

Example value:

idiot,stupid,damn,nonsense,useless

### Environment Variable resolution

1. Read the Current Value for the environment.
2. If Current Value is empty, use the Default Value.
3. Split the configured text into individual words.
4. Perform case\-insensitive whole\-word matching.

The word list can therefore change between Development, Test and Production without rebuilding the plug\-in assembly.

### Create step

| Setting | Value |
| --- | --- |
| Message | Create |
| Table | trpj\_feedback |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

### Update step

| Setting | Value |
| --- | --- |
| Message | Update |
| Table | trpj\_feedback |
| Filtering attribute | trpj\_comments |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

---

### F5: Validate Attendee Attendance

**Class:** ValidateFeedbackAttendancePlugin

### Purpose

Allows Feedback only when the selected Attendee has an Event Registration marked Attended for the Event associated with the selected Session.

### Relationship checked

Feedback Attendee

- Feedback Session → Session Event
- Event Registration Event
- Event Registration Attendee
- Registration Status = Attended

### Logical names and values

| Item | Value |
| --- | --- |
| Feedback Attendee lookup | trpj\_attendee |
| Feedback Session lookup | trpj\_session |
| Session Event lookup | trpj\_eventid |
| Event Registration table | trpj\_eventregistration |
| Registration Event lookup | trpj\_eventid |
| Registration Attendee lookup | trpj\_attendeeid |
| Registration Status | trpj\_registrationstatus |
| Attended value | 738000002 |

### Create step

| Setting | Value |
| --- | --- |
| Message | Create |
| Table | trpj\_feedback |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

### Update protection

Attendance must also be checked if an existing Feedback record’s Attendee or Session is changed.

Recommended Update registration:

| Setting | Value |
| --- | --- |
| Message | Update |
| Table | trpj\_feedback |
| Filtering attributes | trpj\_attendee,trpj\_session |
| Stage | PreValidation |
| Mode | Synchronous |
| Pre Image name | PreImage |
| Pre Image columns | trpj\_attendee,trpj\_session |

The Update Target contains only changed columns. The Pre Image supplies the unchanged Attendee or Session value.

---

### F8: Completed Session Validation

**Class:** ValidateFeedbackSessionStatusPlugin

### Purpose

Allows Feedback only when the selected Session has Session Status equal to Completed.

### Logical names and value

| Item | Value |
| --- | --- |
| Session table | trpj\_session |
| Session Status | trpj\_session\_status |
| Completed value | 738000002 |

### Registration

| Setting | Value |
| --- | --- |
| Message | Create |
| Table | trpj\_feedback |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

### Expected behavior

- Completed Session → Feedback allowed
- Draft, Scheduled, In Progress or Cancelled Session → Feedback blocked
- Missing Session Status → Feedback blocked

---

### F10: Duplicate Feedback Prevention

**Class:** PreventDuplicateFeedbackPlugin

### Purpose

Prevents the same Attendee from submitting more than one Feedback record for the same Session.

### Unique business combination

Attendee \+ Session

### Expected behavior

| Combination | Result |
| --- | --- |
| Same Attendee and same Session | Blocked |
| Same Attendee and different Session | Allowed |
| Different Attendee and same Session | Allowed |
| Different Attendee and different Session | Allowed |

### Registration

| Setting | Value |
| --- | --- |
| Message | Create |
| Table | trpj\_feedback |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

If Attendee and Session remain editable after creation, an Update step using filtering attributes and a Pre Image should also protect the combination against duplicates created through later edits.

---

### Shared S7/F3: Prevent Session Deletion

**Class:** PreventSessionDeletionWithFeedbackPlugin

### Purpose

Prevents deletion of a Session when one or more Feedback records reference the Session.

### Registration

| Setting | Value |
| --- | --- |
| Message | Delete |
| Primary table | trpj\_session |
| Stage | PreValidation |
| Mode | Synchronous |
| Execution order | 1 |
| Images | None |

### User\-facing message

This Session cannot be deleted because related Feedback records exist. Delete the related Feedback records first.

### Expected behavior

- Session with Feedback → deletion blocked
- Session with multiple Feedback records → deletion blocked
- Session with no Feedback → deletion allowed
- Related Feedback deleted first → Session deletion allowed, unless another validation blocks it

---

## 11\. Model\-Driven App and Sitemap

### App name

NSQ Event Management

### Sitemap structure

**Area: Event Management**

#### Group: Event Planning

- Events
- Sessions
- Speakers

#### Group: Attendee Management

- Attendees
- Event Registrations

#### Group: Engagement

- Feedback

All six project tables are available as sitemap Sub\-areas.

### App access

The app is assigned only to:

- System Administrator
- System Customizer

App access is controlled by role assignment, while table privileges continue to be controlled by the Dataverse security roles.

---

## 12\. Auditing

M4 owns auditing configuration and verification.

### Environment\-level auditing

Auditing is enabled in each project environment through:

Power Platform admin center → Environment → Settings → Audit and logs → Audit settings → Start Auditing

### Audited tables

- Event
- Session
- Event Registration
- Feedback

### Table\-level configuration

Each table has **Audit changes to its data** enabled.

### Column\-level configuration

All required custom columns remain enabled for auditing.

### Verification

Audit History was verified by changing values such as:

- Event Status
- Session Status
- Registration Status
- Feedback Rating or Comments

The audit entry must show:

- Table and record
- Changed column
- Previous value
- New value
- Changed By
- Changed On

---

## 13\. Power Automate R9

### Flow name

NSQ \- Event Cancellation \- Cancel Active Registrations

### Requirement

When Event Status becomes Cancelled, all related Event Registrations currently in Registered status must be updated to Cancelled.

### Trigger

| Setting | Value |
| --- | --- |
| Connector | Microsoft Dataverse |
| Trigger | When a row is added, modified or deleted |
| Change type | Modified |
| Table | Events |
| Scope | Organization |
| Select columns | Event Status logical name |
| Filter | Event Status equals Cancelled |

### Choice values

| Choice | Value |
| --- | --- |
| Event Status: Cancelled | 100000003 |
| Registration Status: Registered | 738000000 |
| Registration Status: Cancelled | 738000001 |

### Main processing

1. Event Status changes to Cancelled.
2. List Event Registration rows where: 
    - Event lookup equals the triggering Event ID.
    - Registration Status equals Registered.
3. Use the value array returned by List rows.
4. Apply to each matching registration.
5. Update the current Event Registration to Cancelled.

### OData filter structure

The Event Registration query uses:

- Dynamic Event ID from the trigger
- Registered Status choice value
- Dataverse lookup filter syntax using \_<lookup>\_value

No Event or Event Registration record GUID is hardcoded.

### Error handling

The flow uses:

- Main Processing Scope
- Error Handling Scope
- Terminate action

The Error Handling Scope runs when Main Processing fails or times out.

The Terminate action marks the complete flow run as Failed and provides a meaningful error code and message.

### Expected results

| Registration | Expected result |
| --- | --- |
| Registered registration for the cancelled Event | Changed to Cancelled |
| Attended registration for the cancelled Event | Unchanged |
| Already Cancelled registration | Unchanged |
| Registered registration for another Event | Unchanged |
| Event with no matching registrations | Flow succeeds with zero loop iterations |

A skipped Update action with the message “there are no items to repeat” means List rows returned zero matching registrations. This is valid behavior rather than a flow failure.

---

## 14\. Connection References

All solution\-aware flows should use the approved shared Dataverse connection reference.

### Purpose

A connection stores authenticated credentials.

A connection reference is a solution component that points a flow to an environment\-specific connection.

### Development environment

The shared reference points to an approved Dataverse connection in the development environment.

### Target environment

During managed\-solution import:

1. The connection\-reference component is imported.
2. Authentication credentials are not transported.
3. The importer maps the reference to a valid Dataverse connection in the target environment.
4. All flows using the shared reference use that target connection.

The connection reference should use a neutral project name rather than a developer’s personal name.

---

## 15\. Environment Variables

Environment variables store configuration that may change between environments.

### M4 environment variable

trpj\_ForbiddenWords

### Usage

The Feedback profanity plug\-in reads forbidden words from this variable.

### Deployment behavior

- Definition travels with the solution.
- Current Value can be configured for each environment.
- Plug\-in code does not need to be changed when the word list changes.

Environment variables should also be used for environment\-specific URLs, recipient addresses, feature switches and fixed configuration identifiers.

Dynamic row IDs should come from triggers or action outputs rather than from hardcoded GUIDs.

---

## 16\. Testing Completed

| Requirement | Main test | Result |
| --- | --- | --- |
| F1 | Rating 1/2 shows Comments; 3/4/5 hides and clears Comments | Passed |
| F2 | Submitted On automatically populated on create | Passed |
| F4 | Forbidden whole words blocked on Create and Update | Passed |
| F5 | Only Attended attendees allowed to submit Feedback | Passed |
| F6 | Values outside 1–5 rejected | Passed |
| F7 | Submitted On locked after creation | Passed |
| F8 | Non\-completed Session blocked | Passed |
| F9 | Comments required for Rating 1 or 2 and optional otherwise | Passed |
| F10 | Same Attendee and Session duplicate blocked | Passed |
| S7/F3 | Session with Feedback cannot be deleted | Passed |
| R9 | Registered registrations cancelled when Event is cancelled | Passed |
| Auditing | Status/value changes visible in Audit History | Passed |
| Sitemap | All six tables available in correct Groups | Passed |

### Required regression scenarios

- Rating 2 → 3 → 2: Comments remains cleared.
- Rating 1 → 4 → 1: Comments remains cleared.
- Save and reopen after clearing Comments.
- Change Feedback Attendee after save and revalidate attendance.
- Change Feedback Session after save and revalidate attendance and Session Status.
- Same Attendee and Session duplicate remains blocked.
- Valid Feedback still receives Submitted On.
- Forbidden Comments blocked regardless of uppercase or lowercase.
- Session deletion blocked only when related Feedback exists.
- Event cancellation updates only related Registered registrations.
- Registrations belonging to other Events remain unchanged.

---

## 17\. Known Limitations and Important Notes

1. **JavaScript is form\-side only.** Imports, grid edits and APIs can bypass JavaScript. Server\-side plug\-ins protect the important cross\-table rules.
2. **Rating is a Whole Number.** Decimal ratings such as 2.5 are not stored. Supporting decimals would require a different Dataverse column type and changes to F1 and F9.
3. **F5 and F10 require Update protection if lookups remain editable.** Create\-only steps are insufficient when Attendee or Session can be changed after Feedback is saved.
4. **Whole\-word profanity matching depends on configured values.** The environment variable must contain valid separators and non\-empty words.
5. **The Shared Plug\-in Assembly must be updated carefully.** The Release DLL must be built from the latest dev branch so it contains every team member’s classes.
6. **Registered plug\-in steps belong to the environment.** Updating an assembly must preserve all existing plug\-in types and steps.
7. **Connection\-reference authentication does not travel between environments.** The target importer must map the imported reference to a valid target connection.
8. **Power Automate run timing is not immediate in every case.** Dataverse\-triggered flows normally run quickly but should be verified through Run History.

---

## 18\. Git Workflow

### Branch strategy

main → dev → feature/jay

M4 work is completed in feature/jay and merged into dev through pull requests.

### Allowed commit types

| Type | Usage |
| --- | --- |
| feat | New functionality |
| fix | Defect correction |
| refactor | Internal improvement without intended behavior change |
| docs | Documentation update |

### M4 commit examples

- feat(feedback): show comments only for ratings below 3
- feat(feedback): lock submitted on after record creation
- feat(feedback): auto\-populate submitted on during creation
- feat(feedback): restrict feedback to completed sessions
- feat(feedback): block forbidden words in comments
- feat(feedback): prevent duplicate attendee session feedback
- feat(feedback): restrict feedback to attended attendees
- feat(session): prevent deletion when feedback exists
- fix(feedback): clear comments when rating no longer requires them
- refactor(feedback): load forbidden words from environment variable
- docs(readme): document M4 feedback implementation

### Important Git rules

- Do not use git add . in the shared plug\-in project.
- Stage only intended files.
- Review git status and git diff \-\-cached.
- Do not commit bin, obj, .vs, .dll, .pdb or .user files.
- The .csproj must include every C\# class exactly once.
- Preserve valid class entries from all branches when resolving .csproj conflicts.
- Pull the latest dev before starting new plug\-in work or updating the shared assembly.

---

## 19\. Deployment

### Source environment preparation

1. Merge approved work into dev.
2. Build the shared plug\-in project in Release.
3. Publish all Dataverse customizations.
4. Confirm all flows are inside the solution.
5. Confirm the shared Dataverse connection reference is included.
6. Confirm environment\-variable definitions are included.
7. Confirm plug\-in assembly and registered steps are included.
8. Confirm the model\-driven app and six tables are included.
9. Test JavaScript, Business Rules, plug\-ins, flows and auditing.
10. Export the solution as Managed for the target Test or Demo environment.

### Target environment import

1. Create or verify a valid target Dataverse connection.
2. Import the managed solution.
3. Map the imported connection reference to the target Dataverse connection.
4. Enter target environment\-variable values.
5. Complete the import.
6. Verify flow ownership and connection\-reference mapping.
7. Turn on imported flows if required.
8. Confirm plug\-in steps are enabled.
9. Open the model\-driven app.
10. Run full regression testing in the target environment.

### Preferred Solution

Setting the NSQ solution as the Preferred Solution helps new development components and customizations enter the correct solution automatically.

Preferred Solution does not:

- Transfer authentication credentials
- Remove the need to map connection references
- Replace managed\-solution import
- Automatically configure target environment\-variable values

---

## 20\. M4 Completion Summary

| Deliverable | Status |
| --- | --- |
| Feedback table | Completed |
| Feedback form | Completed |
| Feedback views | Completed |
| F1 JavaScript | Completed |
| F2 plug\-in | Completed |
| F4 plug\-in and Environment Variable | Completed |
| F5 plug\-in | Completed |
| F6 Dataverse validation | Completed |
| F7 JavaScript | Completed |
| F8 plug\-in | Completed |
| F9 Business Rule | Completed |
| F10 plug\-in | Completed |
| Shared Session deletion plug\-in | Completed |
| Model\-driven app shell | Completed |
| Sitemap | Completed |
| Security\-role visibility | Completed |
| Auditing | Completed |
| R9 Power Automate flow | Completed |
| Regression testing | Completed |
| Documentation | Completed |

=======
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
>>>>>>> 2be4fb9ef2612e1e1bb170cf7a333e7a29072879
