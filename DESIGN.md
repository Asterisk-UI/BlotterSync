---
name: BlotterSync
description: The existing slate and blue officer workspace, extended for reviewed incident assistance.
colors:
  blue: "#2559ce"
  blue-hover: "#1944a5"
  focus: "#5686f0"
  ink: "#1e293b"
  muted: "#58677c"
  line: "#e1e7ef"
  surface: "#fff"
  canvas: "#f6f8fb"
  slate-navigation: "#172538"
  navigation-text: "#b9c8dc"
  navigation-hover: "#23364e"
  navigation-active: "#2a4161"
  secondary-text: "#2c3e56"
  secondary-line: "#d9e1ec"
  secondary-hover: "#f0f4fa"
  quiet-hover: "#eaf0fc"
  field-line: "#ccd6e4"
  pending-text: "#855705"
  pending-surface: "#fff4d9"
  ongoing-text: "#2456a9"
  ongoing-surface: "#eaf1fe"
  accepted-text: "#226344"
  accepted-surface: "#e9f5ed"
  inactive-text: "#566274"
  inactive-surface: "#edf0f4"
  failed-text: "#a82a37"
  failed-surface: "#ffebee"
typography:
  headline:
    fontFamily: '"Segoe UI", -apple-system, BlinkMacSystemFont, Arial, sans-serif'
    fontSize: "1.875rem"
    fontWeight: 650
    lineHeight: 1.2
    letterSpacing: "-0.035em"
  title:
    fontFamily: '"Segoe UI", -apple-system, BlinkMacSystemFont, Arial, sans-serif'
    fontSize: "1rem"
    fontWeight: 650
    letterSpacing: "-0.015em"
  body:
    fontFamily: '"Segoe UI", -apple-system, BlinkMacSystemFont, Arial, sans-serif'
    fontSize: "0.875rem"
    lineHeight: 1.75
  action:
    fontFamily: '"Segoe UI", -apple-system, BlinkMacSystemFont, Arial, sans-serif'
    fontSize: "0.875rem"
    fontWeight: 600
  label:
    fontFamily: '"Segoe UI", -apple-system, BlinkMacSystemFont, Arial, sans-serif'
    fontSize: "0.75rem"
    fontWeight: 500
rounded:
  badge: "5px"
  control: "6px"
  navigation: "7px"
  panel: "12px"
spacing:
  compact: "8px"
  small: "12px"
  medium: "16px"
  content: "20px"
  section: "24px"
  column: "25px"
components:
  button-primary:
    backgroundColor: "{colors.blue}"
    textColor: "{colors.surface}"
    typography: "{typography.action}"
    rounded: "{rounded.control}"
    padding: "10px 15px"
  button-primary-hover:
    backgroundColor: "{colors.blue-hover}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.secondary-text}"
    typography: "{typography.action}"
    rounded: "{rounded.control}"
    padding: "10px 15px"
  button-secondary-hover:
    backgroundColor: "{colors.secondary-hover}"
  button-quiet:
    backgroundColor: "transparent"
    textColor: "{colors.blue}"
    typography: "{typography.action}"
    rounded: "{rounded.control}"
    padding: "10px 4px"
  button-quiet-hover:
    backgroundColor: "{colors.quiet-hover}"
  input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "10px 11px"
    width: "100%"
  navigation:
    backgroundColor: "transparent"
    textColor: "{colors.navigation-text}"
    rounded: "{rounded.navigation}"
    padding: "11px 13px"
  navigation-active:
    backgroundColor: "{colors.navigation-active}"
    textColor: "{colors.surface}"
  badge-pending:
    backgroundColor: "{colors.pending-surface}"
    textColor: "{colors.pending-text}"
    typography: "{typography.label}"
    rounded: "{rounded.badge}"
    padding: "4px 8px"
  panel:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.panel}"
  workflow-option:
    backgroundColor: "transparent"
    textColor: "#253c5c"
    padding: "16px 0"
    width: "100%"
  source-disclosure:
    textColor: "#445b7a"
