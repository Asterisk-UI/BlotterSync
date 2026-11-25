# BlotterSync Standalone HTML/CSS/JavaScript Version

This is a complete standalone conversion of BlotterSync from ASP.NET Core C# backend to pure HTML, CSS, and JavaScript with local storage persistence.

## 🎯 Overview

**BlotterSync Standalone** is a fully functional incident management system that runs entirely in the browser with no backend server required. All data is stored locally using browser localStorage.

### ✅ Features

- **Local Storage**: All data persists in your browser's localStorage (no server needed)
- **User Authentication**: Client-side login system with demo accounts
- **Incident Management**: Create, view, edit, and delete incident records
- **Status Tracking**: Track incidents as Pending, Ongoing, Resolved, or Dismissed
- **Search & Filter**: Find incidents by keyword or status
- **Intake Assistant**: Draft new incidents with AI-like assistance (local extraction mode)
- **Agent History**: View all drafted and reviewed incidents
- **Export Data**: Download all your data as JSON
- **Responsive Design**: Works on desktop, tablet, and mobile devices

## 🚀 Getting Started

### Files Structure

```
wwwroot/
├── index-standalone.html      # Login page (entry point)
├── workspace-standalone.html  # Main application shell
├── workspace-app.js          # Complete app logic
├── workspace-standalone.css  # All styling
├── storage.js               # LocalStorage API layer
├── init-data.js            # Demo data initialization
└── ui.js                   # UI helper utilities
```

### Running the App

1. **Open the login page**: Open `index-standalone.html` in a web browser
2. **Demo Login**: Click "Explore Demo" or use credentials:
   - Username: `officer`
   - Password: `password123`
3. **Create New Account**: Use the "Create Officer Account" link to register new users

## 👤 Demo Users

The app comes with three pre-configured demo users:

| Username | Password | Role | Badge |
|----------|----------|------|-------|
| officer | password123 | Officer | PO1-001 |
| maria | password123 | Officer | PO1-002 |
| admin | admin123 | Admin | PO1-999 |

## 📝 Sample Data

The app includes demo data:
- **6 Sample Incidents**: Various incident types with realistic details
- **12 Incident Categories**: Theft, Assault, Vandalism, Noise, etc.
- **5 Mock Residents**: With contact and zone information
- **2 Agent Runs**: Example AI-assisted drafts

All demo data can be reset from the Settings page.

## 🛠️ Technical Details

### Architecture

```
Authentication (login page)
		↓
Browser LocalStorage (storage.js)
		↓
App Logic (workspace-app.js)
		↓
UI Rendering (workspace-standalone.html + CSS)
```

### Data Persistence

All data is stored in browser localStorage under these keys:
- `blotter_users`: Officer accounts
- `blotter_records`: Incident records
- `blotter_categories`: Incident categories
- `blotter_residents`: Person records
- `blotter_runs`: Agent run history
- `blotter_sync_status`: Sync status info
- `blotter_announcements`: System announcements
- `blotter_current_user`: Current session user ID
- `jwtToken`: Session token flag

### No Internet Required

- Runs completely offline
- No API calls to external servers
- No database connections
- All processing happens in your browser

## 🧪 Testing Workflows

### Test 1: Create New Incident

1. Click "New incident" button
2. Enter initial notes in the textarea
3. Click "Prepare intake draft"
4. Fill in the form:
   - Category (select from dropdown)
   - Location
   - Incident date
   - Complainant name
   - Narrative
5. Click "File incident"
6. Go to "Incident register" to verify it appears

**Expected Result**: New incident appears in list with "Pending" status

### Test 2: Search & Filter

1. Go to "Incident register"
2. In the search box, type part of an incident name (e.g., "theft")
3. Use the status filter dropdown to show only "Pending" incidents
4. Switch filters to verify filtering works

**Expected Result**: List updates to show only matching incidents

### Test 3: View & Edit Record

1. Click on any incident in the table
2. Review the full details
3. Click "Edit record"
4. Change the status or narrative
5. Click "Save changes"

**Expected Result**: Record updates and changes persist

### Test 4: Export Data

1. Go to Settings (Connections page)
2. Click "Export data"
3. Check that a JSON file downloads with all incidents

