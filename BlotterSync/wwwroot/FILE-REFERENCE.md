# BlotterSync Standalone - File Reference

## 📁 Complete File Structure

```
BlotterSync/
├── wwwroot/
│   ├── index-standalone.html         ← START HERE
│   ├── workspace-standalone.html          
│   ├── test-suite.html               
│   │
│   ├── workspace-app.js              # Main application logic (841 lines)
│   ├── storage.js                    # Data persistence layer
│   ├── init-data.js                  # Demo data initialization
│   ├── ui.js                         # UI helper utilities
│   ├── tests.js                      # Test suite
│   │
│   ├── workspace-standalone.css      # Main stylesheet (26 KB)
│   │
│   ├── README-STANDALONE.md          # Detailed documentation
│   ├── IMPLEMENTATION-COMPLETE.md    # Implementation status
│   ├── QUICKSTART.md                 # Quick start guide
│   └── FILE-REFERENCE.md             # This file
│
└── [Original backend files unchanged]
	├── Program.cs
	├── Controllers/
	├── Services/
	├── Data/
	└── ...
```

---

## 📋 File Descriptions

### 🔴 Entry Point
**`index-standalone.html`** (535 lines)
- Purpose: Authentication & registration gateway
- Features:
  - Login form with validation
  - User registration
  - Demo account quick-access
  - Stores credentials in localStorage
  - Redirects to workspace-standalone.html after login
- Loads: storage.js, init-data.js

---

### 🟢 Application Shell
**`workspace-standalone.html`** (200+ lines)
- Purpose: Main HTML structure and UI container
- Features:
  - Responsive sidebar navigation
  - Topbar with user info
  - Page content area
  - Toast notifications
  - Modal/dialog containers
  - Authentication overlay
  - SVG icon definitions
- Loads: workspace-app.js, workspace-standalone.css
- Styling: workspace-standalone.css

---

### 🔵 Application Logic
**`workspace-app.js`** (841 lines)
- Purpose: Core application functionality
- Key Functions:
  - `boot()` - Initializes app on page load
  - `navigate()` - Handles page navigation
  - `showAuth()` / `login()` / `logout()` - Authentication
  - `metrics()` - Dashboard statistics
  - `render()` - Renders current page
  - `renderRows()` - Displays incident table
  - `viewRecord()` / `editRecord()` / `deleteRecord()` - CRUD operations
  - `intakeForm()` - New incident form
  - `generateIntakeDraft()` - AI-style incident generation
  - `historyView()` - Agent run history
  - `settingsView()` - Settings page
  - `exportData()` - Export as JSON
- Dependencies: storage.js, init-data.js
- No API calls (all localStorage-based)

---

### 💾 Data Persistence Layer
**`storage.js`** (300+ lines)
- Purpose: localStorage wrapper and CRUD operations
- Key Methods:
  - `getUsers()` / `findUser()` / `saveUser()` - User management
  - `getCurrentUser()` / `setCurrentUser()` / `logout()` - Session
  - `getRecords()` / `getRecord()` / `saveRecord()` / `deleteRecord()` - Incidents
  - `getCategories()` / `saveCategory()` - Categories
  - `getResidents()` / `saveResident()` - Residents
  - `getAgentRuns()` / `saveAgentRun()` - Agent history
  - `getSyncStatus()` / `updateSyncStatus()` - Sync tracking
  - `generateId()` - ID generation
  - `exportData()` / `importData()` - Backup/restore
  - `clearAll()` - Full reset
- Storage Model: BlotterStorage class backed by window.localStorage
- No server calls

---

### 📦 Initial Data
**`init-data.js`** (200+ lines)
- Purpose: Seed demo data on first load
- Features:
  - 3 demo users (officer, maria, admin)
  - 12 incident categories (theft, assault, etc.)
  - 5 mock residents (complainants/witnesses)
  - 6 sample incidents with realistic data
  - 2 agent run history entries
  - Local sync status
- Guard: Uses `blotter_initialized` flag to prevent re-seeding
- Called by: Both standalone HTML pages on load

---

### 🎨 Styling
**`workspace-standalone.css`** (26 KB, 1,457 lines)
- Purpose: Complete application styling
- Includes:
  - Responsive layout (mobile/tablet/desktop)
  - Sidebar navigation styling
  - Topbar design
  - Page layouts:
	- Overview (dashboard)
	- Incidents (table list)
	- Intake (form)
	- History (timeline)
	- Settings (panels)
  - Form styling
  - Table styling
  - Button styles
  - Modal/dialog styling
  - Dark mode support (respects system preference)
  - Mobile responsiveness