---

# Design System: BlotterSync

## Overview

**Creative North Star: "BlotterSync officer workspace"**

This is a record of the implemented officer interface, preserving the repository's BlotterSync identity. Slate navigation frames a light working canvas; blue identifies actions, while compact type and clear borders support scanning incident records and reviewing source text. The descriptor names the existing surface rather than proposing a new visual world.

The source of truth for this extraction is `BlotterSync/wwwroot/workspace.css`, with component structure and behavior in `workspace.html` and `workspace.js`. This record covers that extension; it does not claim that every legacy route has been migrated to these measurements. The Segoe UI/system stack is inherited product identity.

**Key Characteristics:**
- Slate navigation, blue actions, white bordered working surfaces.
- Compact sentence-case labels, native fields and readable incident narratives.
- Flat persistent panels, with shadows reserved for transient overlays.
- Explicit text labels distinguish incident status, draft review and assistance mode.

## Colors

The palette retains the incumbent slate and blue identity, with pale semantic surfaces for record and draft states. Frontmatter contains the measured values; sidecar ramps are generated preview metadata, not additional implemented palette steps.

### Primary
- **Action blue:** primary actions, source links and input carets; the deeper hover shade communicates pointer interaction.
- **Focus blue:** a shared visible outline for keyboard interaction.

### Neutral
- **Navigation slate:** persistent sidebar and authentication story background; navigation hover and active fills remain within the slate family.
- **Ink and muted slate:** main content and supporting context respectively.
- **White surface and light canvas:** panels remain distinct from the page without resting shadows.
- **Divider and field strokes:** distinguish section boundaries from editable controls.

State colors are semantic pairs: amber for Pending and Needs review, blue for Ongoing, green for Resolved and Accepted, gray for Dismissed, Rejected and Cancelled, and red for Failed. Each state also carries its written label.

**The Labeled State Rule.** Preserve the visible state name alongside its semantic color.

## Typography

The inherited Segoe UI/system stack applies throughout. The root size is (16px); font families and observed role values are normative in the frontmatter. No additional display typeface is introduced.

- **Headline:** page headings use the headline role; below the mobile breakpoint the size becomes (1.5rem).
- **Title:** panel headings use the title role. Intake section titles use (1.125rem); record detail titles also use (1.125rem).
- **Body:** incident narratives use the body role, preserve line breaks and wrap long content. General form and supporting text uses (0.875rem), with context-dependent line heights from (1.5) to (1.75).
- **Action:** buttons and form labels use the action weight and size.
- **Label:** badges and table headers use the label role. Supporting metadata commonly uses (0.8125rem) or (0.75rem); tracking numbers and metric values use tabular numerals.

The assistant introduction uses (1.4rem), weight (600), line height (1.35) and a (20ch) maximum width; it changes to (1.3rem) on mobile. Settings descriptions are capped at (65ch). These are specific component measurements, not a replacement type scale.

## Layout

The desktop shell has a sticky full-height sidebar (232px) and a topbar (76px). Main content is centered within (1600px) and uses (28px 38px 38px) padding. The workspace grid gives the register flexible width and the assistant (336px), separated by (25px). Four incident metrics sit above it with dividers rather than independent cards.

At widths of at least (1600px), the assistant grows to (370px). At (1200px) and below, main padding becomes (25px), the sidebar (210px), and the assistant (305px) with an (18px) gap. At (980px) and below, the sidebar is (200px) and the workspace becomes one column. The assistant follows the working panel; selecting a record or run scrolls it into view below (981px).

At (700px) and below, the sidebar becomes a toggleable fixed drawer beneath a sticky (65px) topbar. Main padding becomes (20px 17px 25px), metrics form two columns, and form grids stack. Table scrolling stays inside its wrapper; location columns are hidden at the compact desktop and mobile breakpoints and restored in the intermediate stacked layout. The sign-in layout also collapses to one column.

Spacing is contextual: controls are compact, table cells have generous row padding, and section dividers separate review steps. The frontmatter records reused spacing values rather than imposing an inferred universal grid.

