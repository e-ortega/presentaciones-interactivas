> **Reference only.** Notes generated with Gemini before this project (Esteban's email to himself, 8 Oct 2026, "Project Specification & Technical Documentation: Interactive Presentation Platform"). These are not decisions: current decisions live in `docs/decisiones.md`. In particular, this project uses **SQL Server** (not PostgreSQL), **a single app** (not microservices) and an **email-based identity** (not fully anonymous access).

# Comprehensive System Architecture & Project Documentation: Interactive Presentation Platform

## Executive Summary & Core Value Proposition
The Interactive Presentation Platform is a modern web-based presentation solution designed to bridge the gap between static presentations and audience engagement. By pairing a real-time synchronized slideshow interface with interactive widgets (quizzes, word clouds, Q&A channels) and automated AI-driven insights, the platform enables educators and speakers to gauge comprehension instantly, foster active participation, and adapt content dynamically without breaking lecture flow.

---

## 1. Product Features & MVP Scope

### 1.1 Slide Management & Web Editor
* **File Import:** Support for uploading static presentation files (PPTX and PDF) converted into scalable, lightweight slide assets.
* **Minimalist Web Editor:** Built-in web editor designed for rapid creation and light modification of slides without requiring full desktop software functionality.
  * **Supported Canvas Elements:** Text blocks, basic shape containers, images, background colors, and typography controls (font family, size, alignment, color).
  * **Design Philosophy:** Keep editor tools minimal to prevent feature bloat and minimize development overhead, encouraging users to import established decks or construct clean, high-impact interactive slides natively.

### 1.2 Presentation Navigation & Interaction Control
* **Real-Time Synchronized Navigation:** Manual slide progression controlled by the presenter, broadcasting current state instantly to all connected student devices.
* **Granular Widget Activation:** Presenters selectively trigger interactive components (e.g., locking/unlocking quizzes or launching word clouds) on specific slides. Interactivity is intentionally modular to prevent cognitive overload.

### 1.3 Core MVP Interactive Widgets
* **Multiple Choice / Live Quizzes:** Single or multi-answer questions for immediate formative assessment and knowledge checking.
* **Word Cloud Generator:** Live visual aggregation of short text responses for brainstorming, pulse checks, and sentiment analysis.
* **Audience Q&A & Live Chat:** Moderated channel allowing students to submit questions or upvote peers' questions during the presentation.

### 1.4 Real-Time Analytics & Feedback
* **Aggregated Performance Dashboards:** Instant visual representation of responses (bar charts, distribution graphs, word clusters) displayed on the presenter console.
* **Session Summaries:** Post-session reports highlighting overall participation rates, question accuracy, and key engagement metrics.

---

## 2. User Experience (UX) Flows & Interface Design

### 2.1 Presenter / Speaker UX Flow
1. **Preparation Phase:**
   * Log in to the presenter dashboard.
   * Upload existing PPTX/PDF decks or build a deck using the lightweight web editor.
   * Insert interactive widgets onto target slides and configure parameters (e.g., question text, choices, correct options, submission time limits).
2. **Delivery Phase:**
   * Launch a live presentation session, generating a unique Session PIN and join URL / QR Code.
   * Navigate through slides; the student view updates synchronously in real time.
   * Activate interactive widgets on demand, view live incoming responses, and lock submissions when complete.
3. **Review Phase:**
   * Access post-presentation analytics, export session reports, and review AI-generated summary insights.

### 2.2 Student / Audience UX Flow
1. **Joining Phase:**
   * Access the web portal via session URL, scanned QR code, or by entering the Session PIN.
   * Join anonymously without requiring an account or personal identification.
2. **Participation Phase:**
   * View the active slide broadcasted by the presenter in real time.
   * When a widget is activated by the speaker, the student interface seamlessly overlays the input controls (quiz options, text submission input, or Q&A box).
   * Submit responses and view aggregated results if shared by the presenter.

---

## 3. Technical Architecture & Data Infrastructure

### 3.1 High-Level Architecture
* **Frontend Layer:** Single Page Application (SPA) built for cross-device web compatibility (desktop, tablet, mobile), optimizing for low latency and smooth canvas rendering.
* **Backend Microservices / Services:**
  * **Auth & User Management Service:** Handles presenter accounts, credentials, and session authorization.
  * **Presentation & Media Service:** Manages deck assets, slide metadata, and canvas elements.
  * **Real-Time Communication Service:** WebSocket engine (handling pub/sub channels) responsible for low-latency synchronization of slide indexes, widget state changes, and live response streams.
* **AI Integration Layer:** Integrates with external LLM APIs (e.g., OpenAI / Gemini) to handle background tasks such as automated question generation, distractor option suggestions, and Q&A semantic clustering.

### 3.2 Database Schema & Data Models

```sql
-- Users (Presenters / Instructors)
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Presentations
CREATE TABLE presentations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Slides
CREATE TABLE slides (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    presentation_id UUID REFERENCES presentations(id) ON DELETE CASCADE,
    slide_order INT NOT NULL,
    content_json JSONB NOT NULL, -- Stores text, shapes, image URIs, styling
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Interactive Widgets
CREATE TABLE widgets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slide_id UUID REFERENCES slides(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL, -- 'MULTIPLE_CHOICE', 'WORD_CLOUD', 'QA'
    config_json JSONB NOT NULL, -- Questions, options, time limits
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Live Sessions
CREATE TABLE sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    presentation_id UUID REFERENCES presentations(id) ON DELETE CASCADE,
    session_code VARCHAR(10) UNIQUE NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    current_slide_id UUID REFERENCES slides(id),
    started_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    ended_at TIMESTAMP WITH TIME ZONE
);

-- Anonymous Student Responses
CREATE TABLE responses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id UUID REFERENCES sessions(id) ON DELETE CASCADE,
    widget_id UUID REFERENCES widgets(id) ON DELETE CASCADE,
    anonymous_participant_token VARCHAR(64) NOT NULL, -- Rotated session-bound identifier
    payload_json JSONB NOT NULL, -- Submitted answer choices or text string
    submitted_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### 3.3 Privacy & Data Anonymization Strategy
* **Zero-Knowledge Student Access:** Students require no registration, emails, or personal identifiers to participate.
* **Session-Bound Participant Tokens:** Responses are tagged using temporary, pseudo-random session tokens stored only in transient memory to enforce single-submission rules without tracking real-world identities.
* **Data Aggregation:** All analytical outputs and exports aggregate student inputs to protect individual privacy while giving instructors accurate overall comprehension metrics.

---

## 4. Scalability Strategy & Future Roadmap

### 4.1 System & Technical Scalability
* **WebSocket Connection Management:** Horizontally scalable WebSocket clusters utilizing a Redis Pub/Sub backplane to support concurrent live sessions with high audience counts.
* **Stateless Response Processing:** Response ingest pipeline designed for high throughput during flash-crowd moments (e.g., thousands of students answering a quiz simultaneously).

### 4.2 Future Feature Roadmap
1. **Real-Time AI Quiz & Content Adaptation:** Dynamically adapt quiz difficulty or generate follow-up questions in real time based on audience comprehension levels.
2. **LMS Integration (Canvas, Moodle, Blackboard):** Gradebook synchronization and LTI interoperability for optional non-anonymous formative assessment workflows.
3. **Gamification & Leaderboards:** Optional engagement mechanics, including competitive quiz modes, response speed scoring, and live leaderboards.
4. **Enhanced Analytics:** Longitudinal progress tracking across multiple course sessions to detect systemic learning gaps over time.

---
*Document prepared for project archive, technical migration, and downstream AI consumption.*