- Copied from: Original workspace.css

---

### 🧪 Testing
**`tests.js`** (251 lines)
- Purpose: Automated test suite for browser console
- Test Functions (10 total):
  1. `testStorageInitialization()` - Storage layer ready
  2. `testUserAuthentication()` - Login functionality
  3. `testDataPersistence()` - localStorage persistence
  4. `testRecordCreation()` - Create incidents
  5. `testSearch()` - Search functionality
  6. `testFilter()` - Filter by status
  7. `testRecordUpdate()` - Update incidents
  8. `testRecordDeletion()` - Delete incidents
  9. `testExport()` - Export data
  10. `testDataIntegrity()` - Data validation
- Usage: Run in console: `BlotterSyncTests.runAllTests()`
- Auto-runs on load if included in HTML

---

### 🎯 Visual Test Dashboard
**`test-suite.html`** (450+ lines)
- Purpose: Interactive browser-based test UI
- Features:
  - System status display
  - Individual test buttons
  - Data operations (show, check, export, reset)
  - Persistence verification
  - Real-time stats display
  - Color-coded results (success/error/info)
- No dependencies on app.js (standalone)
- Opens in separate browser tab

---

### 🛠️ UI Utilities
**`ui.js`** (300+ lines)
- Purpose: Reusable UI helper functions
- Components:
  - `UIHelpers.modal()` - Create modal dialogs
  - `UIHelpers.alert()` / `UIHelpers.confirm()` - Simple alerts
  - `UIHelpers.table()` - Generate HTML tables
  - `UIHelpers.form()` - Generate HTML forms
  - `UIHelpers.dateFormat()` - Format dates
  - `UIHelpers.truncate()` - Truncate strings
  - `UIHelpers.debounce()` / `UIHelpers.throttle()` - Performance
  - `UIHelpers.escapeHTML()` - XSS prevention
  - `UIHelpers.getInitials()` - Avatar initials
  - `UIHelpers.getStatusColor()` - Status colors
  - `UIHelpers.copyToClipboard()` - Clipboard operations
  - `UIHelpers.downloadJSON()` - Download files
- Status: Created but not currently loaded (workspace-app.js has own helpers)
- Available for: Future enhancements

---

## 📚 Documentation Files

### README-STANDALONE.md
- Comprehensive feature documentation
- API reference for all functions
- Manual testing workflows
- Limitations and known issues
- Troubleshooting guide
- Storage key reference

### IMPLEMENTATION-COMPLETE.md
- Project completion status
- Files created and their purposes
- Implementation architecture
- Quick start instructions
- Demo data summary
- Technical specifications

### QUICKSTART.md
- 30-second getting started guide
- Common tasks and how-tos
- Troubleshooting tips
- Mobile/PWA instructions
- Feature checklist

### FILE-REFERENCE.md (this file)
- Complete file structure
- Detailed file descriptions
- Function references
- Data models
- Dependencies

---

## 🔗 Dependencies & Data Flow

### Initialization Sequence
```
1. Browser loads index-standalone.html
2. Loads storage.js (creates BlotterStorage global)
3. Loads init-data.js (calls initializeDemoData())
4. On login: redirects to workspace-standalone.html
5. workspace-standalone.html loads workspace-app.js
6. workspace-app.js calls boot() and starts event listeners
```

### Component Dependencies
```
index-standalone.html
├── storage.js        (for user lookups)
├── init-data.js      (for demo data)
└── [no framework]    (vanilla JS)

workspace-standalone.html
├── storage.js        (for all data access)
├── init-data.js      (already loaded, for emergency reset)
├── workspace-app.js  (main app logic)
└── workspace-standalone.css (styling)

workspace-app.js
├── storage.js        (all data operation)
├── window.jwtToken   (session flag only)
└── [no API calls]    (fully offline)

tests.js
├── storage.js        (testing storage)
├── init-data.js      (seeded data)
└── window.* (global state)

test-suite.html
├── storage.js
├── init-data.js
├── tests.js
└── [no framework]
```

### Data Model
```
localStorage Keys:
├── blotter_users → array of users
├── blotter_records → array of incidents
├── blotter_categories → array of categories
├── blotter_residents → array of residents
├── blotter_agents → array of agent runs
├── blotter_sync_status → sync metadata
├── blotter_announcements → notifications
├── blotter_current_user → logged-in user
├── jwtToken → session compatibility flag
└── blotter_initialized → guard flag
```

---

## 🚀 Usage Flows