## Elevation & Depth

Persistent panels are flat: white fill, a thin divider-colored border and gently rounded corners. Navigation and statuses use tonal fills. Only the toast and mobile navigation drawer carry shadows; exact shadow values live in the sidecar. No resting panel shadow is present.

**The Flat Panel Rule.** Use borders and tonal surfaces for persistent working panels; reserve the implemented shadows for transient overlays.

## Shapes

Panels use the panel radius; controls use the control radius; badges and navigation use their respective smaller radii. Borders remain thin and quiet. Status badges are compact rounded rectangles rather than pills. The avatar alone is circular; the blue brand mark and assistant symbol are rounded squares. Icons are inline outlined SVG, with rounded caps and joins.

## Components

### Buttons

Primary, secondary and quiet buttons share compact semibold labels, an icon/text gap (8px), and a minimum height (41px). Primary buttons are blue; secondary buttons are white with a thin stroke; quiet buttons use blue text and narrower horizontal padding. Hover changes the background. All interactive controls share a visible focus outline (3px) with offset (3px). Disabled buttons show a wait cursor and opacity (0.6). Background and text transitions use (0.18s ease-out); there is no bespoke pressed animation. On mobile, review action buttons have a minimum height (44px).

### Inputs / Fields

Native inputs, selects and vertically resizable textareas use white surfaces, field strokes and the control radius. Labels remain visible above fields; help and error text sit adjacent. Textareas use line height (1.65). Keyboard focus uses the same outline as buttons. Required fields and the explicit verification checkbox remain native controls; validation does not rely on color alone.

### Navigation

Full-width, left-aligned navigation rows use outlined SVG icons and a minimum height (46px). Hover and active states use separate slate fills; active navigation also sets `aria-current="page"`. The mobile menu button maintains `aria-expanded`, and Escape closes the drawer.

### Chips / Status badges

Badges are informational, with label-size text, compact padding and semantic foreground/background pairs. Accepted is a review state; Pending is an incident state. Their colors do not make them interchangeable. Badges have no pointer interaction or fabricated hover treatment.

### Cards / Containers

Panels share white fill, a thin divider stroke and the panel radius. Register headings and filters are inset; table rows use separators and clickable incident titles. Search and status filtering update the register in place. Empty views, global errors and local filing errors retain the same type and surface vocabulary.

### Desk assistant and source disclosures

The assistant keeps the same panel while changing from guidance to record detail, request progress, result and review actions. Workflow choices are full-width divided rows with a label, short explanation and outlined SVG icons. Source comparisons and tool activity use native `details`/`summary` disclosures. Incident narratives preserve original line breaks and wrap long strings.

Every draft displays its generation mode and review status. Accepting an intake transfers its persisted original notes and draft into the filing form; accepted history entries offer “Use in filing form” to recover the same draft later. Older runs without stored notes explicitly identify the missing source. Accepting a brief records review, while filing remains a separate form submission with a required verification checkbox. Successful filing produces a Pending incident.

Request progress uses a (3px) loading track with a moving (40%) segment over (1.4s ease-in-out); reduced-motion preference disables animation and transitions. Errors support retry, pending requests support cancellation, and a status toast lasts (4500ms). These behaviors are extracted from the implementation, not new component variants.

## Do's and Don'ts

### Do:
- **Do** retain the BlotterSync name, inherited Segoe UI/system stack, slate navigation and blue actions.
- **Do** keep status names, assistance mode and source comparisons visible in review flows.
- **Do** reuse bordered working panels, native labeled fields and shared keyboard focus treatment.
- **Do** preserve line breaks and wrapping for incident and source narratives.

### Don't:
- **Don't** treat a draft acceptance state as an incident filing state.
- **Don't** add resting shadows to the flat working panels described by this extraction.
- **Don't** treat generated sidecar tonal ramps as implemented color tokens.
- **Don't** assume this workspace extraction replaces the styling of untouched legacy routes.
