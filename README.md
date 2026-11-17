# NSQ Event Management System

## Contributor
Rutul Wable

## Overview
This document describes the validations and automation implemented for the NSQ Event Management System project.

### Technologies Used
- JavaScript
- Business Rules
- Dataverse Plugins (C#)
- Power Automate

# Speaker Table Validations

## SP2 - Email and Phone Number Format Validation
**Technology:** JavaScript

- Validates Email Address format.
- Validates Phone Number format.
- Registered on Email and Phone OnChange events.

## SP3 - LinkedIn URL Validation
**Technology:** JavaScript

- Ensures LinkedIn Profile contains a valid LinkedIn URL.
- Registered on LinkedIn Profile OnChange.

## SP4 - Contact Information Validation
**Technology:** JavaScript

- Either Email Address or Phone Number must be provided.
- Implemented on Form OnSave.

## SP7 - Organization Mandatory When LinkedIn Exists
**Technology:** Business Rule

- If LinkedIn Profile contains data, Organization becomes required.
- Otherwise Organization remains optional.

# Session Table Validations

## S1 - Session Dates Within Event Dates
**Technology:** JavaScript

Rules:
- Session Start Date >= Event Start Date
- Session End Date <= Event End Date

## S2 - End Date Dependency Validation
**Technology:** JavaScript

Rules:
- End Date disabled until Start Time is selected.
- End Date cannot be before Start Date.
- Final DateTime validation occurs on save.

## S9 - Session Cannot Be Completed Before End Time
**Technology:** JavaScript

- Prevents users from marking a Session as Completed before Session End Time.

## S10 - Session Description Mandatory For Workshop
**Technology:** Business Rule

- Session Description becomes mandatory when Session Type = Workshop.

## S12 - Lock Speaker Lookup When Session Completed
**Technology:** JavaScript

- Speaker lookup becomes read-only when Session Status is Completed.

# Plugin Implementations

## SP1 + SP5
**Plugin:** SpeakerDuplicateCheckPlugin

Purpose:
- Prevent duplicate Email Address.
- Prevent duplicate Phone Number.
- Prevent duplicate LinkedIn Profile.

## S5
**Plugin:** SessionSpeakerValidationPlugin

Purpose:
- Speaker must be assigned before Session Status becomes Planned or Completed.

## S6
**Plugin:** SessionOverlapValidationPlugin

Purpose:
- Prevent overlapping Sessions for the same Event.

## S8
**Plugin:** SessionSpeakerOverlapValidationPlugin

Purpose:
- Prevent assigning the same Speaker to overlapping Sessions.

## S11 + E10
**Plugin:** SessionCancelledEventValidationPlugin

Purpose:
- Prevent Session creation or updates when Event Status is Completed or Cancelled.

# Power Automate Flow

## S3 - Welcome Email To Speaker
**Flow Name:** NSQ - Session - Send Welcome Email To Speaker

Trigger:
- Session Status changes to Planned.

Flow Actions:
1. Get Speaker
2. Get Event
3. Convert Start Time
4. Convert End Time
5. Check Speaker Email Exists
6. Get Sender User
7. Create Email Record

Result:
- Email Activity is created.
- Email appears on Session Timeline.

# Deployment Components

## Connection Reference
NSQ Dataverse Connection

## Environment Variable
NSQ Sender Email

# Git Workflow
feature/rutul
-> Commit
-> Push
-> Merge into dev
-> Push dev

## Sample Commit Messages
- feat(plugin): add speaker duplicate validation
- feat(session): add session overlap validation
- fix(session): update date validation logic

# Status

## Completed Speaker Validations
- SP1
- SP2
- SP3
- SP4
- SP5
- SP7

## Completed Session Validations
- S1
- S2
- S5
- S6
- S8
- S9
- S10
- S11
- S12
- E10

## Completed Power Automate
- S3 Welcome Email Flow