**Expected Result**: JSON file contains all records, categories, residents, users

### Test 5: Session Persistence

1. Create a new incident
2. Refresh the page (F5)
3. Log in again with same credentials
4. Go to "Incident register"

**Expected Result**: The incident you created is still there

### Test 6: Multiple Users

1. Log out (click profile → Sign out)
2. Login with a different user (e.g., "maria" / "password123")
3. Create an incident
4. Log out and login as "officer"
5. Check if maria's incident is visible (it should be in shared register)

**Expected Result**: Both users can see each other's incidents

## 📊 Data Operations

### Create Incident
```javascript
const record = storage.saveRecord({
  recordId: storage.generateId(),
  trackingNumber: "BL-2024-001",
  categoryId: "cat_theft",
  location: "Main Street",
  narrative: "Incident details...",
  status: "Pending",
  // ... other fields
});
```

### Search
```javascript
const results = state.records.filter(r => 
  r.location.includes("street") &&
  r.status === "Pending"
);
```

### Export All Data
```javascript
const data = storage.exportData();
const json = JSON.stringify(data, null, 2);
// Download as file
```

## 🎨 UI Customization

The app uses CSS variables for theming. Edit `workspace-standalone.css`:

```css
:root {
  --ink: #1e293b;        /* Text color */
  --muted: #58677c;      /* Muted text */
  --line: #e1e7ef;       /* Borders */
  --blue: #2559ce;       /* Primary color */
  --blue-hover: #1944a5; /* Hover color */
  --surface: #fff;       /* Background */
  --radius: 12px;        /* Border radius */
}
```

## 🔒 Security Notes

- **No Password Encryption**: Passwords stored in plain text in localStorage (for demo only)
- **No Network**: No data leaves your browser
- **LocalStorage Limits**: Browsers typically limit localStorage to 5-10MB
- **Browser Sync**: Disable browser sync if sharing computers
- **Clear Data**: Use browser dev tools > Application > Clear storage to reset

## 📱 Mobile Support

The app is fully responsive:
- Mobile hamburger menu
- Touch-optimized buttons
- Adaptive layout for all screen sizes
- Tested on common mobile browsers

## 💾 Storage Limits

- **Maximum Records**: Limited by browser localStorage (~5-10MB)
- **Estimated Capacity**: 1000-2000 incident records per browser
- **Export Recommended**: Export and backup data regularly

## 🐛 Troubleshooting

### "Data not saving"
- Check if localStorage is enabled in your browser
- Not in private/incognito mode
- Check browser storage quota

### "Can't login"
- Verify your credentials match (case-sensitive username)
- Try the demo account: officer / password123
- Check browser console for errors

### "Records disappeared"
- If you cleared browser storage, data is lost
- Use browser history to recover if possible
- Re-initialize with demo data

### "Slow performance"
- If you have 1000+ records, performance may degrade
- Export old records and clear them
- Use browser storage optimization

## 🔄 Import/Export

### Export Your Data
1. Go to Settings
2. Click "Export data"
3. Save the JSON file

### Import Data
Manually paste exported JSON into browser console:
```javascript
const data = {...}; // Your exported data
storage.importData(data);
```

## 🚫 Limitations (vs. Original API Version)

- No cloud sync
- No AI-powered summaries (local extraction only)
- No role-based permissions enforcement
- No audit trail
- No webhook integrations
- No API access for third-party apps
- localStorage is not shared between browsers/devices

## ✨ Future Enhancements

- IndexedDB for larger datasets
- File upload for attachments
- IndexedDB sync to cloud
- Local PDF export
- Biometric authentication
- Offline service worker
- Dark mode theme

## 📄 License

This is a demonstration project for educational purposes.

## 🎯 Next Steps

1. **Test the app** - Follow the testing workflows above
2. **Create your own incidents** - Use the intake assistant
3. **Export your data** - Backup as JSON
4. **Share with others** - Send them the HTML files
5. **Customize** - Modify CSS and add features as needed

---

**Created**: 2024
**Version**: 1.0-Standalone
**Browser Support**: Chrome, Firefox, Safari, Edge (all modern versions)
