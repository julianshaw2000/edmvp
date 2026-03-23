# auditraks User Manual
## Tungsten Supply Chain Compliance Platform

**Version 2.0 — March 2026**

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Getting Started](#2-getting-started)
3. [Supplier Portal](#3-supplier-portal)
4. [Buyer Portal](#4-buyer-portal)
5. [Admin Portal](#5-admin-portal)
6. [Compliance Engine](#6-compliance-engine)
7. [Public Features](#7-public-features)
8. [Tamper Evidence and Data Integrity](#8-tamper-evidence-and-data-integrity)
9. [Notifications and Email Alerts](#9-notifications-and-email-alerts)
10. [Troubleshooting and FAQ](#10-troubleshooting-and-faq)

---

## 1. Introduction

### What Is auditraks?

auditraks is a supply chain compliance platform that tracks the custody of mineral materials from extraction through to final processing. It provides a verifiable, tamper-evident record of every step a batch of material takes through the supply chain, enabling buyers, suppliers, and auditors to confirm that sourcing and handling practices meet internationally recognized due diligence standards.

auditraks validates compliance against two leading frameworks:

- **RMAP (Responsible Minerals Assurance Process)** — verifies that smelters and refiners have been audited and certified under the Responsible Minerals Initiative.
- **OECD Due Diligence Guidance (DDG)** — assesses origin-country risk, sanctions exposure, and document completeness in line with the OECD's five-step framework for responsible mineral supply chains.

### Who Uses auditraks?

auditraks has three user roles:

| Role               | Description                                                                                                                                      |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Supplier**       | Miners, traders, processors, and exporters who create batches and record custody events as material moves through the chain.                     |
| **Buyer**          | Companies that receive material and need to verify compliance, generate reports, and share documentation with customers or auditors.             |
| **Platform Admin** | auditraks platform administrators who manage users, maintain reference data (such as the RMAP smelter list), and review flagged compliance cases. |

---

## 2. Getting Started

### Accessing auditraks

auditraks is a web-based application. Open your preferred browser and navigate to the auditraks URL provided by your organization or platform administrator. No software installation is required.

**Supported browsers:** Google Chrome (recommended), Mozilla Firefox, Microsoft Edge, Safari.

[Screenshot: auditraks login page]

### Signing In

auditraks uses secure single sign-on via **Auth0**. You can sign in with:

- **Google account** — click "Continue with Google" and sign in with your Google credentials.
- **Email and password** — enter the email address associated with your auditraks account and your password.

**First-time users:** Your account must be created by a Platform Admin before you can sign in. You will receive an invitation email with a link to activate your account. The link expires after 7 days; contact your administrator if it has expired.

**Forgot your password?** Click "Forgot password?" on the login screen. A reset link will be sent to your registered email address.

### After Signing In

Upon successful sign-in, auditraks detects your role and redirects you to the appropriate portal:

- Suppliers are taken to the **Supplier Dashboard**.
- Buyers are taken to the **Buyer Dashboard**.
- Platform Admins are taken to the **Admin Dashboard**.

If you believe you have been assigned the wrong role, contact your Platform Admin.

---

## 3. Supplier Portal

The Supplier Portal is where organizations responsible for handling material — miners, processors, traders, and exporters — create batch records and log custody events as material moves through the supply chain.

### 3.1 Dashboard Overview

[Screenshot: Supplier Dashboard]

The Supplier Dashboard displays all batches associated with your organization. Each batch is shown as a card that includes:

- **Batch number** — the unique identifier for the batch.
- **Mineral type** — the material being tracked.
- **Origin** — the country and mine where the material was extracted.
- **Current weight** — the most recently recorded weight for the batch.
- **Compliance status** — a color-coded indicator (see Section 3.5).
- **Last updated** — the date and time of the most recent event.

Use the search bar at the top of the dashboard to find a specific batch by number. You can also filter batches by compliance status using the filter controls.

### 3.2 Creating a New Batch

A batch represents a discrete quantity of material that will be tracked through the supply chain.

**To create a new batch:**

1. Click the **"New Batch"** button on the Supplier Dashboard.
2. Complete the batch creation form:

| Field                   | Description                                                                                                                 |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| **Batch Number**        | A unique identifier for this batch. Use your organization's internal numbering system or the format provided by your buyer. |
| **Mineral Type**        | The type of mineral material in this batch.                                                                                 |
| **Origin Country**      | The country where the material was extracted.                                                                               |
| **Mine Name / Site**    | The name of the mine or extraction site.                                                                                    |
| **Initial Weight (kg)** | The weight of the batch at creation.                                                                                        |

3. Click **"Create Batch"** to save.

The batch will appear on your dashboard with a status of **PENDING** until custody events are recorded and compliance checks are completed.

> **Note:** Batch numbers must be unique within the platform. If you receive an error stating the batch number already exists, check whether the batch has already been created by a colleague or use a different identifier.

### 3.3 Splitting a Batch

When a batch of material is physically divided — for example, when part of a shipment is sold to one buyer and the remainder to another — you can split the batch into two child batches.

**To split a batch:**

1. Open the Batch Detail view for the batch you want to split.
2. Click **"Split Batch"**.
3. Enter the weight for **Child A** and **Child B**. The two weights must sum exactly to the parent batch's total weight.
4. Click **"Confirm Split"**.

auditraks will:
- Create two new child batches with suffixes `-A` and `-B` appended to the original batch number.
- Mark the parent batch as **COMPLETED** (it has been consumed by the split).
- Preserve the full custody chain — both child batches inherit the parent's origin, mineral type, and mine site.

Each child batch then continues through the supply chain independently and is subject to its own compliance checks.

> **Note:** A completed batch cannot be split. You must split the batch before marking it as completed.

### 3.4 Submitting Custody Events

A custody event records a specific activity that occurred with the batch — such as extraction, processing, transfer, or export. Events form a chronological chain that documents the full journey of the material.

**To add a custody event:**

1. Open the batch by clicking on its card from the dashboard.
2. Click **"Add Event"**.
3. Select the event type from the dropdown menu.
4. Fill in the required fields for that event type (described below).
5. Optionally attach supporting documents (see Section 3.4).
6. Click **"Submit Event"**.

[Screenshot: Add Event form]

Once submitted, an event cannot be deleted. If a correction is needed, a correction event must be submitted that links back to the original (see Section 8 — Tamper Evidence).

---

#### Event Type 1: Mine Extraction

Records the extraction of material from the mine.

| Field                                   | Description                                                                  |
| --------------------------------------- | ---------------------------------------------------------------------------- |
| **GPS Coordinates**                     | Latitude and longitude of the extraction site (e.g., 47.3769° N, 8.5417° E). |
| **Mine Operator**                       | Name of the company or individual operating the mine.                        |
| **Mineralogical Certificate Reference** | Reference number of the mineralogical certificate issued at extraction.      |

---

#### Event Type 2: Concentration

Records a concentration or beneficiation process applied to the material.

| Field                   | Description                                                                                 |
| ----------------------- | ------------------------------------------------------------------------------------------- |
| **Facility Name**       | Name of the processing facility.                                                            |
| **Process Description** | A brief description of the concentration method used (e.g., gravity separation, flotation). |
| **Input Weight (kg)**   | Weight of material entering the process.                                                    |
| **Output Weight (kg)**  | Weight of material leaving the process.                                                     |
| **Concentration Ratio** | The ratio of output to input weight (calculated automatically if left blank).               |

---

#### Event Type 3: Trading / Transfer

Records the transfer of ownership or custody of the material between parties.

| Field                  | Description                                                          |
| ---------------------- | -------------------------------------------------------------------- |
| **Seller**             | Name of the party transferring the material.                         |
| **Buyer**              | Name of the party receiving the material.                            |
| **Transfer Date**      | The date on which the transfer took place.                           |
| **Contract Reference** | The reference number of the purchase contract or transfer agreement. |

---

#### Event Type 4: Laboratory Assay

Records an analytical test performed on the material.

| Field                     | Description                                                      |
| ------------------------- | ---------------------------------------------------------------- |
| **Laboratory Name**       | Name of the laboratory that conducted the assay.                 |
| **Method**                | The analytical method used (e.g., XRF, ICP-MS).                  |
| **Tungsten Content (%)**  | The tungsten content of the material as determined by the assay. |
| **Certificate Reference** | Reference number of the assay certificate.                       |

---

#### Event Type 5: Primary Processing (Smelting)

Records a smelting or primary processing step.

| Field                  | Description                                                                                          |
| ---------------------- | ---------------------------------------------------------------------------------------------------- |
| **Smelter ID (RMAP)**  | The RMAP-assigned identifier for the smelter facility. This field triggers an RMAP compliance check. |
| **Process Type**       | The type of smelting or refining process (e.g., electric arc furnace, hydrometallurgical).           |
| **Input Weight (kg)**  | Weight of material entering the smelter.                                                             |
| **Output Weight (kg)** | Weight of material leaving the smelter.                                                              |

> **Important:** The Smelter ID must match an entry in the current RMAP-approved smelter list. If the smelter is not listed, the batch will be flagged for compliance review. Contact your Platform Admin if you believe the smelter should be on the list.

---

#### Event Type 6: Export / Shipment

Records the export or international shipment of the material.

| Field                       | Description                                                     |
| --------------------------- | --------------------------------------------------------------- |
| **Origin Country**          | The country from which the shipment departs.                    |
| **Destination Country**     | The country to which the shipment is sent.                      |
| **Transport Mode**          | The method of transport (e.g., air freight, sea freight, road). |
| **Export Permit Reference** | Reference number of the export permit or customs declaration.   |

---

### 3.5 Uploading Documents

Supporting documents can be attached to a batch or to specific custody events. Accepted file formats are:

- PDF
- JPEG / JPG
- PNG
- TIFF
- GIF

**Maximum file size: 25 MB per file.**

**To upload a document:**

1. Open the batch or event to which you want to attach the document.
2. Click **"Upload Document"** or drag and drop the file into the upload area.
3. Enter a description of the document (e.g., "Mineralogical Certificate — Batch W-2026-041").
4. Click **"Save"**.

[Screenshot: Document upload panel]

Uploaded documents are stored securely and are accessible to authorized buyers and administrators. Documents cannot be deleted once uploaded; if an incorrect document was uploaded, upload the correct version and add a note in the description to indicate which version supersedes the other.

### 3.6 Viewing Batch Details

Click on any batch card on the dashboard to open the Batch Detail view. This view is organized into four tabs:

- **Overview** — batch identification, origin, weight, current status, and compliance summary at a glance.
- **Events** — a chronological timeline of all custody events recorded for this batch. Each event shows the event type, date, the user who submitted it, and a summary of key fields.
- **Documents** — a list of all documents attached to the batch and its events, with download links.
- **Compliance** — detailed compliance check results for RMAP, OECD DDG, mass balance, and sequence checks.

### 3.7 Updating Batch Status

As a batch progresses through the supply chain, you can update its status:

- **CREATED** → **ACTIVE** — when the first custody events are being recorded and the batch is in transit.
- **ACTIVE** → **COMPLETED** — when the batch has reached its final destination and all events have been recorded.

**To update the status:** Open the Batch Detail view and click the status button (e.g., "Mark Active" or "Mark Completed").

> **Note:** Status transitions are one-way. A completed batch cannot be reverted to active.

### 3.8 Understanding Compliance Statuses

Each batch displays one of the following compliance statuses:

| Status                | Color | Meaning                                                                                                                                                                   |
| --------------------- | ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **COMPLIANT**         | Green | All compliance checks have passed. The batch meets RMAP and OECD DDG requirements based on the information recorded.                                                      |
| **FLAGGED**           | Red   | One or more compliance checks have failed. The batch requires review. Common causes include an unrecognized smelter ID, a high-risk origin country, or a sanctions match. |
| **INSUFFICIENT_DATA** | Amber | The compliance engine does not have enough information to make a determination. Additional events or documents are needed.                                                |
| **PENDING**           | Grey  | The batch has been created but no compliance checks have been triggered yet.                                                                                              |

Compliance statuses are recalculated automatically each time a new event is submitted. You will receive a notification if a batch you are responsible for is flagged.

---

## 4. Buyer Portal

The Buyer Portal allows purchasing organizations to monitor the compliance status of incoming material, review custody chains, generate compliance reports, and share verified documentation with customers and auditors.

### 4.1 Dashboard

[Screenshot: Buyer Dashboard]

The Buyer Dashboard provides an at-a-glance overview of all batches associated with your organization. At the top of the page, summary cards show:

- Total number of active batches.
- Number of compliant batches.
- Number of flagged batches.
- Number of batches with insufficient data.

Below the summary cards, a **batch table** lists all batches. You can:

- **Search** by batch number, origin country, or mineral type using the text search bar.
- **Filter by compliance status** using the dropdown (ALL, COMPLIANT, FLAGGED, PENDING, INSUFFICIENT_DATA).
- **Filter by date range** using the From and To date pickers to narrow results to a specific time period.
- **Clear filters** by clicking the "Clear" button that appears when any filter is active.
- **Sort** by clicking any column header.

Click on any row in the table to open the Batch Detail view.

### 4.2 Batch Detail View

The Batch Detail view for buyers mirrors the supplier view but includes additional compliance information. It contains three tabs:

**Events tab**
A chronological log of all custody events for the batch. For each event, you can see the event type, date submitted, submitting organization, and the key data fields recorded.

**Compliance Checks tab**
A detailed breakdown of each compliance check run against the batch, including:
- The check type (RMAP or OECD DDG).
- The result (Pass / Fail / Inconclusive).
- The specific rule or criterion that was evaluated.
- Notes explaining any failures.

**Documents tab**
All documents attached to the batch and its events. Click on any document to preview or download it.

[Screenshot: Buyer Batch Detail — Compliance Checks tab]

### 4.3 Generating a Material Passport

A Material Passport is a PDF report that summarizes the verified custody chain and compliance status of a batch. It is designed to be shared with customers, regulators, or auditors as evidence of responsible sourcing.

The Material Passport includes:
- Batch identification information (batch number, mineral type, origin).
- A summary of the custody chain (key events and parties).
- Compliance summary (RMAP and OECD DDG results).
- Hash chain integrity verification status.
- A QR code that links to the publicly verifiable batch record.
- Platform version and compliance rule set version (for audit traceability).
- The name of the user who generated the report and the generation timestamp.

**To generate a Material Passport:**

1. Open the Batch Detail view for the relevant batch.
2. Click **"Generate Material Passport"**.
3. auditraks will prepare the PDF. This may take a few seconds.
4. Click **"Download"** when the report is ready.

[Screenshot: Material Passport generation dialog]

> **Note:** A Material Passport can only be generated for batches with a compliance status of COMPLIANT. If the batch is flagged or has insufficient data, resolve the compliance issues before generating the passport.

### 4.4 Generating an Audit Dossier

An Audit Dossier is a comprehensive PDF report intended for formal audits and due diligence reviews. It contains the complete event log, all compliance check results with supporting detail, references to all attached documents with their SHA-256 file hashes, and hash chain integrity verification. Platform version and rule set version are included in the footer for audit traceability.

**To generate an Audit Dossier:**

1. Open the Batch Detail view.
2. Click **"Generate Audit Dossier"**.
3. Select the date range you wish to include (or leave blank to include all events).
4. Click **"Generate"**.
5. Download the completed PDF.

The Audit Dossier is typically larger than a Material Passport and may take longer to prepare for batches with many events and documents.

### 4.5 Sharing a Material Passport

You can share a Material Passport with external parties — such as customers, auditors, or regulatory bodies — using a time-limited secure link. The recipient does not need an auditraks account to view the passport.

**Shared links are valid for 30 days from the date of creation.**

**To create a shared link:**

1. Generate the Material Passport (see Section 4.3).
2. Click **"Share"** next to the Material Passport.
3. auditraks generates a unique URL and displays it in a green confirmation box.
4. Click **"Copy"** to copy the link to your clipboard, then send it to the recipient via email, messaging, or any other channel.

The recipient can open the link in any browser to view the Material Passport. The link will expire automatically after 30 days. If the recipient needs access after expiry, generate a new shared link.

> **Security note:** Treat shared links as confidential. Anyone who has the link can view the Material Passport during the active period. Do not post shared links publicly.

### 4.6 Downloading Documents

From the Documents tab of any Batch Detail view, click the download icon next to any document to save it to your device. You can also download all documents for a batch as a ZIP archive by clicking **"Download All"**.

---

## 5. Admin Portal

The Admin Portal is used by Platform Admins to manage users, maintain reference data, and oversee compliance reviews across the platform.

### 5.1 Dashboard

[Screenshot: Admin Dashboard]

The Admin Dashboard displays a system-wide overview, including:

- Total number of registered users (broken down by role).
- Total number of active batches on the platform.
- Number of batches by compliance status.
- Number of flagged batches awaiting review.
- Recent platform activity.

### 5.2 User Management

#### Inviting a New User

1. Navigate to **Users** in the left-hand navigation menu.
2. Click **"Invite User"**.
3. Enter the new user's email address.
4. Select their role: **Supplier**, **Buyer**, or **Platform Admin**.
5. Click **"Send Invitation"**.

The user will receive an email invitation with a link to activate their account. The invitation link expires after 7 days. You can resend the invitation from the Users list if needed.

[Screenshot: Invite User dialog]

#### Assigning or Changing a Role

1. Find the user in the Users list.
2. Click the user's name to open their profile.
3. Select the new role from the **Role** dropdown.
4. Click **"Save Changes"**.

Role changes take effect immediately. The user will be redirected to the appropriate portal on their next page load.

#### Deactivating a User

Deactivating a user prevents them from signing in without permanently deleting their account or any records they have created.

1. Open the user's profile from the Users list.
2. Click **"Deactivate Account"**.
3. Confirm the action when prompted.

To reactivate a deactivated user, locate their profile (use the "Show deactivated users" filter), open it, and click **"Reactivate Account"**.

> **Note:** Deactivating a user does not affect any batches or events they have created. All historical records remain intact.

### 5.3 RMAP Smelter List Management

auditraks maintains a list of RMAP-certified smelters. Smelter IDs entered in Primary Processing events are checked against this list. The list must be kept up to date to ensure accurate compliance checking.

The RMAP smelter list is uploaded as a **CSV file**. The Responsible Minerals Initiative publishes an updated list periodically; download the latest version from the RMI website and upload it to auditraks.

**To update the RMAP smelter list:**

1. Navigate to **Compliance Settings** > **RMAP Smelter List** in the admin navigation.
2. Click **"Upload New List"**.
3. Select the CSV file from your device.
4. auditraks will validate the file format and display a preview of the records to be imported.
5. If the preview looks correct, click **"Confirm Upload"**.

[Screenshot: RMAP Smelter List upload screen]

The new list takes effect immediately. Any batches with smelter IDs that were previously unrecognized will be re-evaluated automatically.

**CSV format requirements:** The file must include at minimum a column for the RMAP Smelter ID and the smelter name. Consult your Platform Admin technical contact for the exact column specification if you encounter import errors.

### 5.4 System Health and Job Monitor

The System Health page provides platform administrators with visibility into the operational status of the auditraks platform.

**To access:** Navigate to **System Health** from the Admin Dashboard or click **Jobs** in the admin navigation.

The page displays:
- **API Health Status** — a green or red indicator showing whether the API is responding normally.
- **Job Queue** — a table of recent background jobs (compliance checks, document generation, email dispatch) showing:
  - Job type and status (PENDING, PROCESSING, COMPLETED, FAILED)
  - Creation and completion timestamps
  - Error details for failed jobs

The job queue auto-refreshes every 10 seconds. Click **"Refresh"** for an immediate update.

> **Note:** Failed jobs are automatically retried by the background worker. If a job remains in FAILED status after multiple retries, investigate the error detail and contact the auditraks support team if needed.

### 5.5 Compliance Review

The Compliance Review section lists all batches with a **FLAGGED** status that require administrator attention.

[Screenshot: Compliance Review queue]

**To review a flagged batch:**

1. Navigate to **Compliance Review** in the admin navigation.
2. Click on a flagged batch to open the review view.
3. Review the compliance check results to understand why the batch was flagged.
4. You may:
   - **Add a review note** documenting your findings.
   - **Request additional information** from the supplier (this sends a notification to the supplier).
   - **Override the flag** if you determine that the flag was raised in error, providing a written justification.
   - **Escalate** the case for formal investigation.
5. Click **"Save Review"** to record your action.

All compliance review actions are logged with the reviewer's name, timestamp, and notes.

---

## 6. Compliance Engine

auditraks's compliance engine automatically evaluates batches against RMAP and OECD DDG frameworks as events are submitted. This section explains how each check works and how individual check results combine into an overall batch compliance status.

### 6.1 RMAP Checks

**When triggered:** An RMAP check is triggered automatically whenever a **Primary Processing (Smelting)** event is submitted.

**What is checked:** auditraks looks up the Smelter ID entered in the event against the current RMAP-approved smelter list (maintained by Platform Admins — see Section 5.3).

**Outcomes:**
- If the smelter ID is found on the list, the RMAP check **passes**.
- If the smelter ID is not found on the list, the RMAP check **fails** and the batch is flagged.

**What to do if an RMAP check fails:** Verify that the smelter ID was entered correctly. If the smelter is genuinely not listed, it has either not yet been audited under RMAP or its certification has lapsed. Contact the smelter to obtain their current RMAP certification status, and contact your Platform Admin to check whether the smelter list needs updating.

### 6.2 OECD DDG Checks

OECD DDG checks evaluate a broader range of risk factors in accordance with the OECD Due Diligence Guidance for Responsible Supply Chains of Minerals from Conflict-Affected and High-Risk Areas.

**What is checked:**

| Check                     | Description                                                                                                                                                                                            |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Origin Country Risk**   | The origin country recorded for the batch is assessed against a risk classification. Countries identified as conflict-affected or high-risk under OECD guidance trigger a flag.                        |
| **Sanctions Screening**   | The origin country, trading parties, and smelter are checked against applicable sanctions lists. Any match results in a flag.                                                                          |
| **Document Completeness** | auditraks verifies that the expected documentation is present for the events recorded. Missing key documents (such as an export permit for an export event) will result in an INSUFFICIENT_DATA status. |

**When triggered:** OECD DDG checks run automatically when relevant events are submitted (for example, an origin country risk check runs when a batch is created; a document completeness check runs continuously as events are added).

### 6.3 Mass Balance Checks

**When triggered:** A mass balance check runs automatically when a **Concentration** or **Primary Processing** event is submitted with both input and output weights.

**What is checked:** auditraks compares the output weight to the input weight. If the output exceeds the input by more than 5%, the batch is flagged. This guards against reporting errors or fraudulent weight inflation.

**What to do if a mass balance check fails:** Review the input and output weights on the event. If there is a genuine error, submit a correction event (see Section 8.3). If the process legitimately produces more output than input (e.g., due to added materials), document this with a note and contact your Platform Admin.

### 6.4 Sequence Checks

**When triggered:** A sequence check runs automatically every time a new custody event is submitted.

**What is checked:** auditraks verifies that the new event's date is not earlier than the most recent existing event for the batch. Out-of-order events are flagged, as they may indicate a data entry error or a process irregularity.

**What to do if a sequence check fails:** Verify the event date. If the date was entered incorrectly, submit a correction. If events genuinely occurred out of chronological order (e.g., backdated lab results), document the reason.

### 6.5 GPS Coordinate Validation

**When triggered:** GPS coordinates are validated at event submission time.

**What is checked:** auditraks verifies that GPS coordinates are in valid `latitude,longitude` format with latitude between -90 and 90, and longitude between -180 and 180.

### 6.6 Batch Compliance Rollup

A batch's overall compliance status is determined by combining the results of all individual checks:

- If **all checks pass** → status is **COMPLIANT**.
- If **any check fails** → status is **FLAGGED**.
- If **checks cannot be completed** due to missing information → status is **INSUFFICIENT_DATA**.
- If **no checks have been triggered yet** (e.g., a newly created batch with no events) → status is **PENDING**.

The overall status is recalculated every time a new event is submitted.

### 6.4 Compliance Notifications

auditraks sends email notifications when compliance-relevant events occur:

| Event                                          | Recipients                                                                                              |
| ---------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| Batch flagged (any compliance failure)         | The supplier who submitted the triggering event, all buyers associated with the batch, Platform Admins. |
| Batch status changes from FLAGGED to COMPLIANT | The supplier, all buyers associated with the batch.                                                     |
| Compliance review action taken by admin        | The supplier and buyers associated with the batch.                                                      |

Ensure your registered email address is current so you receive these notifications promptly.

---

## 7. Public Features

### 7.1 Batch Verification via QR Code or URL

Each batch in auditraks has a publicly accessible verification page that allows anyone — including customers, auditors, and end consumers — to confirm a batch's compliance status without needing an auditraks account.

**To verify a batch:**

- **Via QR code:** Scan the QR code on a Material Passport using any smartphone camera or QR code reader app. You will be taken directly to the batch verification page.
- **Via URL:** Navigate to the following address in any browser, replacing `{batchId}` with the actual batch identifier:

  ```
  /api/verify/{batchId}
  ```

  For example: `/api/verify/W-2026-041`

The public verification page displays:
- The batch number and mineral type.
- The origin country.
- The overall compliance status (COMPLIANT, FLAGGED, INSUFFICIENT_DATA, or PENDING).
- The date the compliance status was last updated.
- A summary of which compliance frameworks were evaluated.

The public verification page does **not** display detailed event data, document contents, or commercially sensitive information.

### 7.2 Shared Material Passport Links

When a buyer shares a Material Passport using the "Share" function (see Section 4.5), the recipient can view the passport via a secure, time-limited link without needing an auditraks account.

Shared passport links are valid for **30 days** from the date of creation. After expiry, the link will display an "This link has expired" message. Contact the buyer who shared the link to request a new one.

---

## 8. Tamper Evidence and Data Integrity

auditraks is designed so that the custody record for any batch cannot be secretly altered. This is achieved through cryptographic hash chaining and an immutable event log.

### 8.1 How SHA-256 Hash Chains Work

Every time an event is submitted to auditraks, the system generates a **cryptographic fingerprint** (called a SHA-256 hash) of the event's data — including the event type, all field values, the timestamp, the submitting user, and the fingerprint of the previous event in the chain.

Because each event's fingerprint depends on the previous event's fingerprint, the entire chain is mathematically linked. If any event were altered — even a single character changed — its fingerprint would change, and all subsequent events would no longer match. This makes undetected tampering computationally infeasible.

You do not need to understand the technical details to benefit from this protection. What matters is that auditraks gives auditors and buyers a reliable guarantee that the event log they are reviewing has not been modified since it was recorded.

### 8.2 Integrity Verification

Platform Admins can run an **integrity check** on any batch to verify that the hash chain is intact. If the check passes, it confirms that no event data has been altered since submission. If the check fails, it indicates a data integrity issue that requires investigation.

Buyers can see the integrity status of a batch on the Compliance Checks tab of the Batch Detail view.

### 8.3 Corrections

Because events cannot be deleted or edited, corrections to mistaken entries are handled by submitting a **Correction event**. A correction event:

- References the original event it is correcting (by event ID).
- Records the corrected values.
- Includes a mandatory explanation of what was changed and why.
- Is itself part of the immutable hash chain.

Both the original event and the correction remain visible in the event timeline, maintaining a complete and transparent record of all changes.

**To submit a correction:**

1. Open the Batch Detail view and locate the event that needs correcting in the Timeline tab.
2. Click the **"Submit Correction"** button on that event.
3. Enter the corrected values and a written explanation.
4. Click **"Submit"**.

The correction will be reviewed and linked to the original event in the timeline. Compliance checks are re-run after a correction is submitted.

---

## 9. Notifications and Email Alerts

### 9.1 In-App Notifications

auditraks displays notifications in the **notification bell** in the top navigation bar. A red dot appears on the bell when you have unread notifications.

Click the bell to open the notification dropdown, which shows your most recent notifications. Click on any notification to view details or navigate to the relevant batch.

### 9.2 Email Notifications

auditraks sends email notifications for the following events:

| Event                        | Recipients                                                                                       |
| ---------------------------- | ------------------------------------------------------------------------------------------------ |
| **User Invitation**          | The invited user receives a welcome email with a sign-in link.                                   |
| **Compliance Flag**          | The supplier who submitted the triggering event, and all Platform Admins.                        |
| **Compliance Status Change** | The supplier and buyers associated with the batch.                                               |
| **Document Generated**       | The user who requested the Material Passport or Audit Dossier.                                   |
| **48-Hour Escalation**       | All Platform Admins are notified if a compliance flag remains unresolved for more than 48 hours. |

Email notifications are sent automatically. If emails are not being delivered, check your spam/junk folder and ensure your organization's email system allows messages from the auditraks platform address.

> **Note:** The platform retries failed email deliveries up to 3 times. If you are not receiving emails after multiple days, contact your Platform Admin.

### 9.3 Escalation Policy

Compliance flags that remain unresolved for more than **48 hours** trigger an automatic escalation. All Platform Admins in the affected tenant receive an escalation notification (both in-app and via email) prompting them to review and resolve the flagged batch.

---

## 10. Troubleshooting and FAQ

### Sign-In Issues

**I did not receive my invitation email.**
Check your spam or junk folder. If it is not there, ask your Platform Admin to resend the invitation. Invitation emails are sent from the auditraks platform address — ask your IT team to add this to your allow-list if emails are being blocked.

**My invitation link has expired.**
Invitation links are valid for 7 days. Contact your Platform Admin to send a new invitation.

**I cannot sign in with Google.**
Ensure you are using the Google account associated with your auditraks invitation. If you signed up with a different Google account, contact your Platform Admin to update your registered email address.

**I am redirected to the wrong portal after signing in.**
Your role may have been assigned incorrectly. Contact your Platform Admin to verify and correct your role.

---

### Batch and Event Issues

**I entered the wrong information on an event. Can I edit it?**
Events cannot be edited after submission. Submit a Correction event referencing the original (see Section 8.3).

**My batch shows FLAGGED after I submitted a Primary Processing event.**
The smelter ID you entered may not be on the current RMAP-approved list. Check that the ID was entered correctly. If it is correct, contact your Platform Admin — the smelter list may need to be updated.

**My batch shows INSUFFICIENT_DATA.**
Review the Compliance Checks tab for the batch. The compliance engine has identified one or more missing documents or events. Add the required events or documents to resolve this status.

**I cannot find a batch I created.**
Ensure you are signed in to the correct account. Use the search bar on the Supplier Dashboard to search by batch number. If the batch still does not appear, it may have been created under a different supplier account — contact your Platform Admin.

---

### Document Issues

**My document upload is failing.**
Check that your file meets the requirements: supported formats are PDF, JPEG, PNG, TIFF, and GIF; maximum file size is 25 MB. If your file meets these requirements and the upload still fails, try a different browser or contact support.

**I uploaded the wrong document.**
Documents cannot be deleted. Upload the correct document and add a note in the description field indicating that the previous upload should be disregarded and specifying which document supersedes it.

---

### Compliance and Reports

**Why has my batch been flagged for an origin country I believe is low risk?**
auditraks uses origin country risk classifications based on published OECD guidance. If you believe the classification is incorrect, contact your Platform Admin — they can review the case and, if appropriate, override the flag with documented justification.

**I need to generate a Material Passport but the button is greyed out.**
Material Passports can only be generated for batches with a COMPLIANT status. Resolve any compliance flags or missing data issues first.

**A shared Material Passport link I sent has expired.**
Shared links are valid for 30 days. Generate a new shared link from the Batch Detail view and send it to the recipient.

**Can external parties access our batch data through a shared link?**
Shared Material Passport links display only the information included in the Material Passport itself. Detailed event logs, document contents, and commercially sensitive data are not exposed through shared links.

---

### General Questions

**Is my data secure?**
Yes. All data transmitted to and from auditraks is encrypted in transit using TLS. Stored data is encrypted at rest. Access to batch data is restricted to authorized users in the relevant supplier and buyer organizations, plus Platform Admins.

**Can multiple people in my organization use auditraks?**
Yes. Each person should have their own individual account. Contact your Platform Admin to invite additional users. Sharing login credentials is not permitted.

**What do I do if I suspect a data integrity issue?**
Contact your Platform Admin immediately. Do not attempt to correct the issue yourself. Platform Admins can run integrity checks and escalate to auditraks support if needed.

**How do I contact support?**
Contact your Platform Admin in the first instance. For platform-level issues that your admin cannot resolve, they will escalate to the auditraks support team.

---

*auditraks User Manual — Version 2.0 — March 2026*

*This document is provided for guidance purposes. Features and interfaces may change as the platform is updated. For the most current information, refer to the latest version of this manual.*