### User Login Flow
```
index-standalone.html
  ↓
User enters credentials
  ↓
storage.findUser(username, password)
  ↓
storage.setCurrentUser(user)
  ↓
Redirect to workspace-standalone.html
  ↓
workspace-app.js boot()
  ↓
show() page based on route
```

### Create Incident Flow
```
User clicks "+ New Incident"
  ↓
intakeForm() renders form
  ↓
User fills & submits
  ↓
workspace-app.js calls storage.saveRecord()
  ↓
storage.js saves to localStorage['blotter_records']
  ↓
renderRows() updates table
```

### Search/Filter Flow
```
User types in search box
  ↓
Event listener triggers
  ↓
state.search = value
  ↓
renderRows() filters records:
  includes(search) && status matches
  ↓
Table updates in real-time
```

### Export Data Flow
```
User clicks "Export Data"
  ↓
exportData() calls storage.exportData()
  ↓
storage.js returns all data as object
  ↓
JSON.stringify(data)
  ↓
Browser download dialog
  ↓
Save as .json file
```

---

## 🔐 Authentication Model

### Local Session Storage
```javascript
// User object in localStorage
{
  userId: "user_1",
  username: "officer",
  firstName: "John",
  lastName: "Officer",
  email: "john@example.com",
  role: "Officer"
}

// Session flag (compatibility only)
localStorage.jwtToken = "local_session_" + timestamp
```

### No Real JWT
- No server-side signature validation
- No token expiration (page refresh re-validates)
- No role-based access control (client-side only)
- Demo-only authentication

---

## 📊 Data Structures

### User Object
```javascript
{
  userId: string,
  username: string,
  firstName: string,
  lastName: string,
  email: string,
  role: "Officer" | "Admin"
}
```

### Record (Incident) Object
```javascript
{
  recordId: string,
  trackingNumber: string,
  categoryId: string,
  location: string,
  narrative: string,
  status: "Pending" | "Ongoing" | "Resolved" | "Dismissed",
  officerId: string,
  complainantId: string,
  incidentDate: ISO string,
  reportedDate: ISO string,
  createdAt: ISO string,
  updatedAt: ISO string
}
```

### Category Object
```javascript
{
  categoryId: string,
  name: string,
  description: string,
  severity: 1-5
}
```

---

## ⚙️ Configuration

### No Config Files
- Zero configuration needed
- All settings in code
- No environment variables
- No API keys required

### Customization Points
In `init-data.js`:
- Add/remove users
- Change demo incidents
- Modify categories
- Update residents

In `workspace-app.js`:
- Change page layouts
- Modify form fields
- Adjust search logic
- Customize styling

---

## 🎯 File Size Summary

| File | Size | Lines | Purpose |
|------|------|-------|---------|
| workspace-app.js | ~25 KB | 841 | Core logic |
| workspace-standalone.css | 26 KB | 1,457 | Styling |
| storage.js | ~12 KB | 300+ | Data layer |
| tests.js | ~8 KB | 251 | Test suite |
| index-standalone.html | ~20 KB | 535 | Login page |
| test-suite.html | ~18 KB | 450+ | Test UI |
| init-data.js | ~8 KB | 200+ | Demo data |
| workspace-standalone.html | ~15 KB | 200+ | App shell |
| ui.js | ~12 KB | 300+ | Utilities |
| **Total** | **~144 KB** | | All together |

---

## ✨ Browser Compatibility

### Testing Environments
- ✅ Chrome 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Edge 90+
- ✅ Mobile browsers (iOS/Android)

### Required Features
- ES6 JavaScript
- localStorage
- Modern CSS Grid/Flexbox
- Fetch API (not used, but available)

---

## 🔄 Update Guide

To update the standalone version:

1. **Bug fixes**: Edit relevant `.js` file
2. **New features**: Add to `workspace-app.js`
3. **Styling changes**: Edit `workspace-standalone.css`
4. **Add pages**: Extend `workspace-standalone.html` HTML
5. **Add data types**: Update `storage.js` methods
6. **Update demo data**: Modify `init-data.js`

No backend changes needed!

---

## 🆘 Common Issues & Solutions

| Issue | File | Solution |
|-------|------|----------|
| App won't load | index-standalone.html | Check console, verify file path |
| Data lost | storage.js | Check localStorage isn't cleared |
| Styling broken | workspace-standalone.css | Verify CSS file loaded (F12 Network) |
| Tests fail | tests.js | Run `storage.getRecords()` to verify data |
| Search not working | workspace-app.js | Check filter logic, try different term |
| Login fails | storage.js | Verify demo users exist via console |

---

**BlotterSync Standalone v1.0** | Pure Frontend • No Backend • Browser Storage

Last Updated: 2024
